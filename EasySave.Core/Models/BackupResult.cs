public class BackupResult
{
    public int JobNumber { get; set; }
    public string BackupName { get; set; }
    public BackupExecutionStatus Status { get; set; }
    public int TransferredFileCount { get; set; }
    public long TransferredBytes { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public bool StoppedByBusinessSoftware { get; set; }
    public string BlockingProcessName { get; set; }

    public BackupResult()
    {
        JobNumber = 0;
        BackupName = string.Empty;
        Status = BackupExecutionStatus.Inactive;
        TransferredFileCount = 0;
        TransferredBytes = 0;
        ErrorMessage = string.Empty;
        ElapsedTime = TimeSpan.Zero;
        StoppedByBusinessSoftware = false;
        BlockingProcessName = string.Empty;
    }
}
