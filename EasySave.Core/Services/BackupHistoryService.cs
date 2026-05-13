using System.Text.Json;

public class BackupHistoryService
{
    private readonly string _historyFilePath;
    private readonly AtomicRuntimeFileStore _fileStore;
    private readonly JsonSerializerOptions _serializerOptions;

    public BackupHistoryService()
    {
        _historyFilePath = RuntimeStoragePaths.BackupHistoryFilePath;
        _fileStore = new AtomicRuntimeFileStore();
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
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
        _fileStore.WriteJson(_historyFilePath, history, _serializerOptions);
    }

    private Dictionary<string, DateTime> LoadHistory()
    {
        return _fileStore.ReadJson(
            _historyFilePath,
            _serializerOptions,
            static () => new Dictionary<string, DateTime>());
    }
}
