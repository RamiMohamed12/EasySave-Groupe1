public class ApplicationViewModel
{
    private readonly ArgumentParser _argumentParser;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly BackupController _backupController;
    private readonly StateService _stateService;
    private ApplicationTextService _textService;

    public IReadOnlyList<string> Messages { get; private set; }
    public IReadOnlyList<SelectedBackupJob> SelectedJobs { get; private set; }
    public IReadOnlyList<BackupJob> AvailableJobs { get; private set; }
    public bool ShowHelp { get; private set; }
    public bool ShowJobList { get; private set; }
    public int? ConfiguredJobNumber { get; private set; }
    public bool IsConfigurationMessage { get; private set; }
    public bool IsBackupResultMessage { get; private set; }
    public ApplicationTextService TextService => _textService;

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
        ConfiguredJobNumber = null;
        IsConfigurationMessage = false;
        IsBackupResultMessage = false;
    }

    public void Load(string[] args)
    {
        Messages = Array.Empty<string>();
        SelectedJobs = Array.Empty<SelectedBackupJob>();
        AvailableJobs = _jobRegistry.LoadJobs();
        _stateService.SynchronizeConfiguredJobs(AvailableJobs);
        ShowHelp = false;
        ShowJobList = false;
        ConfiguredJobNumber = null;
        IsConfigurationMessage = false;
        IsBackupResultMessage = false;

        if (AvailableJobs.Count == 0)
        {
            Messages = new[] { _textService.GetNoConfiguredJobsMessage() };
            return;
        }

        try
        {
            CliCommand command = _argumentParser.Parse(args);

            if (command.Type == CliCommandType.ShowHelp)
            {
                ShowHelp = true;
                return;
            }

            if (command.Type == CliCommandType.ShowJobs)
            {
                ShowJobList = true;
                return;
            }

            if (command.Type == CliCommandType.ConfigureJobPath)
            {
                BackupJob updatedJob = _jobRegistry.UpdateJobPath(command.JobNumber, command.PathField!.Value, command.PathValue);
                AvailableJobs = _jobRegistry.LoadJobs();
                _stateService.SynchronizeConfiguredJobs(AvailableJobs);
                Messages = new[]
                {
                    _textService.GetJobPathUpdatedMessage(command.JobNumber, updatedJob, command.PathField.Value)
                };
                ShowJobList = true;
                ConfiguredJobNumber = command.JobNumber;
                IsConfigurationMessage = true;
                return;
            }

            if (command.Type == CliCommandType.ConfigureStorageDirectory)
            {
                RuntimeStoragePaths.SetStorageDirectory(command.PathValue);
                var jobRegistry = new BackupJobRegistry();
                var stateService = new StateService();
                AvailableJobs = jobRegistry.LoadJobs();
                stateService.SynchronizeConfiguredJobs(AvailableJobs);
                Messages = new[]
                {
                    _textService.GetStorageDirectoryUpdatedMessage(RuntimeStoragePaths.BackupStateDirectory)
                };
                ShowJobList = true;
                return;
            }

            if (command.Type == CliCommandType.ConfigureLanguage)
            {
                RuntimeStoragePaths.SetLanguageCode(command.LanguageCode);
                _textService = ApplicationTextService.Create(command.LanguageCode);
                AvailableJobs = _jobRegistry.LoadJobs();
                _stateService.SynchronizeConfiguredJobs(AvailableJobs);
                Messages = new[]
                {
                    _textService.GetLanguageUpdatedMessage()
                };
                ShowJobList = true;
                IsConfigurationMessage = true;
                return;
            }

            SelectedJobs = BuildSelectedJobs(command.SelectedJobNumbers);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
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

        IReadOnlyList<BackupResult> results = _backupController.StartBackups(SelectedJobs);
        var messages = new List<string>(results.Select(_textService.FormatBackupResult));
        if (results.All(result => result.Status == BackupExecutionStatus.Finished))
        {
            messages.Add(_textService.GetBackupSuccessMessage());
        }

        Messages = messages;
        IsBackupResultMessage = true;
    }

    private IReadOnlyList<SelectedBackupJob> BuildSelectedJobs(IEnumerable<int> selectedJobNumbers)
    {
        var selectedJobs = new List<SelectedBackupJob>();

        foreach (int jobNumber in selectedJobNumbers)
        {
            if (jobNumber > AvailableJobs.Count)
            {
                throw new ArgumentException(_textService.GetJobNotConfiguredMessage(jobNumber));
            }

            selectedJobs.Add(new SelectedBackupJob
            {
                JobNumber = jobNumber,
                Job = AvailableJobs[jobNumber - 1]
            });
        }

        return selectedJobs;
    }
}
