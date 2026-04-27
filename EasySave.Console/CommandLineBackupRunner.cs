public class CommandLineBackupRunner
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private readonly Func<string?, ConsoleMenuRuntime> _runtimeFactory;

    public CommandLineBackupRunner(
        BackupJobRegistry jobRegistry,
        StateService stateService,
        Func<string?, ConsoleMenuRuntime> runtimeFactory)
    {
        _jobRegistry = jobRegistry;
        _stateService = stateService;
        _runtimeFactory = runtimeFactory;
    }

    public void Run(string[] args)
    {
        ConsoleMenuRuntime runtime = _runtimeFactory(null);
        string selection = BuildSelection(args);

        try
        {
            IReadOnlyList<int> selectedJobNumbers = runtime.ArgumentParser.ParseJobSelection(selection);
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
                WriteError(runtime.TextService.GetNoConfiguredJobsMessage());
                return;
            }

            IReadOnlyList<BackupResult> results = runtime.BackupController.StartBackups(selectedJobs);

            for (int index = 0; index < results.Count; index++)
            {
                BackupResult result = results[index];
                bool showHeader = results.Count > 1;

                if (result.Status == BackupExecutionStatus.Finished)
                {
                    WriteSuccess(BuildBackupSuccessMessage(runtime.TextService, result, showHeader));
                }
                else
                {
                    WriteError(BuildBackupErrorMessage(runtime.TextService, result, showHeader));
                }

                if (index < results.Count - 1)
                {
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            WriteError(BuildErrorMessage(runtime.TextService, ex.Message));
        }
    }

    private static string BuildSelection(string[] args)
    {
        return string.Concat(args).Replace(" ", string.Empty);
    }

    private static string BuildBackupSuccessMessage(ApplicationTextService textService, BackupResult result, bool showHeader)
    {
        string details = textService.FormatBackupResult(result);
        string successMessage = textService.GetBackupSuccessMessage();

        if (!showHeader)
        {
            return $"{details}\n{successMessage}";
        }

        return $"{GetJobHeader(textService, result)}\n{details}\n{successMessage}";
    }

    private static string BuildBackupErrorMessage(ApplicationTextService textService, BackupResult result, bool showHeader)
    {
        string details = textService.FormatBackupResult(result);
        return showHeader
            ? $"{GetJobHeader(textService, result)}\n{details}"
            : details;
    }

    private static string GetJobHeader(ApplicationTextService textService, BackupResult result)
    {
        bool isFrench = string.Equals(
            textService.GetLanguageCode(),
            ApplicationTextService.FrenchLanguageCode,
            StringComparison.OrdinalIgnoreCase);

        return isFrench
            ? $"Tache {result.JobNumber} : {result.BackupName}"
            : $"Job {result.JobNumber}: {result.BackupName}";
    }

    private static string BuildErrorMessage(ApplicationTextService textService, string details)
    {
        bool isFrench = string.Equals(
            textService.GetLanguageCode(),
            ApplicationTextService.FrenchLanguageCode,
            StringComparison.OrdinalIgnoreCase);

        return isFrench
            ? $"Erreur : {details}"
            : $"Error: {details}";
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
}
