public interface IBackupProgressReporter
{
    void ReportJobStarted(SelectedBackupJob selectedBackupJob, BackupState state);
    void ReportFileCopied(SelectedBackupJob selectedBackupJob, BackupState state, BackupTransferredFile transferredFile);
    void ReportJobCompleted(SelectedBackupJob selectedBackupJob, BackupState state, TimeSpan elapsedTime);
    void ReportFileSkipped(SelectedBackupJob selectedBackupJob, string filePath, string reason);
    void ReportSourceDirectoryMissing(SelectedBackupJob selectedBackupJob);
}
