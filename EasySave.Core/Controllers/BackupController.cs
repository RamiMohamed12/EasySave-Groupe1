public class BackupController
{
    private readonly IBackupService _backupService;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly IBackupExecutionController? _executionController;

    public BackupController(IBackupService backupService)
        : this(
            backupService,
            new BackupJobRegistry(),
            backupService is BackupService concreteBackupService ? concreteBackupService.ExecutionController : null)
    {
    }

    public BackupController(
        IBackupService backupService,
        BackupJobRegistry jobRegistry,
        IBackupExecutionController? executionController = null)
    {
        _backupService = backupService;
        _jobRegistry = jobRegistry;
        _executionController = executionController;
    }

    public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
    {
        return _backupService.StartBackup(selectedBackupJob);
    }

    public IReadOnlyList<BackupResult> StartBackups(IEnumerable<SelectedBackupJob> backupJobs)
    {
        List<SelectedBackupJob> orderedJobs = backupJobs.ToList();
        Task<BackupResult>[] tasks = orderedJobs
            .Select(backupJob => Task.Run(() => _backupService.StartBackup(backupJob)))
            .ToArray();

        Task.WaitAll(tasks);

        return tasks
            .Select(task => task.Result)
            .ToList();
    }

    public void PauseJob(int jobNumber)
    {
        _executionController?.RequestPause(jobNumber);
    }

    public void ResumeJob(int jobNumber)
    {
        _executionController?.RequestResume(jobNumber);
    }

    public void StopJob(int jobNumber)
    {
        _executionController?.RequestStop(jobNumber);
    }

    public BackupJob CreateJob(int jobNumber, BackupJob job)
    {
        return _jobRegistry.CreateJob(jobNumber, job);
    }

    public BackupJob UpdateJob(int jobNumber, BackupJob job)
    {
        return _jobRegistry.UpdateJob(jobNumber, job);
    }

    public BackupJob DeleteJob(int jobNumber)
    {
        return _jobRegistry.DeleteJob(jobNumber);
    }
}
