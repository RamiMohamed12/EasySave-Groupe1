public class BackupConsoleFeatures
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private readonly ConsoleTranslationService _translationService;
    private readonly Func<ConsoleMenuRuntime> _runtimeAccessor;
    private readonly Action<string> _setLanguage;

    public BackupConsoleFeatures(
        BackupJobRegistry jobRegistry,
        StateService stateService,
        ConsoleTranslationService translationService,
        Func<ConsoleMenuRuntime> runtimeAccessor,
        Action<string> setLanguage)
    {
        _jobRegistry = jobRegistry;
        _stateService = stateService;
        _translationService = translationService;
        _runtimeAccessor = runtimeAccessor;
        _setLanguage = setLanguage;
    }

    public void ViewJobs()
    {
        Console.Clear();
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        RenderJobs(jobs);
        Pause();
    }

    public void ConfigureJob()
    {
        Console.Clear();
        WriteSectionHeader(_translationService.GetConfigureJobLabel(TextService));

        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        RenderJobs(jobs);

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        try
        {
            BackupJob updatedJob = jobs[jobNumber - 1];
            string? sourcePath = ReadPath(JobPathField.Source);
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Source, sourcePath);
            }

            string? targetPath = ReadPath(JobPathField.Target);
            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Target, targetPath);
            }

            _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
            Console.WriteLine();
            WriteSuccess(_translationService.GetConfigurationCompletedMessage(TextService));
            Console.WriteLine();
            Console.WriteLine(TextService.GetConfiguredJobsHeader());
            RenderJob(jobNumber, updatedJob);
        }
        catch (Exception ex)
        {
            WriteError(_translationService.BuildErrorMessage(TextService, ex.Message));
        }

        Pause();
    }

    public void RunBackups()
    {
        Console.Clear();
        WriteSectionHeader(_translationService.GetRunBackupsLabel(TextService));
        Console.WriteLine(_translationService.GetSelectionInstructionsTitle(TextService));
        Console.WriteLine(_translationService.GetSingleSelectionExample(TextService));
        Console.WriteLine(_translationService.GetRangeSelectionExample(TextService));
        Console.WriteLine(_translationService.GetMultipleSelectionExample(TextService));
        Console.WriteLine();

        Console.Write("> ");
        string? selection = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(selection))
        {
            WriteError(TextService.GetSelectionRequiredMessage());
            Pause();
            return;
        }

        try
        {
            var selectedJobNumbers = ArgumentParser.ParseJobSelection(selection);
            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            _stateService.SynchronizeConfiguredJobs(jobs);

            var selectedJobs = new List<SelectedBackupJob>();
            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber >= 1 && jobNumber <= jobs.Count)
                {
                    selectedJobs.Add(new SelectedBackupJob
                    {
                        JobNumber = jobNumber,
                        Job = jobs[jobNumber - 1]
                    });
                }
            }

            if (selectedJobs.Count == 0)
            {
                WriteError(_translationService.GetNoValidJobsSelectedMessage(TextService));
                Pause();
                return;
            }

            Console.WriteLine();
            Console.WriteLine(_translationService.GetRunningJobsMessage(TextService, selectedJobs.Count));
            Console.WriteLine();

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

    public void ChangeLanguage()
    {
        Console.Clear();
        WriteSectionHeader(_translationService.GetChangeLanguageLabel(TextService));
        Console.WriteLine(_translationService.GetCurrentLanguageLine(TextService));
        Console.WriteLine();
        Console.WriteLine(_translationService.GetLanguageOptionLabel(1, "English"));
        Console.WriteLine(_translationService.GetLanguageOptionLabel(2, "Francais"));
        Console.WriteLine(_translationService.GetLanguageOptionLabel(3, _translationService.GetBackLabel(TextService)));
        Console.WriteLine();
        Console.Write(_translationService.GetLanguageSelectionPrompt(TextService));

        string? choice = Console.ReadLine();
        string normalizedChoice = choice?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalizedChoice is "3" or "back" or "retour")
        {
            return;
        }

        string? languageCode = normalizedChoice switch
        {
            "1" or "en" or "english" or "anglais" => ApplicationTextService.EnglishLanguageCode,
            "2" or "fr" or "french" or "francais" => ApplicationTextService.FrenchLanguageCode,
            _ => null
        };

        if (languageCode == null)
        {
            WriteError(_translationService.GetInvalidLanguageSelectionMessage(TextService));
            Pause();
            return;
        }

        _setLanguage(languageCode);
        WriteSuccess(TextService.GetLanguageUpdatedMessage());
        Pause();
    }

    private ApplicationTextService TextService => _runtimeAccessor().TextService;

    private ArgumentParser ArgumentParser => _runtimeAccessor().ArgumentParser;

    private BackupController BackupController => _runtimeAccessor().BackupController;

    private string? ReadPath(JobPathField pathField)
    {
        Console.WriteLine();
        Console.WriteLine(_translationService.GetLanguageOptionLabel(1, GetPathInputLabel(pathField)));
        Console.WriteLine(_translationService.GetLanguageOptionLabel(2, _translationService.GetSkipLabel(TextService)));
        Console.WriteLine();
        Console.Write(_translationService.GetPathInputModePrompt(TextService));

        string normalizedChoice = Console.ReadLine()?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalizedChoice switch
        {
            "1" or "paste" or "coller" => ReadPastedPath(pathField),
            "2" or "skip" or "back" or "ignorer" or "retour" => null,
            _ => null
        };
    }

    private string GetPathInputLabel(JobPathField pathField)
    {
        return pathField == JobPathField.Source
            ? _translationService.GetPasteSourcePathLabel(TextService)
            : _translationService.GetPasteTargetPathLabel(TextService);
    }

    private string? ReadPastedPath(JobPathField pathField)
    {
        Console.Write(pathField == JobPathField.Source
            ? _translationService.GetSourcePathPrompt(TextService)
            : _translationService.GetTargetPathPrompt(TextService));

        string path = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            WriteError(TextService.GetPathValueRequiredMessage());
            return null;
        }

        if (!Directory.Exists(path))
        {
            WriteError(_translationService.BuildErrorMessage(TextService, _translationService.GetDirectoryDoesNotExistMessage(TextService)));
            return null;
        }

        return path;
    }

    private string? SearchPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            WriteError(_translationService.GetSearchUnsupportedMessage(TextService));
            return null;
        }

        Console.Write(_translationService.GetSearchRootPrompt(TextService));
        string rootDirectory = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
        {
            WriteError(_translationService.GetInvalidSearchRootMessage(TextService));
            return null;
        }

        Console.Write(_translationService.GetSearchQueryPrompt(TextService));
        string query = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            WriteError(TextService.GetPathValueRequiredMessage());
            return null;
        }

        DirectorySearchResult searchResult = FastDirectorySearch.Search(rootDirectory, query);
        if (searchResult.Directories.Count == 0)
        {
            WriteError(_translationService.GetNoSearchMatchesMessage(TextService));
            return null;
        }

        Console.WriteLine();
        foreach ((string path, int index) in searchResult.Directories.Select((path, index) => (path, index)))
        {
            Console.WriteLine(_translationService.GetMenuOptionLabel(index + 1, path));
        }

        if (searchResult.WasLimitReached)
        {
            Console.WriteLine(_translationService.GetSearchStoppedMessage(TextService, FastDirectorySearch.DefaultResultLimit));
        }

        Console.WriteLine();
        Console.Write(_translationService.GetSearchResultSelectionPrompt(TextService));
        string selection = Console.ReadLine()?.Trim() ?? string.Empty;

        if (!int.TryParse(selection, out int resultNumber)
            || resultNumber < 1
            || resultNumber > searchResult.Directories.Count)
        {
            WriteError(_translationService.GetInvalidSearchResultSelectionMessage(TextService));
            return null;
        }

        return searchResult.Directories[resultNumber - 1];
    }

    private int GetJobNumber()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();

        Console.WriteLine(_translationService.GetAvailableJobsLine(TextService, jobs.Count));
        Console.Write(_translationService.GetJobNumberPrompt(TextService));

        string? input = Console.ReadLine();

        if (int.TryParse(input, out int jobNumber) && jobNumber >= 1 && jobNumber <= jobs.Count)
        {
            return jobNumber;
        }

        WriteError(_translationService.GetInvalidJobNumberSelectionMessage(TextService, jobs.Count));
        Pause();

        return -1;
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

    private string FormatPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? _translationService.GetNotConfiguredLabel(TextService)
            : $"<{path}>";
    }

    private void Pause()
    {
        Console.WriteLine(_translationService.GetPauseMessage(TextService));
        Console.ReadKey();
    }

    private static void WriteSuccess(string message)
    {
        WriteColored(message, ConsoleColor.Green);
    }

    private static void WriteError(string message)
    {
        WriteColored(message, ConsoleColor.Red);
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
}
