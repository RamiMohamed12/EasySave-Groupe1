public class ApplicationViewModel
{
    private readonly ArgumentParser _argumentParser;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly BackupController _backupController;
    private readonly StateService _stateService;
    private readonly ApplicationTextService _textService;

    public IReadOnlyList<string> Messages { get; private set; }
    public IReadOnlyList<SelectedBackupJob> SelectedJobs { get; private set; }
    public IReadOnlyList<BackupJob> AvailableJobs { get; private set; }
    public bool ShowHelp { get; private set; }
    public bool ShowJobList { get; private set; }

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
        SelectedJobs = Array.Empty<SelectedBackupJob>();
        AvailableJobs = Array.Empty<BackupJob>();
        ShowHelp = false;
        ShowJobList = false;
    }

    public void Load(string[] args)
    {
        Messages = Array.Empty<string>();
        SelectedJobs = Array.Empty<SelectedBackupJob>();
        AvailableJobs = _jobRegistry.LoadJobs();
        _stateService.SynchronizeConfiguredJobs(AvailableJobs);
        ShowHelp = false;
        ShowJobList = false;

        if (AvailableJobs.Count == 0)
        {
            Messages = new[] { _textService.GetNoConfiguredJobsMessage() };
            return;
        }

        try
        {
            if (args.Length == 0)
            {
                ShowHelp = true;
                ShowJobList = true;
                return;
            }

            if (args.Length != 1)
            {
                Messages = new[] { _textService.GetSingleArgumentExpectedMessage() };
                ShowHelp = true;
                ShowJobList = true;
                return;
            }

            if (IsHelpArgument(args[0]))
            {
                ShowHelp = true;
                ShowJobList = true;
                return;
            }

            List<int> selectedJobNumbers = _argumentParser.ParseJobSelection(args[0]);
            var selectedJobs = new List<SelectedBackupJob>();

            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber > AvailableJobs.Count)
                {
                    Messages = new[] { _textService.GetJobNotConfiguredMessage(jobNumber) };
                    ShowHelp = true;
                    ShowJobList = true;
                    return;
                }

                selectedJobs.Add(new SelectedBackupJob
                {
                    JobNumber = jobNumber,
                    Job = AvailableJobs[jobNumber - 1]
                });
            }

            SelectedJobs = selectedJobs;
        }
        catch (ArgumentException exception)
        {
            Messages = new[] { exception.Message };
            ShowHelp = true;
            ShowJobList = true;
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

    private static bool IsHelpArgument(string argument)
    {
        return argument.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("-h", StringComparison.OrdinalIgnoreCase);
    }
}
