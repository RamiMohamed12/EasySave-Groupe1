public class MenuManager
{
    private readonly ApplicationTextService _textService;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly ArgumentParser _argumentParser;
    private readonly BackupController _backupController;
    private readonly StateService _stateService;
    private readonly ConsoleApplicationView _view;

    public MenuManager(
        ApplicationTextService textService,
        BackupJobRegistry jobRegistry,
        ArgumentParser argumentParser,
        BackupController backupController,
        StateService stateService,
        ConsoleApplicationView view)
    {
        _textService = textService;
        _jobRegistry = jobRegistry;
        _argumentParser = argumentParser;
        _backupController = backupController;
        _stateService = stateService;
        _view = view;
    }

    public void Start()
    {
        while (true)
        {
            Console.Clear();
            DisplayMainMenu();

            Console.Write("\n> ");
            string? choice = Console.ReadLine();

            switch (choice?.ToLower())
            {
                case "1":
                    ViewJobs();
                    break;
                case "2":
                    ConfigureJobSource();
                    break;
                case "3":
                    ConfigureJobTarget();
                    break;
                case "4":
                    RunBackups();
                    break;
                case "5":
                    return;
                default:
                    WriteError(_textService.GetInvalidCommandMessage());
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private void DisplayMainMenu()
    {
        string title = _textService.GetConfiguredJobsHeader();
        Console.WriteLine("+------------------------------------+");
        Console.WriteLine($"| {title.PadRight(34)} |");
        Console.WriteLine("+------------------------------------+");
        Console.WriteLine("| 1. View Jobs                       |");
        Console.WriteLine("| 2. Configure Source Path           |");
        Console.WriteLine("| 3. Configure Target Path           |");
        Console.WriteLine("| 4. Run Backups                     |");
        Console.WriteLine("| 5. Exit                            |");
        Console.WriteLine("+------------------------------------+");
    }

    private void ViewJobs()
    {
        Console.Clear();
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        RenderJobs(jobs);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void ConfigureJobSource()
    {
        Console.Clear();
        Console.WriteLine("Configure Job Source");
        Console.WriteLine("====================\n");

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        Console.Write("Enter source path: ");
        string? sourcePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            WriteError("Invalid path!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            BackupJob updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Source, sourcePath);
            RenderConfigurationSuccess(jobNumber, updatedJob, JobPathField.Source);
        }
        catch (Exception ex)
        {
            WriteError($"Error: {ex.Message}");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void ConfigureJobTarget()
    {
        Console.Clear();
        Console.WriteLine("Configure Job Target");
        Console.WriteLine("====================\n");

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        Console.Write("Enter target path: ");
        string? targetPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            WriteError("Invalid path!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            BackupJob updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Target, targetPath);
            RenderConfigurationSuccess(jobNumber, updatedJob, JobPathField.Target);
        }
        catch (Exception ex)
        {
            WriteError($"Error: {ex.Message}");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void RunBackups()
    {
        Console.Clear();
        Console.WriteLine("Run Backups");
        Console.WriteLine("===========\n");
        Console.WriteLine("Enter job selection:");
        Console.WriteLine("  - Single job: 1");
        Console.WriteLine("  - Range: 1-3");
        Console.WriteLine("  - Multiple: 1;3;5");
        Console.WriteLine();

        Console.Write("> ");
        string? selection = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(selection))
        {
            WriteError("No selection provided!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            var selectedJobNumbers = _argumentParser.ParseJobSelection(selection);
            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            _stateService.SynchronizeConfiguredJobs(jobs);

            var selectedJobs = new List<SelectedBackupJob>();
            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber >= 1 && jobNumber <= jobs.Count)
                {
                    selectedJobs.Add(new SelectedBackupJob { JobNumber = jobNumber, Job = jobs[jobNumber - 1] });
                }
            }

            if (selectedJobs.Count == 0)
            {
                WriteError("No valid jobs selected!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nRunning {selectedJobs.Count} backup job(s)...\n");

            IReadOnlyList<BackupResult> results = _backupController.StartBackups(selectedJobs);

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
            WriteError($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private int GetJobNumber()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();

        Console.WriteLine($"Available jobs: 1-{jobs.Count}");
        Console.Write("Enter job number: ");

        string? input = Console.ReadLine();

        if (int.TryParse(input, out int jobNumber) && jobNumber >= 1 && jobNumber <= jobs.Count)
        {
            return jobNumber;
        }

        WriteError("Invalid job number!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();

        return -1;
    }

    private void RenderConfigurationSuccess(int jobNumber, BackupJob updatedJob, JobPathField pathField)
    {
        string fieldName = GetConfigurationFieldName(pathField);
        string pathValue = pathField == JobPathField.Source ? updatedJob.Source : updatedJob.Target;

        WriteSuccess(GetConfigurationSuccessMessage(jobNumber, fieldName, pathValue));
        Console.WriteLine();
        Console.WriteLine(_textService.GetConfiguredJobsHeader());
        RenderJob(jobNumber, updatedJob);
    }

    private void RenderJobs(IReadOnlyList<BackupJob> jobs)
    {
        Console.WriteLine(_textService.GetConfiguredJobsHeader());
        Console.WriteLine();

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            RenderJob(index + 1, job);
        }
    }

    private void RenderJob(int jobNumber, BackupJob job)
    {
        Console.WriteLine(_textService.GetJobSummaryLine(jobNumber, job));
        Console.WriteLine($"{GetSourceLabel()}{FormatPath(job.Source)}");
        Console.WriteLine($"{GetTargetLabel()}{FormatPath(job.Target, GetNotConfiguredLabel())}");
        Console.WriteLine(_textService.GetJobTypeLine(job.Type));
        Console.WriteLine(_textService.GetJobConfigurationStatusLine(job));
        Console.WriteLine();
    }

    private string BuildBackupSuccessMessage(BackupResult result, bool showHeader)
    {
        string details = _textService.FormatBackupResult(result);
        string successMessage = _textService.GetBackupSuccessMessage();

        if (!showHeader)
        {
            return $"{details}\n{successMessage}";
        }

        return $"{GetJobHeader(result)}\n{details}\n{successMessage}";
    }

    private string BuildBackupErrorMessage(BackupResult result, bool showHeader)
    {
        string details = _textService.FormatBackupResult(result);
        return showHeader
            ? $"{GetJobHeader(result)}\n{details}"
            : details;
    }

    private string GetJobHeader(BackupResult result)
    {
        return IsFrench()
            ? $"Tache {result.JobNumber} : {result.BackupName}"
            : $"Job {result.JobNumber}: {result.BackupName}";
    }

    private string GetConfigurationFieldName(JobPathField pathField)
    {
        if (pathField == JobPathField.Source)
        {
            return "source";
        }

        return IsFrench() ? "cible" : "target";
    }

    private string GetConfigurationSuccessMessage(int jobNumber, string fieldName, string pathValue)
    {
        return IsFrench()
            ? $"La tache {jobNumber} a ete mise a jour : {fieldName} = {FormatPath(pathValue)}"
            : $"Job {jobNumber} was updated: {fieldName} = {FormatPath(pathValue)}";
    }

    private string GetSourceLabel()
    {
        return IsFrench() ? "  Source : " : "  Source: ";
    }

    private string GetTargetLabel()
    {
        return IsFrench() ? "  Cible : " : "  Target: ";
    }

    private string GetNotConfiguredLabel()
    {
        return IsFrench() ? "<non configure>" : "<not configured>";
    }

    private bool IsFrench()
    {
        return string.Equals(
            _textService.GetLanguageCode(),
            ApplicationTextService.FrenchLanguageCode,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatPath(string path, string emptyValue = "<non configure>")
    {
        return string.IsNullOrWhiteSpace(path)
            ? emptyValue
            : $"<{path}>";
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