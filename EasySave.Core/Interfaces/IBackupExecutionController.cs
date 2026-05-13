public interface IBackupExecutionController
{
    void BeginJobRun(int jobNumber);
    void RequestPause(int jobNumber);
    void RequestResume(int jobNumber);
    void RequestStop(int jobNumber);
    void RequestAutomaticPause(int jobNumber, string reasonDetails);
    BackupExecutionCommandState GetCommandState(int jobNumber);
    void CompleteJob(int jobNumber);
}
