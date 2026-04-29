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
        Console.Clear();
        WriteSectionHeader(_translationService.GetViewJobsLabel(TextService));
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        RenderJobs(jobs);
        Pause();
    }

    public void ConfigureJob()
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

        Console.Clear();
        WriteSectionHeader(_translationService.GetRunBackupsLabel(TextService));
        Console.WriteLine(_translationService.GetRunningJobsMessage(TextService, selectedJobs.Count));
        Console.WriteLine();

        try
        {
            IReadOnlyList<BackupResult> results = BackupController.StartBackups(selectedJobs);

            for (int index = 0; index < results.Count; index++)
            {
                BackupResult result = results[index];
                bool showHeader = results.Count > 1;

                if (result.Status == BackupExecutionStatus.Finished)
                {
                    WriteSuccess(BuildBackupSuccessMessage(result, showHeader));
                }
                else
                {
                    WriteError(BuildBackupErrorMessage(result, showHeader));
                }

                if (index < results.Count - 1)
                {
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            WriteError(_translationService.BuildErrorMessage(TextService, ex.Message));
        }

        Console.WriteLine();
        Pause();
    }

    public void ViewState()
    {
        Console.Clear();
        WriteSectionHeader(_translationService.GetViewStateLabel(TextService));
        RenderFile(RuntimeStoragePaths.StateFilePath, "state.json");
        Pause();
    }

    public void ViewLogs()
    {
        IReadOnlyList<string> logFilePaths = GetLogFilePathsToDisplay();
        if (logFilePaths.Count == 0)
        {
            Console.Clear();
            WriteSectionHeader(_translationService.GetViewLogsLabel(TextService));
            WriteWarning(_translationService.GetNoLogsFoundMessage(TextService));
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

        Console.Clear();
        WriteSectionHeader(_translationService.GetViewLogsLabel(TextService));
        string selectedLogFilePath = logFilePaths[selection.Value];
        RenderFile(selectedLogFilePath, Path.GetFileName(selectedLogFilePath));
        Pause();
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

        Console.Clear();
        WriteSectionHeader(_translationService.GetChangeLanguageLabel(TextService));
        WriteSuccess(TextService.GetLanguageUpdatedMessage());
        Console.WriteLine();
        Console.WriteLine(_translationService.GetCurrentLanguageLine(TextService));
        Pause();
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

        Console.Clear();
        WriteSectionHeader(_translationService.GetChangeLogFormatLabel(TextService));
        WriteSuccess(_translationService.GetLogFormatUpdatedMessage(TextService, logFileFormat));
        Console.WriteLine();
        Console.WriteLine(_translationService.GetCurrentLogFormatLine(TextService));
        Pause();
    }

    private ApplicationTextService TextService => _runtimeAccessor().TextService;

    private BackupController BackupController => _runtimeAccessor().BackupController;

    private void ConfigureSelectedJob(int jobNumber, BackupJob job)
    {
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

        Console.Clear();
        WriteSectionHeader(_translationService.GetConfigureJobLabel(TextService));

        if (hasChanges)
        {
            WriteSuccess(_translationService.GetConfigurationCompletedMessage(TextService));
        }
        else
        {
            WriteWarning(_translationService.GetNoConfigurationChangesMessage(TextService));
        }

        Console.WriteLine();
        Console.WriteLine(_translationService.GetSelectedJobLabel(TextService));
        RenderJob(jobNumber, updatedJob);
        Pause();
    }

    private void RenderFile(string filePath, string displayName)
    {
        Console.WriteLine(_translationService.GetFilePathLine(TextService, filePath));
        Console.WriteLine();

        if (!File.Exists(filePath))
        {
            WriteWarning(_translationService.GetFileNotFoundMessage(TextService, displayName));
            return;
        }

        string content = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            WriteWarning(_translationService.GetFileEmptyMessage(TextService, displayName));
            return;
        }

        Console.WriteLine(IsDailyLogFile(displayName)
            ? FormatLogContent(filePath, content)
            : content);
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
            Console.Clear();
            WriteSectionHeader(_translationService.GetConfigurePathTitle(TextService, pathField));
            WriteError(_translationService.GetSearchUnsupportedMessage(TextService));
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
                Console.Clear();
                WriteSectionHeader(_translationService.GetConfigurePathTitle(TextService, pathField));
                WriteError(_translationService.GetNoSearchMatchesMessage(TextService));
                Console.WriteLine();
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

    private void RenderJobs(IReadOnlyList<BackupJob> jobs)
    {
        Console.WriteLine(TextService.GetConfiguredJobsHeader());
        Console.WriteLine();

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            RenderJob(index + 1, job);
        }
    }

    private void RenderJob(int jobNumber, BackupJob job)
    {
        Console.WriteLine(TextService.GetJobSummaryLine(jobNumber, job));
        Console.WriteLine($"{_translationService.GetSourceLabel(TextService)}{FormatPath(job.Source)}");
        Console.WriteLine($"{_translationService.GetTargetLabel(TextService)}{FormatPath(job.Target)}");
        Console.WriteLine(TextService.GetJobTypeLine(job.Type));
        Console.WriteLine(TextService.GetJobConfigurationStatusLine(job));
        Console.WriteLine();
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

    private void Pause()
    {
        Console.WriteLine(_translationService.GetPauseMessage(TextService));
        Console.ReadKey(true);
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

    private static void WriteSuccess(string message)
    {
        WriteColored(message, ConsoleColor.Green);
    }

    private static void WriteError(string message)
    {
        WriteColored(message, ConsoleColor.Red);
    }

    private static void WriteWarning(string message)
    {
        WriteColored(message, ConsoleColor.Yellow);
    }

    private static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteSectionHeader(string title)
    {
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine();
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
