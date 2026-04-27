using System.Text.Json;

public class LoggerService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public void WriteLog(LogEntry entry)
    {
        string logFilePath = RuntimeStoragePaths.GetDailyLogFilePath(entry.Timestamp);
        string jsonLine = JsonSerializer.Serialize(entry, SerializerOptions);

        File.AppendAllText(logFilePath, jsonLine + Environment.NewLine);
    }
}
