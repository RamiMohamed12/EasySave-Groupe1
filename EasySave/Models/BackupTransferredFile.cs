public class BackupTransferredFile
{
    public DateTime Timestamp { get; set; }
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public long FileSizeBytes { get; set; }
    public long TransferTimeMilliseconds { get; set; }

    public BackupTransferredFile()
    {
        Timestamp = DateTime.MinValue;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        FileSizeBytes = 0;
        TransferTimeMilliseconds = 0;
    }
}
