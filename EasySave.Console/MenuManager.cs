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
                    Console.WriteLine(_textService.GetInvalidCommandMessage());
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private void DisplayMainMenu()
    {
        string title = _textService.GetConfiguredJobsHeader();
        Console.WriteLine("╔════════════════════════════════════╗");
        Console.WriteLine($"║ {title.PadRight(34)} ║");
        Console.WriteLine("╠════════════════════════════════════╣");
        Console.WriteLine("║ 1. View Jobs                       ║");
        Console.WriteLine("║ 2. Configure Source Path           ║");
        Console.WriteLine("║ 3. Configure Target Path           ║");
        Console.WriteLine("║ 4. Run Backups                     ║");
        Console.WriteLine("║ 5. Exit                            ║");
        Console.WriteLine("╚════════════════════════════════════╝");
    }

    private void ViewJobs()
    {
        Console.Clear();
        var jobs = _jobRegistry.LoadJobs();

        Console.WriteLine(_textService.GetConfiguredJobsHeader());
        Console.WriteLine();

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            Console.WriteLine($"Job {index + 1}:");
            Console.WriteLine(_textService.GetJobSourceLine(job.Source));
            Console.WriteLine(_textService.GetJobTargetLine(job.Target));
            Console.WriteLine(_textService.GetJobTypeLine(job.Type));
            Console.WriteLine(_textService.GetJobConfigurationStatusLine(job));
            Console.WriteLine();
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private void ConfigureJobSource()
    {
        Console.Clear();
        Console.WriteLine("Configure Job Source");
        Console.WriteLine("====================\n");

        int jobNumber = GetJobNumber();
        if (jobNumber == -1) return;

        Console.Write("Enter source path: ");
        string? sourcePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.WriteLine("Invalid path!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Source, sourcePath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Source path updated successfully!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
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
        if (jobNumber == -1) return;

        Console.Write("Enter target path: ");
        string? targetPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            Console.WriteLine("Invalid path!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Target, targetPath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Target path updated successfully!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
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
            Console.WriteLine("No selection provided!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            var selectedJobNumbers = _argumentParser.ParseJobSelection(selection);
            var jobs = _jobRegistry.LoadJobs();
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
                Console.WriteLine("No valid jobs selected!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nRunning {selectedJobs.Count} backup job(s)...\n");

            var results = _backupController.StartBackups(selectedJobs);

            foreach (var result in results)
            {
                if (result.Status == BackupExecutionStatus.Finished)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Job {result.JobNumber} ({result.BackupName}): Completed");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Job {result.JobNumber} ({result.BackupName}): Failed - {result.ErrorMessage}");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private int GetJobNumber()
    {
        var jobs = _jobRegistry.LoadJobs();

        Console.WriteLine($"Available jobs: 1-{jobs.Count}");
        Console.Write("Enter job number: ");

        string? input = Console.ReadLine();

        if (int.TryParse(input, out int jobNumber) && jobNumber >= 1 && jobNumber <= jobs.Count)
        {
            return jobNumber;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Invalid job number!");
        Console.ResetColor();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();

        return -1;
    }
}
