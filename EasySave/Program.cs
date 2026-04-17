if (args.Length != 1)
{
    Console.WriteLine("Usage: EasySave <job-selection>");
    Console.WriteLine("Examples: EasySave 1-3 | EasySave 1;3 | EasySave 2");
    return;
}

var loggerService = new LoggerService();
var stateService = new StateService();
var backupHistoryService = new BackupHistoryService();
IBackupService backupService = new BackupService(loggerService, stateService, backupHistoryService);
var controller = new BackupController(backupService);
var argumentParser = new ArgumentParser();
var jobRegistry = new BackupJobRegistry();

try
{
    List<int> selectedJobNumbers = argumentParser.ParseJobSelection(args[0]);
    IReadOnlyList<BackupJob> jobs = jobRegistry.LoadJobs();

    if (jobs.Count == 0)
    {
        Console.WriteLine("No backup jobs are configured.");
        return;
    }

    var selectedJobs = new List<BackupJob>();

    foreach (int jobNumber in selectedJobNumbers)
    {
        if (jobNumber > jobs.Count)
        {
            Console.WriteLine($"Job {jobNumber} is not configured in jobs.json.");
            return;
        }

        selectedJobs.Add(jobs[jobNumber - 1]);
    }

    controller.StartBackups(selectedJobs);
}
catch (ArgumentException exception)
{
    Console.WriteLine(exception.Message);
}

