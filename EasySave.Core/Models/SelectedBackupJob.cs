public class SelectedBackupJob
{
    public int JobNumber { get; set; }
    public BackupJob Job { get; set; }

    public SelectedBackupJob()
    {
        JobNumber = 0;
        Job = new BackupJob();
    }
}
