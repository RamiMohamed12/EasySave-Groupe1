public class LogEntry
{
    public DateTime Timestamp { get; set; }

    public string BackupName { get; set; }

    public string SourcePath { get; set; }

    public string DestinationPath { get; set; }

    // One log entry represents one copied file, so this stores a single file size.
    public long FileSizeBytes { get; set; }

    // Keep the unit in the name because the spec requires milliseconds.
    public long TransferTimeMilliseconds { get; set; }

    public LogEntry()
    {
        Timestamp = DateTime.MinValue;
        BackupName = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        FileSizeBytes = 0;
        TransferTimeMilliseconds = 0;
    }
}