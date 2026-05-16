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
        if (_executionController?.IsBatchRunActive == true)
        {
            throw new InvalidOperationException("Cannot start a manual backup while a batch backup is running.");
        }

        return _backupService.StartBackup(selectedBackupJob);
    }

    public IReadOnlyList<BackupResult> StartBackups(IEnumerable<SelectedBackupJob> backupJobs)
    {
        if (_executionController?.IsAnyJobRunning() == true)
        {
            throw new InvalidOperationException("Cannot start a batch backup while another backup is running.");
        }

        List<SelectedBackupJob> orderedJobs = backupJobs.ToList();
        _executionController?.BeginBatchRun();

        try
        {
            Task<BackupResult>[] tasks = orderedJobs
                .Select(backupJob => Task.Run(() => _backupService.StartBackup(backupJob)))
                .ToArray();

            Task.WaitAll(tasks);

            return tasks
                .Select(task => task.Result)
                .ToList();
        }
        finally
        {
            _executionController?.CompleteBatchRun();
        }
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
        EnsureConfigurationCanChange(jobNumber, includeJobSpecificCheck: false);
        return _jobRegistry.CreateJob(jobNumber, job);
    }

    public BackupJob UpdateJob(int jobNumber, BackupJob job)
    {
        EnsureConfigurationCanChange(jobNumber, includeJobSpecificCheck: true);
        return _jobRegistry.UpdateJob(jobNumber, job);
    }

    public BackupJob DeleteJob(int jobNumber)
    {
        EnsureConfigurationCanChange(jobNumber, includeJobSpecificCheck: true);
        return _jobRegistry.DeleteJob(jobNumber);
    }

    private void EnsureConfigurationCanChange(int jobNumber, bool includeJobSpecificCheck)
    {
        if (_executionController?.IsBatchRunActive == true)
        {
            throw new InvalidOperationException("Cannot change backup jobs while a batch backup is running.");
        }

        if (includeJobSpecificCheck && _executionController?.IsJobRunning(jobNumber) == true)
        {
            throw new InvalidOperationException($"Cannot change backup job {jobNumber} while it is running.");
        }
    }
}
