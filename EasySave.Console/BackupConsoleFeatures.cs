using System.Text.Json;
using System.Xml.Linq;

public class BackupConsoleFeatures
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private readonly ConsoleTranslationService _translationService;
    private readonly InteractiveConsole _interactiveConsole;
    private readonly Func<ConsoleMenuRuntime> _runtimeAccessor;
    private readonly Action<string> _setLanguage;

    public BackupConsoleFeatures(
        BackupJobRegistry jobRegistry,
        StateService stateService,
        ConsoleTranslationService translationService,
        InteractiveConsole interactiveConsole,
        Func<ConsoleMenuRuntime> runtimeAccessor,
        Action<string> setLanguage)
    {
        _jobRegistry = jobRegistry;
        _stateService = stateService;
        _translationService = translationService;
        _interactiveConsole = interactiveConsole;
        _runtimeAccessor = runtimeAccessor;
        _setLanguage = setLanguage;
    }

    public void ViewJobs()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        _interactiveConsole.BrowseOutputScreen(
            _translationService.GetViewJobsLabel(TextService),
            BuildJobsScreenLines(jobs));
    }

    public void ManageJobs()
    {
        int selectedIndex = 0;

        while (true)
        {
            IReadOnlyList<string> options =
            [
                _translationService.GetAddJobLabel(TextService),
                _translationService.GetEditJobLabel(TextService),
                _translationService.GetDeleteJobLabel(TextService),
                _translationService.GetBackLabel(TextService)
            ];

            int? selection = _interactiveConsole.SelectOption(
                _translationService.GetManageJobsLabel(TextService),
                options,
                [TextService.GetConfiguredJobsHeader()],
                _translationService.GetNavigationHelp(TextService),
                initialIndex: selectedIndex);

            if (selection == null || selection.Value == 3)
            {
                return;
            }

            selectedIndex = selection.Value;

            switch (selection.Value)
            {
                case 0:
                    AddJob();
                    break;
                case 1:
                    EditJob();
                    break;
                case 2:
                    DeleteJob();
                    break;
            }
        }
    }

    public void ConfigureJob()
    {
        EditJob();
    }

    private void EditJob()
    {
        int selectedIndex = 0;

        while (true)
        {
            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            IReadOnlyList<string> options = jobs
                .Select(BuildJobOptionLabel)
                .Append(_translationService.GetBackLabel(TextService))
                .ToArray();

            int? selection = _interactiveConsole.SelectOption(
                _translationService.GetConfigureJobLabel(TextService),
                options,
                [TextService.GetConfiguredJobsHeader()],
                _translationService.GetNavigationHelp(TextService),
                initialIndex: selectedIndex);

            if (selection == null || selection.Value == jobs.Count)
            {
                return;
            }

            selectedIndex = selection.Value;
            ConfigureSelectedJob(selection.Value + 1, jobs[selection.Value]);
        }
    }

    private void AddJob()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        string? jobNumberValue = _interactiveConsole.PromptLine(
            _translationService.GetAddJobTitle(TextService),
            _translationService.GetNewJobNumberPrompt(TextService),
            [_translationService.GetAvailableJobsLine(TextService, jobs.Count)],
            _translationService.GetLeaveEmptyToGoBackMessage(TextService),
            input => ValidateNewJobNumber(input, jobs.Count));

        if (string.IsNullOrWhiteSpace(jobNumberValue))
        {
            return;
        }

        int jobNumber = int.Parse(jobNumberValue);
        BackupType? selectedType = SelectBackupType();
        if (!selectedType.HasValue)
        {
            return;
        }

        PathSelectionResult sourceSelection = ReadPath(JobPathField.Source, string.Empty);
        if (sourceSelection.Action == PathSelectionAction.Back)
        {
            return;
        }

        PathSelectionResult targetSelection = ReadPath(JobPathField.Target, string.Empty);
        if (targetSelection.Action == PathSelectionAction.Back)
        {
            return;
        }

        var newJob = new BackupJob
        {
            Type = selectedType.Value,
            Source = sourceSelection.Action == PathSelectionAction.UsePath ? sourceSelection.Path! : string.Empty,
            Target = targetSelection.Action == PathSelectionAction.UsePath ? targetSelection.Path! : string.Empty
        };

        _jobRegistry.CreateJob(jobNumber, newJob);
        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
        BackupJob createdJob = _jobRegistry.LoadJobs()[jobNumber - 1];
        var lines = new List<InteractiveConsole.ScreenLine>
        {
            SuccessLine(_translationService.GetJobAddedMessage(TextService, jobNumber)),
            BlankLine()
        };
        lines.AddRange(BuildJobScreenLines(jobNumber, createdJob));
        _interactiveConsole.RenderOutputScreen(
            _translationService.GetAddJobTitle(TextService),
            lines,
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    private void DeleteJob()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        IReadOnlyList<string> options = jobs
            .Select(BuildJobOptionLabel)
            .Append(_translationService.GetBackLabel(TextService))
            .ToArray();

        int? selection = _interactiveConsole.SelectOption(
            _translationService.GetDeleteJobTitle(TextService),
            options,
            [TextService.GetConfiguredJobsHeader()],
            _translationService.GetNavigationHelp(TextService));

        if (selection == null || selection.Value == jobs.Count)
        {
            return;
        }

        int jobNumber = selection.Value + 1;
        BackupJob selectedJob = jobs[selection.Value];

        int? confirmation = _interactiveConsole.SelectOption(
            _translationService.GetDeleteJobTitle(TextService),
            [
                _translationService.GetConfirmDeleteLabel(TextService),
                _translationService.GetBackLabel(TextService)
            ],
            [TextService.GetJobSummaryLine(jobNumber, selectedJob)],
            _translationService.GetNavigationHelp(TextService),
            initialIndex: 1);

        if (confirmation == null || confirmation.Value == 1)
        {
            return;
        }

        BackupJob deletedJob = _jobRegistry.DeleteJob(jobNumber);
        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());

        _interactiveConsole.RenderOutputScreen(
            _translationService.GetDeleteJobTitle(TextService),
            [
                SuccessLine(_translationService.GetJobDeletedMessage(TextService, jobNumber)),
                BlankLine(),
                AccentLine(_translationService.GetSelectedJobLabel(TextService)),
                .. BuildJobScreenLines(jobNumber, deletedJob)
            ],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    public void RunBackups()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        _stateService.SynchronizeConfiguredJobs(jobs);

        IReadOnlyList<string> options = jobs.Select(BuildJobOptionLabel).ToArray();
        IReadOnlyList<int>? selectedIndices = _interactiveConsole.SelectMultipleOptions(
            _translationService.GetRunBackupsLabel(TextService),
            options,
            [TextService.GetConfiguredJobsHeader()],
            _translationService.GetMultiSelectNavigationHelp(TextService),
            _translationService.GetNoValidJobsSelectedMessage(TextService));

        if (selectedIndices == null)
        {
            return;
        }

        var selectedJobs = selectedIndices
            .Select(index => new SelectedBackupJob
            {
                JobNumber = index + 1,
                Job = jobs[index]
            })
            .ToList();

        var outputLines = new List<InteractiveConsole.ScreenLine>
        {
            AccentLine(_translationService.GetRunningJobsMessage(TextService, selectedJobs.Count)),
            BlankLine()
        };

        try
        {
            IReadOnlyList<BackupResult> results = BackupController.StartBackups(selectedJobs);

            for (int index = 0; index < results.Count; index++)
            {
                BackupResult result = results[index];
                bool showHeader = results.Count > 1;

                if (result.Status == BackupExecutionStatus.Finished)
                {
                    outputLines.AddRange(BuildMessageLines(BuildBackupSuccessMessage(result, showHeader), InteractiveConsole.ScreenLineKind.Success));
                }
                else
                {
                    outputLines.AddRange(BuildMessageLines(BuildBackupErrorMessage(result, showHeader), InteractiveConsole.ScreenLineKind.Error));
                }

                if (index < results.Count - 1)
                {
                    outputLines.Add(BlankLine());
                }
            }
        }
        catch (Exception ex)
        {
            outputLines.AddRange(BuildMessageLines(
                _translationService.BuildErrorMessage(TextService, ex.Message),
                InteractiveConsole.ScreenLineKind.Error));
        }

        _interactiveConsole.RenderOutputScreen(
            _translationService.GetRunBackupsLabel(TextService),
            outputLines,
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    public void ViewState()
    {
        _interactiveConsole.BrowseOutputScreen(
            _translationService.GetViewStateLabel(TextService),
            BuildFileScreenLines(RuntimeStoragePaths.StateFilePath, "state.json"));
    }

    public void ViewLogs()
    {
        IReadOnlyList<string> logFilePaths = GetLogFilePathsToDisplay();
        if (logFilePaths.Count == 0)
        {
            _interactiveConsole.RenderOutputScreen(
                _translationService.GetViewLogsLabel(TextService),
                [WarningLine(_translationService.GetNoLogsFoundMessage(TextService))],
                _translationService.GetPauseMessage(TextService));
            Pause();
            return;
        }

        IReadOnlyList<string> options = logFilePaths
            .Select(path => Path.GetFileName(path))
            .Append(_translationService.GetBackLabel(TextService))
            .ToArray();

        int? selection = _interactiveConsole.SelectOption(
            _translationService.GetViewLogsLabel(TextService),
            options,
            [_translationService.GetAvailableLogsLine(TextService)],
            _translationService.GetNavigationHelp(TextService));

        if (selection == null || selection.Value == logFilePaths.Count)
        {
            return;
        }

        string selectedLogFilePath = logFilePaths[selection.Value];
        _interactiveConsole.BrowseOutputScreen(
            _translationService.GetViewLogsLabel(TextService),
            BuildFileScreenLines(selectedLogFilePath, Path.GetFileName(selectedLogFilePath)));
    }

    public void ChangeLanguage()
    {
        int initialIndex = string.Equals(
            TextService.GetLanguageCode(),
            ApplicationTextService.FrenchLanguageCode,
            StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        IReadOnlyList<string> options =
        [
            "English",
            "Francais",
            _translationService.GetBackLabel(TextService)
        ];

        int? selection = _interactiveConsole.SelectOption(
            _translationService.GetChangeLanguageLabel(TextService),
            options,
            [_translationService.GetCurrentLanguageLine(TextService)],
            _translationService.GetNavigationHelp(TextService),
            initialIndex: initialIndex);

        if (selection == null || selection.Value == 2)
        {
            return;
        }

        string languageCode = selection.Value == 0
            ? ApplicationTextService.EnglishLanguageCode
            : ApplicationTextService.FrenchLanguageCode;

        _setLanguage(languageCode);

        _interactiveConsole.RenderOutputScreen(
            _translationService.GetChangeLanguageLabel(TextService),
            [
                SuccessLine(TextService.GetLanguageUpdatedMessage()),
                BlankLine(),
                NormalLine(_translationService.GetCurrentLanguageLine(TextService))
            ],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    public void ManageBusinessSoftware()
    {
        int selectedIndex = 0;

        while (true)
        {
            IReadOnlyList<string> configuredProcesses = RuntimeStoragePaths.GetBlockedProcessNames();
            var contextLines = new List<string>();
            if (configuredProcesses.Count == 0)
            {
                contextLines.Add(_translationService.GetNoBlockedProcessesMessage(TextService));
            }
            else
            {
                contextLines.AddRange(configuredProcesses.Select(processName => $"- {processName}.exe"));
            }

            IReadOnlyList<string> options =
            [
                _translationService.GetAddProcessLabel(TextService),
                _translationService.GetRemoveProcessLabel(TextService),
                _translationService.GetBackLabel(TextService)
            ];

            int? selection = _interactiveConsole.SelectOption(
                _translationService.GetBusinessSoftwareTitle(TextService),
                options,
                contextLines,
                _translationService.GetNavigationHelp(TextService),
                initialIndex: selectedIndex);

            if (selection == null || selection.Value == 2)
            {
                return;
            }

            selectedIndex = selection.Value;

            if (selection.Value == 0)
            {
                AddBlockedProcess(configuredProcesses);
            }
            else
            {
                RemoveBlockedProcess(configuredProcesses);
            }
        }
    }

    public void ChangeLogFormat()
    {
        int initialIndex = RuntimeStoragePaths.GetLogFileFormat() == RuntimeStoragePaths.XmlLogFileFormat
            ? 1
            : 0;

        IReadOnlyList<string> options =
        [
            "JSON",
            "XML",
            _translationService.GetBackLabel(TextService)
        ];

        int? selection = _interactiveConsole.SelectOption(
            _translationService.GetChangeLogFormatLabel(TextService),
            options,
            [_translationService.GetCurrentLogFormatLine(TextService)],
            _translationService.GetNavigationHelp(TextService),
            initialIndex: initialIndex);

        if (selection == null || selection.Value == 2)
        {
            return;
        }

        string logFileFormat = selection.Value == 0
            ? RuntimeStoragePaths.JsonLogFileFormat
            : RuntimeStoragePaths.XmlLogFileFormat;

        RuntimeStoragePaths.SetLogFileFormat(logFileFormat);

        _interactiveConsole.RenderOutputScreen(
            _translationService.GetChangeLogFormatLabel(TextService),
            [
                SuccessLine(_translationService.GetLogFormatUpdatedMessage(TextService, logFileFormat)),
                BlankLine(),
                NormalLine(_translationService.GetCurrentLogFormatLine(TextService))
            ],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    public void ConfigureEncryptionSettings()
    {
        int selectedIndex = 0;

        while (true)
        {
            IReadOnlyList<string> options =
            [
                TextService.GetText("Console.ConfigureEncryptedExtensionsLabel"),
                TextService.GetText("Console.ConfigureCryptoSoftKeyLabel"),
                _translationService.GetBackLabel(TextService)
            ];

            int? selection = _interactiveConsole.SelectOption(
                TextService.GetText("Console.EncryptionSettingsLabel"),
                options,
                BuildEncryptionSettingsContext(),
                _translationService.GetNavigationHelp(TextService),
                initialIndex: selectedIndex);

            if (selection == null || selection.Value == 2)
            {
                return;
            }

            selectedIndex = selection.Value;

            if (selection.Value == 0)
            {
                ConfigureEncryptedExtensions();
                continue;
            }

            ConfigureCryptoSoftKey();
        }
    }

    private ApplicationTextService TextService => _runtimeAccessor().TextService;

    private BackupController BackupController => _runtimeAccessor().BackupController;

    private IReadOnlyList<string> BuildEncryptionSettingsContext()
    {
        string extensions = FormatExtensions(RuntimeStoragePaths.GetEncryptedExtensions());
        string keyStatus = string.IsNullOrWhiteSpace(RuntimeStoragePaths.GetCryptoSoftKey())
            ? TextService.GetText("Console.CryptoSoftKeyMissing")
            : TextService.GetText("Console.CryptoSoftKeyConfigured");

        return
        [
            TextService.FormatText("Console.CurrentEncryptedExtensionsLine", extensions),
            TextService.FormatText("Console.CurrentCryptoSoftKeyLine", keyStatus)
        ];
    }

    private void ConfigureEncryptedExtensions()
    {
        string? extensionValue = _interactiveConsole.PromptLine(
            TextService.GetText("Console.ConfigureEncryptedExtensionsLabel"),
            TextService.GetText("Console.EncryptedExtensionsPrompt"),
            [TextService.FormatText("Console.CurrentEncryptedExtensionsLine", FormatExtensions(RuntimeStoragePaths.GetEncryptedExtensions()))],
            TextService.GetText("Console.EmptyExtensionsAllowedMessage"));

        if (extensionValue is null)
        {
            return;
        }

        RuntimeStoragePaths.SetEncryptedExtensions(IsClearCommand(extensionValue) ? [] : [extensionValue]);

        _interactiveConsole.RenderOutputScreen(
            TextService.GetText("Console.ConfigureEncryptedExtensionsLabel"),
            [
                SuccessLine(TextService.GetText("Console.EncryptionSettingsUpdatedMessage")),
                BlankLine(),
                NormalLine(TextService.FormatText(
                    "Console.CurrentEncryptedExtensionsLine",
                    FormatExtensions(RuntimeStoragePaths.GetEncryptedExtensions())))
            ],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    private void ConfigureCryptoSoftKey()
    {
        string? key = _interactiveConsole.PromptLine(
            TextService.GetText("Console.ConfigureCryptoSoftKeyLabel"),
            TextService.GetText("Console.CryptoSoftKeyPrompt"),
            BuildEncryptionSettingsContext(),
            TextService.GetText("Console.EmptyCryptoSoftKeyWarning"));

        if (key is null)
        {
            return;
        }

        RuntimeStoragePaths.SetCryptoSoftKey(IsClearCommand(key) ? string.Empty : key);

        _interactiveConsole.RenderOutputScreen(
            TextService.GetText("Console.ConfigureCryptoSoftKeyLabel"),
            [
                SuccessLine(TextService.GetText("Console.EncryptionSettingsUpdatedMessage")),
                BlankLine(),
                .. BuildEncryptionSettingsContext().Select(NormalLine)
            ],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    private void ConfigureSelectedJob(int jobNumber, BackupJob job)
    {
        BackupType? selectedType = SelectBackupType(job.Type, allowKeepCurrent: true);
        if (!selectedType.HasValue)
        {
            return;
        }

        if (selectedType.Value != job.Type)
        {
            _jobRegistry.UpdateJob(jobNumber, new BackupJob
            {
                Name = job.Name,
                Source = job.Source,
                Target = job.Target,
                Type = selectedType.Value
            });
            job = _jobRegistry.LoadJobs()[jobNumber - 1];
        }

        PathSelectionResult sourceSelection = ReadPath(JobPathField.Source, job.Source);
        if (sourceSelection.Action == PathSelectionAction.Back)
        {
            return;
        }

        PathSelectionResult targetSelection = ReadPath(JobPathField.Target, job.Target);
        if (targetSelection.Action == PathSelectionAction.Back)
        {
            return;
        }

        bool hasChanges = false;
        BackupJob updatedJob = job;

        if (sourceSelection.Action == PathSelectionAction.UsePath
            && !PathsAreEqual(job.Source, sourceSelection.Path!))
        {
            updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Source, sourceSelection.Path!);
            hasChanges = true;
        }

        if (targetSelection.Action == PathSelectionAction.UsePath
            && !PathsAreEqual(updatedJob.Target, targetSelection.Path!))
        {
            updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Target, targetSelection.Path!);
            hasChanges = true;
        }

        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());

        var lines = new List<InteractiveConsole.ScreenLine>();

        if (hasChanges)
        {
            lines.Add(SuccessLine(_translationService.GetJobEditedMessage(TextService, jobNumber)));
        }
        else
        {
            lines.Add(WarningLine(_translationService.GetNoConfigurationChangesMessage(TextService)));
        }

        lines.Add(BlankLine());
        lines.Add(AccentLine(_translationService.GetSelectedJobLabel(TextService)));
        lines.AddRange(BuildJobScreenLines(jobNumber, updatedJob));
        _interactiveConsole.RenderOutputScreen(
            _translationService.GetConfigureJobLabel(TextService),
            lines,
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    private string? ValidateNewJobNumber(string input, int existingCount)
    {
        if (!int.TryParse(input, out int jobNumber) || jobNumber < 1)
        {
            return _translationService.BuildErrorMessage(TextService, TextService.GetInvalidJobNumberMessage());
        }

        if (jobNumber <= existingCount)
        {
            return _translationService.BuildErrorMessage(TextService, _translationService.GetJobAlreadyExistsMessage(TextService, jobNumber));
        }

        return null;
    }

    private BackupType? SelectBackupType(BackupType? currentType = null, bool allowKeepCurrent = false)
    {
        var options = new List<string>
        {
            TextService.GetBackupTypeDisplayName(BackupType.Full),
            TextService.GetBackupTypeDisplayName(BackupType.Differential)
        };

        int keepCurrentIndex = -1;
        if (allowKeepCurrent && currentType.HasValue)
        {
            keepCurrentIndex = options.Count;
            options.Add(_translationService.GetSkipLabel(TextService));
        }

        options.Add(_translationService.GetBackLabel(TextService));

        int initialIndex = currentType == BackupType.Differential ? 1 : 0;
        int? selection = _interactiveConsole.SelectOption(
            _translationService.GetSelectBackupTypeTitle(TextService),
            options,
            allowKeepCurrent && currentType.HasValue
                ? [$"{_translationService.GetChangeTypeLabel(TextService)}: {TextService.GetBackupTypeDisplayName(currentType.Value)}"]
                : null,
            _translationService.GetNavigationHelp(TextService),
            initialIndex: initialIndex);

        if (selection == null || selection.Value == options.Count - 1)
        {
            return null;
        }

        if (selection.Value == keepCurrentIndex && currentType.HasValue)
        {
            return currentType.Value;
        }

        return selection.Value == 0 ? BackupType.Full : BackupType.Differential;
    }

    private void AddBlockedProcess(IReadOnlyList<string> configuredProcesses)
    {
        string? processNameInput = _interactiveConsole.PromptLine(
            _translationService.GetBusinessSoftwareTitle(TextService),
            _translationService.GetProcessNamePrompt(TextService),
            null,
            _translationService.GetLeaveEmptyToGoBackMessage(TextService));

        if (string.IsNullOrWhiteSpace(processNameInput))
        {
            return;
        }

        string normalizedProcessName = NormalizeProcessName(processNameInput);
        if (configuredProcesses.Any(name => string.Equals(name, normalizedProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            _interactiveConsole.RenderOutputScreen(
                _translationService.GetBusinessSoftwareTitle(TextService),
                [WarningLine(_translationService.GetProcessAlreadyConfiguredMessage(TextService))],
                _translationService.GetPauseMessage(TextService));
            Pause();
            return;
        }

        RuntimeStoragePaths.SetBlockedProcessNames(configuredProcesses.Append(normalizedProcessName));
        _interactiveConsole.RenderOutputScreen(
            _translationService.GetBusinessSoftwareTitle(TextService),
            [SuccessLine(_translationService.GetProcessAddedMessage(TextService))],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    private void RemoveBlockedProcess(IReadOnlyList<string> configuredProcesses)
    {
        if (configuredProcesses.Count == 0)
        {
            _interactiveConsole.RenderOutputScreen(
                _translationService.GetBusinessSoftwareTitle(TextService),
                [WarningLine(_translationService.GetNoBlockedProcessesMessage(TextService))],
                _translationService.GetPauseMessage(TextService));
            Pause();
            return;
        }

        IReadOnlyList<string> options = configuredProcesses
            .Select(processName => $"{processName}.exe")
            .Append(_translationService.GetBackLabel(TextService))
            .ToArray();

        int? selection = _interactiveConsole.SelectOption(
            _translationService.GetBusinessSoftwareTitle(TextService),
            options,
            null,
            _translationService.GetNavigationHelp(TextService));

        if (selection == null || selection.Value == configuredProcesses.Count)
        {
            return;
        }

        string processToRemove = configuredProcesses[selection.Value];
        RuntimeStoragePaths.SetBlockedProcessNames(
            configuredProcesses.Where(process => !string.Equals(process, processToRemove, StringComparison.OrdinalIgnoreCase)));

        _interactiveConsole.RenderOutputScreen(
            _translationService.GetBusinessSoftwareTitle(TextService),
            [SuccessLine(_translationService.GetProcessRemovedMessage(TextService))],
            _translationService.GetPauseMessage(TextService));
        Pause();
    }

    private IReadOnlyList<InteractiveConsole.ScreenLine> BuildFileScreenLines(string filePath, string displayName)
    {
        var lines = new List<InteractiveConsole.ScreenLine>
        {
            AccentLine(_translationService.GetFilePathLine(TextService, filePath)),
            BlankLine()
        };

        if (!File.Exists(filePath))
        {
            lines.Add(WarningLine(_translationService.GetFileNotFoundMessage(TextService, displayName)));
            return lines;
        }

        string content = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            lines.Add(WarningLine(_translationService.GetFileEmptyMessage(TextService, displayName)));
            return lines;
        }

        string formattedContent = IsDailyLogFile(displayName)
            ? FormatLogContent(filePath, content)
            : content;

        lines.AddRange(formattedContent
            .ReplaceLineEndings()
            .Split(Environment.NewLine)
            .Select(NormalLine));
        return lines;
    }

    private static bool IsDailyLogFile(string displayName)
    {
        return displayName.Length == "yyyy-MM-dd.json".Length
            && (displayName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || displayName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatLogContent(string filePath, string content)
    {
        return Path.GetExtension(filePath).Equals(".xml", StringComparison.OrdinalIgnoreCase)
            ? FormatXmlLogContent(content)
            : FormatJsonLogContent(content);
    }

    private static string FormatJsonLogContent(string content)
    {
        var formattedEntries = new List<string>();

        foreach (string jsonBlock in ReadJsonBlocks(content))
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(jsonBlock);
                formattedEntries.Add(JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (JsonException)
            {
                formattedEntries.Add(jsonBlock);
            }
        }

        return string.Join(Environment.NewLine, formattedEntries);
    }

    private static IEnumerable<string> ReadJsonBlocks(string content)
    {
        var blockLines = new List<string>();
        int depth = 0;

        foreach (string line in content.Split(Environment.NewLine).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            blockLines.Add(line);
            depth += line.Count(character => character == '{');
            depth -= line.Count(character => character == '}');

            if (depth == 0 && blockLines.Count > 0)
            {
                yield return string.Join(Environment.NewLine, blockLines);
                blockLines.Clear();
            }
        }
    }

    private static string FormatXmlLogContent(string content)
    {
        try
        {
            return XDocument.Parse(content).ToString();
        }
        catch
        {
            return content;
        }
    }

    private static IReadOnlyList<string> GetLogFilePathsToDisplay()
    {
        if (!Directory.Exists(RuntimeStoragePaths.LogsDirectoryPath))
        {
            return Array.Empty<string>();
        }

        return RuntimeStoragePaths.GetSupportedLogFilePatterns()
            .SelectMany(pattern => Directory.EnumerateFiles(RuntimeStoragePaths.LogsDirectoryPath, pattern))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();
    }

    private PathSelectionResult ReadPath(JobPathField pathField, string currentPath)
    {
        while (true)
        {
            IReadOnlyList<string> options =
            [
                pathField == JobPathField.Source
                    ? _translationService.GetPasteSourcePathLabel(TextService)
                    : _translationService.GetPasteTargetPathLabel(TextService),
                _translationService.GetSearchDirectoryLabel(TextService),
                _translationService.GetSkipLabel(TextService),
                _translationService.GetBackLabel(TextService)
            ];

            int? selection = _interactiveConsole.SelectOption(
                _translationService.GetConfigurePathTitle(TextService, pathField),
                options,
                [_translationService.GetCurrentValueLine(TextService, FormatPath(currentPath))],
                _translationService.GetNavigationHelp(TextService));

            if (selection == null || selection.Value == 3)
            {
                return PathSelectionResult.Back();
            }

            if (selection.Value == 2)
            {
                return PathSelectionResult.KeepCurrent();
            }

            if (selection.Value == 0)
            {
                string? pastedPath = PromptDirectoryPath(pathField, currentPath);
                if (!string.IsNullOrWhiteSpace(pastedPath))
                {
                    return PathSelectionResult.UsePath(pastedPath);
                }

                continue;
            }

            string? searchedPath = SearchPath(pathField, currentPath);
            if (!string.IsNullOrWhiteSpace(searchedPath))
            {
                return PathSelectionResult.UsePath(searchedPath);
            }
        }
    }

    private string? PromptDirectoryPath(JobPathField pathField, string currentPath)
    {
        return _interactiveConsole.PromptLine(
            _translationService.GetConfigurePathTitle(TextService, pathField),
            pathField == JobPathField.Source
                ? _translationService.GetSourcePathPrompt(TextService)
                : _translationService.GetTargetPathPrompt(TextService),
            [_translationService.GetCurrentValueLine(TextService, FormatPath(currentPath))],
            _translationService.GetLeaveEmptyToGoBackMessage(TextService),
            path => Directory.Exists(path)
                ? null
                : _translationService.BuildErrorMessage(TextService, _translationService.GetDirectoryDoesNotExistMessage(TextService)));
    }

    private string? SearchPath(JobPathField pathField, string currentPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            _interactiveConsole.RenderOutputScreen(
                _translationService.GetConfigurePathTitle(TextService, pathField),
                [ErrorLine(_translationService.GetSearchUnsupportedMessage(TextService))],
                _translationService.GetPauseMessage(TextService));
            Pause();
            return null;
        }

        while (true)
        {
            IReadOnlyList<string> currentValueContext =
            [
                _translationService.GetCurrentValueLine(TextService, FormatPath(currentPath))
            ];

            string? rootDirectory = _interactiveConsole.PromptLine(
                _translationService.GetConfigurePathTitle(TextService, pathField),
                _translationService.GetSearchRootPrompt(TextService),
                currentValueContext,
                _translationService.GetLeaveEmptyToGoBackMessage(TextService),
                path => Directory.Exists(path)
                    ? null
                    : _translationService.BuildErrorMessage(TextService, _translationService.GetInvalidSearchRootMessage(TextService)));

            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                return null;
            }

            string? query = _interactiveConsole.PromptLine(
                _translationService.GetConfigurePathTitle(TextService, pathField),
                _translationService.GetSearchQueryPrompt(TextService),
                currentValueContext,
                _translationService.GetLeaveEmptyToGoBackMessage(TextService));

            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            DirectorySearchResult searchResult = FastDirectorySearch.Search(rootDirectory, query);
            if (searchResult.Directories.Count == 0)
            {
                _interactiveConsole.RenderOutputScreen(
                    _translationService.GetConfigurePathTitle(TextService, pathField),
                    [ErrorLine(_translationService.GetNoSearchMatchesMessage(TextService))],
                    _translationService.GetPauseMessage(TextService));
                Pause();
                continue;
            }

            var contextLines = new List<string>(currentValueContext);
            if (searchResult.WasLimitReached)
            {
                contextLines.Add(_translationService.GetSearchStoppedMessage(TextService, FastDirectorySearch.DefaultResultLimit));
            }

            IReadOnlyList<string> options = searchResult.Directories
                .Append(_translationService.GetBackLabel(TextService))
                .ToArray();

            int? selection = _interactiveConsole.SelectOption(
                _translationService.GetSearchDirectoryLabel(TextService),
                options,
                contextLines,
                _translationService.GetNavigationHelp(TextService));

            if (selection == null || selection.Value == searchResult.Directories.Count)
            {
                return null;
            }

            return searchResult.Directories[selection.Value];
        }
    }

    private IReadOnlyList<InteractiveConsole.ScreenLine> BuildJobsScreenLines(IReadOnlyList<BackupJob> jobs)
    {
        var lines = new List<InteractiveConsole.ScreenLine>
        {
            AccentLine(TextService.GetConfiguredJobsHeader()),
            BlankLine()
        };

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            lines.AddRange(BuildJobScreenLines(index + 1, job));
        }

        return lines;
    }

    private IReadOnlyList<InteractiveConsole.ScreenLine> BuildJobScreenLines(int jobNumber, BackupJob job)
    {
        return
        [
            AccentLine(TextService.GetJobSummaryLine(jobNumber, job)),
            NormalLine($"{_translationService.GetSourceLabel(TextService)}{FormatPath(job.Source)}"),
            NormalLine($"{_translationService.GetTargetLabel(TextService)}{FormatPath(job.Target)}"),
            NormalLine(TextService.GetJobTypeLine(job.Type)),
            NormalLine(TextService.GetJobConfigurationStatusLine(job)),
            BlankLine()
        ];
    }

    private string BuildBackupSuccessMessage(BackupResult result, bool showHeader)
    {
        string details = TextService.FormatBackupResult(result);
        string successMessage = TextService.GetBackupSuccessMessage();

        if (!showHeader)
        {
            return $"{details}\n{successMessage}";
        }

        return $"{_translationService.GetJobHeader(TextService, result)}\n{details}\n{successMessage}";
    }

    private string BuildBackupErrorMessage(BackupResult result, bool showHeader)
    {
        string details = TextService.FormatBackupResult(result);
        return showHeader
            ? $"{_translationService.GetJobHeader(TextService, result)}\n{details}"
            : details;
    }

    private string BuildJobOptionLabel(BackupJob job)
    {
        string status = IsConfigured(job)
            ? _translationService.GetConfiguredLabel(TextService)
            : _translationService.GetIncompleteLabel(TextService);

        return $"{job.Name} ({TextService.GetBackupTypeDisplayName(job.Type)}) - {status}";
    }

    private string FormatPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? _translationService.GetNotConfiguredLabel(TextService)
            : $"<{path}>";
    }

    private string FormatExtensions(IReadOnlyList<string> extensions)
    {
        return extensions.Count == 0
            ? TextService.GetText("Console.NoEncryptedExtensions")
            : string.Join("; ", extensions);
    }

    private static bool IsClearCommand(string value)
    {
        return value.Equals("clear", StringComparison.OrdinalIgnoreCase)
            || value.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    private void Pause()
    {
        _interactiveConsole.WaitForKey();
    }

    private static bool IsConfigured(BackupJob job)
    {
        return !string.IsNullOrWhiteSpace(job.Source)
            && !string.IsNullOrWhiteSpace(job.Target);
    }

    private static bool PathsAreEqual(string left, string right)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(left, right, comparison);
    }

    private static string NormalizeProcessName(string processName)
    {
        string normalized = processName.Trim().ToLowerInvariant();
        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }

    private static IEnumerable<InteractiveConsole.ScreenLine> BuildMessageLines(
        string message,
        InteractiveConsole.ScreenLineKind kind)
    {
        return message.ReplaceLineEndings().Split(Environment.NewLine).Select(line => new InteractiveConsole.ScreenLine(line, kind));
    }

    private static InteractiveConsole.ScreenLine NormalLine(string text)
    {
        return new InteractiveConsole.ScreenLine(text);
    }

    private static InteractiveConsole.ScreenLine AccentLine(string text)
    {
        return new InteractiveConsole.ScreenLine(text, InteractiveConsole.ScreenLineKind.Accent);
    }

    private static InteractiveConsole.ScreenLine SuccessLine(string text)
    {
        return new InteractiveConsole.ScreenLine(text, InteractiveConsole.ScreenLineKind.Success);
    }

    private static InteractiveConsole.ScreenLine WarningLine(string text)
    {
        return new InteractiveConsole.ScreenLine(text, InteractiveConsole.ScreenLineKind.Warning);
    }

    private static InteractiveConsole.ScreenLine ErrorLine(string text)
    {
        return new InteractiveConsole.ScreenLine(text, InteractiveConsole.ScreenLineKind.Error);
    }

    private static InteractiveConsole.ScreenLine BlankLine()
    {
        return new InteractiveConsole.ScreenLine(string.Empty);
    }

    private enum PathSelectionAction
    {
        Back,
        KeepCurrent,
        UsePath
    }

    private sealed class PathSelectionResult
    {
        private PathSelectionResult(PathSelectionAction action, string? path)
        {
            Action = action;
            Path = path;
        }

        public PathSelectionAction Action { get; }

        public string? Path { get; }

        public static PathSelectionResult Back()
        {
            return new PathSelectionResult(PathSelectionAction.Back, null);
        }

        public static PathSelectionResult KeepCurrent()
        {
            return new PathSelectionResult(PathSelectionAction.KeepCurrent, null);
        }

        public static PathSelectionResult UsePath(string path)
        {
            return new PathSelectionResult(PathSelectionAction.UsePath, path);
        }
    }
}
