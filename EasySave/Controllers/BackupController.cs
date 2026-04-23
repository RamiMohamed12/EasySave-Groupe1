public class BackupController
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public void StartBackup(SelectedBackupJob selectedBackupJob)
    {
        _backupService.StartBackup(selectedBackupJob);
    }

    public void StartBackups(IEnumerable<SelectedBackupJob> backupJobs)
    {
        foreach (SelectedBackupJob backupJob in backupJobs)
        {
            _backupService.StartBackup(backupJob);
        }
    }
}
