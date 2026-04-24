using System.Text.Json;

public class StateService
{
    private readonly string _stateFilePath;
    private readonly string _jobsFilePath;
    private readonly Dictionary<string, BackupState> _statesByBackupName;

    public StateService()
    {
        _stateFilePath = RuntimeStoragePaths.StateFilePath;
        _jobsFilePath = RuntimeStoragePaths.JobsFilePath;
        _statesByBackupName = LoadExistingStates();
    }

    public void WriteState(BackupState state)
    {
        _statesByBackupName[state.BackupName] = state;
        SynchronizeConfiguredJobsCore(LoadConfiguredJobs());
        WriteStateSnapshot();
    }

    public void SynchronizeConfiguredJobs(IEnumerable<BackupJob> jobs)
    {
        SynchronizeConfiguredJobsCore(jobs);
        WriteStateSnapshot();
    }

    private void SynchronizeConfiguredJobsCore(IEnumerable<BackupJob> jobs)
    {
        var configuredJobNames = jobs
            .Select(job => job.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string backupName in _statesByBackupName.Keys.ToList())
        {
            if (!configuredJobNames.Contains(backupName))
            {
                _statesByBackupName.Remove(backupName);
            }
        }

        DateTime snapshotTime = DateTime.Now;

        foreach (BackupJob job in jobs)
        {
            if (_statesByBackupName.ContainsKey(job.Name))
            {
                continue;
            }

            _statesByBackupName[job.Name] = new BackupState
            {
                BackupName = job.Name,
                IsRunning = false,
                LastBackupUpdateTime = snapshotTime
            };
        }
    }

    private IReadOnlyList<BackupJob> LoadConfiguredJobs()
    {
        if (!File.Exists(_jobsFilePath))
        {
            return Array.Empty<BackupJob>();
        }

        string json = File.ReadAllText(_jobsFilePath);
        List<BackupJob>? jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);

        return jobs ?? new List<BackupJob>();
    }

    private Dictionary<string, BackupState> LoadExistingStates()
    {
        if (!File.Exists(_stateFilePath))
        {
            return new Dictionary<string, BackupState>(StringComparer.OrdinalIgnoreCase);
        }

        string json = File.ReadAllText(_stateFilePath);
        List<BackupState>? states = JsonSerializer.Deserialize<List<BackupState>>(json);
        var statesByBackupName = new Dictionary<string, BackupState>(StringComparer.OrdinalIgnoreCase);

        if (states is null)
        {
            return statesByBackupName;
        }

        foreach (BackupState state in states)
        {
            state.LastRunTransferredFiles ??= new List<BackupTransferredFile>();
            statesByBackupName[state.BackupName] = state;
        }

        return statesByBackupName;
    }

    private void WriteStateSnapshot()
    {
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
