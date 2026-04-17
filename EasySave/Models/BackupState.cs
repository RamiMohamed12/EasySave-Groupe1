
public class BackupState
{
    public string BackupName {get; set;}
    public string CurrentSourcePath {get; set;}
    public string CurrentTargetPath {get; set;}
    public bool IsRunning {get; set;} 
    public long CurrentFileSize {get; set;}
    public DateTime LastBackupUpdateTime {get; set;}

    public long TransferredBytes {get; set;}

    public long TotalEligibleFileCount {get; set;}
    public long RemainingFileCount {get; set;}

    public long TotalEligibleBytes {get; set;}
    public long RemainingBytes {get; set;}

    public double Progress => TotalEligibleBytes > 0 ? (double)TransferredBytes / TotalEligibleBytes * 100 : 0;
    public BackupState()
    {
        BackupName = "";
        CurrentSourcePath = "";
        CurrentTargetPath = "";
        IsRunning = false;
        CurrentFileSize = 0;
        LastBackupUpdateTime = DateTime.MinValue;
        TransferredBytes = 0;
        TotalEligibleFileCount = 0;
        RemainingFileCount = 0;
        TotalEligibleBytes = 0;
        RemainingBytes = 0;
    }
    
}