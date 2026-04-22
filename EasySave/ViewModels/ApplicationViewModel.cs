public class ApplicationViewModel
{
    private readonly ArgumentParser _argumentParser;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly BackupController _backupController;
    private readonly StateService _stateService;
    private readonly ApplicationTextService _textService;

    public IReadOnlyList<string> Messages { get; private set; }
    public IReadOnlyList<BackupJob> SelectedJobs { get; private set; }
    public IReadOnlyList<BackupJob> AvailableJobs { get; private set; }

    public bool CanStartBackups => Messages.Count == 0 && SelectedJobs.Count > 0;

    public ApplicationViewModel(
        ArgumentParser argumentParser,
        BackupJobRegistry jobRegistry,
        BackupController backupController,
        StateService stateService,
        ApplicationTextService textService)
    {
        _argumentParser = argumentParser;
        _jobRegistry = jobRegistry;
        _backupController = backupController;
        _stateService = stateService;
        _textService = textService;
        Messages = Array.Empty<string>();
        SelectedJobs = Array.Empty<BackupJob>();
        AvailableJobs = Array.Empty<BackupJob>();
    }

    public void Load(string[] args)
    {
        Messages = Array.Empty<string>();
        SelectedJobs = Array.Empty<BackupJob>();
        AvailableJobs = _jobRegistry.LoadJobs();
        _stateService.SynchronizeConfiguredJobs(AvailableJobs);

        try
        {
            if (args.Length != 1)
            {
                Messages = new[]
                {
                    _textService.GetUsageMessage(),
                    _textService.GetUsageExamples()
                };
                return;
            }

            List<int> selectedJobNumbers = _argumentParser.ParseJobSelection(args[0]);

            if (AvailableJobs.Count == 0)
            {
                Messages = new[] { _textService.GetNoConfiguredJobsMessage() };
                return;
            }

            var selectedJobs = new List<BackupJob>();

            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber > AvailableJobs.Count)
                {
                    Messages = new[] { _textService.GetJobNotConfiguredMessage(jobNumber) };
                    return;
                }

                selectedJobs.Add(AvailableJobs[jobNumber - 1]);
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
