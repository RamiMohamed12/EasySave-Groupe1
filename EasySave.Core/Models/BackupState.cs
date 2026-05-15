
public class BackupState
{
    public string BackupName {get; set;}
    public string CurrentSourcePath {get; set;}
    public string CurrentTargetPath {get; set;}
    public bool IsRunning {get; set;} 
    public BackupExecutionStatus Status { get; set; }
    public string ErrorMessage { get; set; }
    public long CurrentFileSize {get; set;}
    public DateTime LastBackupUpdateTime {get; set;}

    public long TransferredBytes {get; set;}
    public long ProcessedBytes { get; set; }

    public long TotalEligibleFileCount {get; set;}
    public long RemainingFileCount {get; set;}

    public long TotalEligibleBytes {get; set;}
    public long RemainingBytes {get; set;}
    public DateTime? LastRunStartedAt { get; set; }
    public DateTime? LastRunCompletedAt { get; set; }
    public List<BackupTransferredFile> LastRunTransferredFiles { get; set; }
    public bool IsPriorityWorkPending { get; set; }
    public FileTransferPriority CurrentFilePriority { get; set; }
    public bool IsLargeFileTransfer { get; set; }
    public BackupPauseReason PauseReason { get; set; }
    public BackupControlAction RequestedAction { get; set; }
    public string PauseReasonDetails { get; set; }
    public string CurrentPriorityExtension { get; set; }

    public double Progress => TotalEligibleBytes > 0 ? (double)ProcessedBytes / TotalEligibleBytes * 100 : 0;
    public BackupState()
    {
        BackupName = "";
        CurrentSourcePath = "";
        CurrentTargetPath = "";
        IsRunning = false;
        Status = BackupExecutionStatus.Inactive;
        ErrorMessage = string.Empty;
        CurrentFileSize = 0;
        LastBackupUpdateTime = DateTime.MinValue;
        TransferredBytes = 0;
        ProcessedBytes = 0;
        TotalEligibleFileCount = 0;
        RemainingFileCount = 0;
        TotalEligibleBytes = 0;
        RemainingBytes = 0;
        LastRunStartedAt = null;
        LastRunCompletedAt = null;
        LastRunTransferredFiles = new List<BackupTransferredFile>();
        IsPriorityWorkPending = false;
        CurrentFilePriority = FileTransferPriority.Normal;
        IsLargeFileTransfer = false;
        PauseReason = BackupPauseReason.None;
        RequestedAction = BackupControlAction.None;
        PauseReasonDetails = string.Empty;
        CurrentPriorityExtension = string.Empty;
    }
    
}
