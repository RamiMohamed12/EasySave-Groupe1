public class BackupController
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
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
            results.Add(_backupService.StartBackup(backupJob));
        }

        return results;
    }
}
