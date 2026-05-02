public class BackupController
{
    private readonly IBackupService _backupService;
    private readonly BackupJobRegistry _jobRegistry;

    public BackupController(IBackupService backupService)
        : this(backupService, new BackupJobRegistry())
    {
    }

    public BackupController(IBackupService backupService, BackupJobRegistry jobRegistry)
    {
        _backupService = backupService;
        _jobRegistry = jobRegistry;
    }

    public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
    {
        return _backupService.StartBackup(selectedBackupJob);
    }

    public IReadOnlyList<BackupResult> StartBackups(IEnumerable<SelectedBackupJob> backupJobs)
    {
        var results = new List<BackupResult>();

        foreach (SelectedBackupJob backupJob in backupJobs)
        {
            BackupResult result = _backupService.StartBackup(backupJob);
            results.Add(result);

            if (result.StoppedByBusinessSoftware)
            {
                break;
            }
        }

        return results;
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
