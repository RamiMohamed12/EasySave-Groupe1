using System.Text.Json;
using System.Text.Json.Serialization;

public class StateService
{
    public const string InterruptedShutdownMessage = "Backup stopped because the application closed.";
    private static readonly object SnapshotSyncRoot = new();
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
        lock (SnapshotSyncRoot)
        {
            MergeExistingStates();
            _statesByBackupName[state.BackupName] = state;
            SynchronizeConfiguredJobsCore(LoadConfiguredJobs());
            WriteStateSnapshot();
        }
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
        lock (SnapshotSyncRoot)
        {
            MergeExistingStates();
            SynchronizeConfiguredJobsCore(jobs);
            WriteStateSnapshot();
        }
    }

    public int RecoverInterruptedBackups(LoggerService loggerService)
    {
        if (loggerService is null)
        {
            throw new ArgumentNullException(nameof(loggerService));
        }

        lock (SnapshotSyncRoot)
        {
            MergeExistingStates();
            DateTime timestamp = DateTime.Now;
            int recoveredCount = 0;

            foreach (BackupState state in _statesByBackupName.Values)
            {
                if (!IsInterruptedStatus(state.Status))
                {
                    continue;
                }

                state.Status = BackupExecutionStatus.Stopped;
                state.IsRunning = false;
                state.RequestedAction = BackupControlAction.Stop;
                state.LastBackupUpdateTime = timestamp;
                state.LastRunCompletedAt = timestamp;
                state.ErrorMessage = InterruptedShutdownMessage;
                state.PauseReason = BackupPauseReason.None;
                state.PauseReasonDetails = string.Empty;
                recoveredCount++;

                loggerService.WriteLog(new LogEntry
                {
                    Timestamp = timestamp,
                    BackupName = state.BackupName,
                    SourcePath = state.CurrentSourcePath,
                    DestinationPath = state.CurrentTargetPath,
                    ActionType = "Stopped",
                    ErrorMessage = InterruptedShutdownMessage,
                    FileSizeBytes = state.CurrentFileSize,
                    TransferTimeMilliseconds = 0
                });
            }

            if (recoveredCount > 0)
            {
                WriteStateSnapshot();
            }

            return recoveredCount;
        }
    }

    private static bool IsInterruptedStatus(BackupExecutionStatus status)
    {
        return status == BackupExecutionStatus.Active
            || status == BackupExecutionStatus.Stopping
            || status == BackupExecutionStatus.Paused
            || status == BackupExecutionStatus.PausedByBusinessSoftware;
    }

    private void MergeExistingStates()
    {
        foreach (BackupState state in LoadExistingStates().Values)
        {
            _statesByBackupName[state.BackupName] = state;
        }
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
