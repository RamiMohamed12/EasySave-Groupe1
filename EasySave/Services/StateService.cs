using System.Text.Json;

public class StateService
{
    private readonly string _stateFilePath;
    private readonly Dictionary<string, BackupState> _statesByBackupName;

    public StateService()
    {
        _stateFilePath = RuntimeStoragePaths.StateFilePath;
        _statesByBackupName = new Dictionary<string, BackupState>();
    }

    public void WriteState(BackupState state)
    {
        _statesByBackupName[state.BackupName] = state;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        List<BackupState> stateSnapshot = _statesByBackupName.Values
            .OrderBy(currentState => currentState.BackupName)
            .ToList();

        string json = JsonSerializer.Serialize(stateSnapshot, options);
        File.WriteAllText(_stateFilePath, json);
    }
}