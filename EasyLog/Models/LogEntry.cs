public class LogEntry
{
    public DateTime Timestamp { get; set; }

    public string BackupName { get; set; }

    public string SourcePath { get; set; }

    public string DestinationPath { get; set; }

    public string ActionType { get; set; }

    public string ErrorMessage { get; set; }

    // One log entry represents one copied file, so this stores a single file size.
    public long FileSizeBytes { get; set; }

    // Keep the unit in the name because the spec requires milliseconds.
    public long TransferTimeMilliseconds { get; set; }

    // 0 means no encryption, positive values are encryption time, negative values are CryptoSoft errors.
    public long EncryptionTimeMilliseconds { get; set; }

    public LogEntry()
    {
        Timestamp = DateTime.MinValue;
        BackupName = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        ActionType = string.Empty;
        ErrorMessage = string.Empty;
        FileSizeBytes = 0;
        TransferTimeMilliseconds = 0;
        EncryptionTimeMilliseconds = 0;
    }
}
