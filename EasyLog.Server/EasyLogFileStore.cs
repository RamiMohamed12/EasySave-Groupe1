using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed partial class EasyLogFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly string _logsDirectory;

    public EasyLogFileStore(string logsDirectory)
    {
        _logsDirectory = string.IsNullOrWhiteSpace(logsDirectory) ? "logs" : logsDirectory;
        Directory.CreateDirectory(_logsDirectory);
    }

    public async Task AppendAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        string logFilePath = GetLogFilePath(entry.Timestamp);
        string line = JsonSerializer.Serialize(entry, JsonOptions);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(logFilePath, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<string?> ReadDailyLogAsync(string date, CancellationToken cancellationToken = default)
    {
        if (!IsValidDate(date))
        {
            return null;
        }

        string logFilePath = Path.Combine(_logsDirectory, $"{date}.jsonl");
        if (!File.Exists(logFilePath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(logFilePath, cancellationToken).ConfigureAwait(false);
    }

    public static bool IsValid(LogEntry? entry, out string errorMessage)
    {
        if (entry is null)
        {
            errorMessage = "Log entry is required.";
            return false;
        }

        if (entry.Timestamp == DateTime.MinValue)
        {
            errorMessage = "Timestamp is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.ClientId))
        {
            errorMessage = "ClientId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.UserName))
        {
            errorMessage = "UserName is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.MachineName))
        {
            errorMessage = "MachineName is required.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private string GetLogFilePath(DateTime timestamp)
    {
        return Path.Combine(_logsDirectory, $"{timestamp:yyyy-MM-dd}.jsonl");
    }

    private static bool IsValidDate(string date)
    {
        return DailyLogDateRegex().IsMatch(date)
            && DateTime.TryParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
    }

    [GeneratedRegex("^\\d{4}-\\d{2}-\\d{2}$")]
    private static partial Regex DailyLogDateRegex();
}
