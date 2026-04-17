using System.Text.Json;

public class LoggerService
{
    public void WriteLog(LogEntry entry)
    {
        string logFilePath = RuntimeStoragePaths.GetDailyLogFilePath(entry.Timestamp);
        string jsonLine = JsonSerializer.Serialize(entry);

        File.AppendAllText(logFilePath, jsonLine + Environment.NewLine);
    }
}