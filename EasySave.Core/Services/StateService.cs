using System.Text.Json;
using System.Text.Json.Serialization;

public class StateService
{
    private readonly string _stateFilePath;
    private readonly string _jobsFilePath;
    private readonly Dictionary<string, BackupState> _statesByBackupName;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly AtomicRuntimeFileStore _fileStore;

    public StateService()
    {
        _stateFilePath = RuntimeStoragePaths.StateFilePath;
        _jobsFilePath = RuntimeStoragePaths.JobsFilePath;
        _fileStore = new AtomicRuntimeFileStore();
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        _statesByBackupName = LoadExistingStates();
    }

    public void WriteState(BackupState state)
    {
        _statesByBackupName[state.BackupName] = state;
        SynchronizeConfiguredJobsCore(LoadConfiguredJobs());
        WriteStateSnapshot();
    }

    public IReadOnlyList<BackupState> ReadAllStates()
    {
        List<BackupState>? states = _fileStore.ReadJson(
            _stateFilePath,
            _serializerOptions,
            static () => new List<BackupState>());
        return states ?? new List<BackupState>();
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
                Status = BackupExecutionStatus.Inactive,
                LastBackupUpdateTime = snapshotTime
            };
        }
    }

    private IReadOnlyList<BackupJob> LoadConfiguredJobs()
    {
        List<BackupJob>? jobs = _fileStore.ReadJson(_jobsFilePath, _serializerOptions, static () => new List<BackupJob>());

        return jobs ?? new List<BackupJob>();
    }

    private Dictionary<string, BackupState> LoadExistingStates()
    {
        List<BackupState>? states = _fileStore.ReadJson(
            _stateFilePath,
            _serializerOptions,
            static () => new List<BackupState>());
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
        List<BackupState> stateSnapshot = _statesByBackupName.Values
            .OrderBy(currentState => currentState.BackupName)
            .ToList();
        _fileStore.WriteJson(_stateFilePath, stateSnapshot, _serializerOptions);
    }
}
