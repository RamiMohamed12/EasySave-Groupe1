public interface IBackupExecutionController
{
    void BeginJobRun(int jobNumber);
    void BeginBatchRun();
    void RequestPause(int jobNumber);
    void RequestResume(int jobNumber);
    void RequestStop(int jobNumber);
    void RequestAutomaticPause(int jobNumber, string reasonDetails);
    BackupExecutionCommandState GetCommandState(int jobNumber);
    bool IsJobRunning(int jobNumber);
    bool IsAnyJobRunning();
    bool IsBatchRunActive { get; }
    void CompleteJob(int jobNumber);
    void CompleteBatchRun();
}
