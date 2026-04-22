public class ApplicationViewModel
{
    private readonly ArgumentParser _argumentParser;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly BackupController _backupController;

    public IReadOnlyList<string> Messages { get; private set; }
    public IReadOnlyList<BackupJob> SelectedJobs { get; private set; }

    public bool CanStartBackups => Messages.Count == 0 && SelectedJobs.Count > 0;

    public ApplicationViewModel(
        ArgumentParser argumentParser,
        BackupJobRegistry jobRegistry,
        BackupController backupController)
    {
        _argumentParser = argumentParser;
        _jobRegistry = jobRegistry;
        _backupController = backupController;
        Messages = Array.Empty<string>();
        SelectedJobs = Array.Empty<BackupJob>();
    }

    public void Load(string[] args)
    {
        Messages = Array.Empty<string>();
        SelectedJobs = Array.Empty<BackupJob>();

        try
        {
            if (args.Length != 1)
            {
                Messages = new[]
                {
                    "Usage: EasySave <job-selection>",
                    "Examples: EasySave 1-3 | EasySave 1;3 | EasySave 2"
                };
                return;
            }

            List<int> selectedJobNumbers = _argumentParser.ParseJobSelection(args[0]);
            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();

            if (jobs.Count == 0)
            {
                Messages = new[] { "No backup jobs are configured." };
                return;
            }

            var selectedJobs = new List<BackupJob>();

            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber > jobs.Count)
                {
                    Messages = new[] { $"Job {jobNumber} is not configured in jobs.json." };
                    return;
                }

                selectedJobs.Add(jobs[jobNumber - 1]);
            }

            SelectedJobs = selectedJobs;
        }
        catch (ArgumentException exception)
        {
            Messages = new[] { exception.Message };
        }
    }

    public void StartBackups()
    {
        if (!CanStartBackups)
        {
            return;
        }

        _backupController.StartBackups(SelectedJobs);
    }
}
