public class TransferWorkItem
{
    public int JobNumber { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public long FileSizeBytes { get; set; }
    public FileTransferPriority Priority { get; set; }
    public string MatchedPriorityExtension { get; set; }
    public int PriorityRank { get; set; }

    public bool IsLargeFile => FileSizeBytes > 0
        && RuntimeStoragePaths.GetLargeFileThresholdKb() > 0
        && FileSizeBytes > RuntimeStoragePaths.GetLargeFileThresholdKb() * 1024L;

    public TransferWorkItem()
    {
        JobNumber = 0;
        BackupName = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        FileSizeBytes = 0;
        Priority = FileTransferPriority.Normal;
        MatchedPriorityExtension = string.Empty;
        PriorityRank = int.MaxValue;
    }
}
