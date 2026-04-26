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

    public void ConfigureJobSource()
    {
        Console.Clear();
        WriteSectionHeader(_translationService.GetConfigureSourceLabel(TextService));

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        Console.Write(_translationService.GetSourcePathPrompt(TextService));
        string? sourcePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            WriteError(TextService.GetPathValueRequiredMessage());
            Pause();
            return;
        }

        try
        {
            BackupJob updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Source, sourcePath);
            RenderConfigurationSuccess(jobNumber, updatedJob, JobPathField.Source);
        }
        catch (Exception ex)
        {
            WriteError(_translationService.BuildErrorMessage(TextService, ex.Message));
        }

        Pause();
    }

    public void ConfigureJobTarget()
    {
        Console.Clear();
        WriteSectionHeader(_translationService.GetConfigureTargetLabel(TextService));

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        Console.Write(_translationService.GetTargetPathPrompt(TextService));
        string? targetPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            WriteError(TextService.GetPathValueRequiredMessage());
            Pause();
            return;
        }

        try
        {
            BackupJob updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Target, targetPath);
            RenderConfigurationSuccess(jobNumber, updatedJob, JobPathField.Target);
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

    private void RenderConfigurationSuccess(int jobNumber, BackupJob updatedJob, JobPathField pathField)
    {
        WriteSuccess(_translationService.GetConfigurationSuccessMessage(TextService, jobNumber, updatedJob, pathField));
        Console.WriteLine();
        Console.WriteLine(TextService.GetConfiguredJobsHeader());
        RenderJob(jobNumber, updatedJob);
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
