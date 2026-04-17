using System.Text.Json;

public class BackupHistoryService
{
    private readonly string _historyFilePath;

    public BackupHistoryService()
    {
        _historyFilePath = Path.Combine(RuntimeStoragePaths.BackupStateDirectory, "backup-history.json");
    }

    public DateTime? GetLastFullBackupUtc(string backupName)
    {
        Dictionary<string, DateTime> history = LoadHistory();

        return history.TryGetValue(backupName, out DateTime timestampUtc)
            ? timestampUtc
            : null;
    }

    public void SetLastFullBackupUtc(string backupName, DateTime timestampUtc)
    {
        Dictionary<string, DateTime> history = LoadHistory();
        history[backupName] = timestampUtc;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(history, options);
        File.WriteAllText(_historyFilePath, json);
    }

    private Dictionary<string, DateTime> LoadHistory()
    {
        if (!File.Exists(_historyFilePath))
        {
            return new Dictionary<string, DateTime>();
        }

        string json = File.ReadAllText(_historyFilePath);
        Dictionary<string, DateTime>? history = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);

        return history ?? new Dictionary<string, DateTime>();
    }
}