public class BackupController
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public void StartBackup(BackupJob backupJob)
    {
        _backupService.StartBackup(backupJob);
    }

    public void StartBackups(IEnumerable<BackupJob> backupJobs)
    {
        foreach (BackupJob backupJob in backupJobs)
        {
            _backupService.StartBackup(backupJob);
        }
    }
}