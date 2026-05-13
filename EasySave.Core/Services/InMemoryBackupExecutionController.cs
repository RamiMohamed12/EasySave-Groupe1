using System.Collections.Concurrent;

public class InMemoryBackupExecutionController : IBackupExecutionController
{
    private readonly ConcurrentDictionary<int, BackupExecutionCommandState> _commandStates = new();

    public void BeginJobRun(int jobNumber)
    {
        _commandStates.AddOrUpdate(
            jobNumber,
            _ => CreateState(jobNumber, BackupControlAction.Run, BackupPauseReason.None, string.Empty),
            (_, _) => CreateState(jobNumber, BackupControlAction.Run, BackupPauseReason.None, string.Empty));
    }

    public void RequestPause(int jobNumber)
    {
        _commandStates.AddOrUpdate(
            jobNumber,
            _ => CreateState(jobNumber, BackupControlAction.Pause, BackupPauseReason.UserRequested, string.Empty),
            (_, _) => CreateState(jobNumber, BackupControlAction.Pause, BackupPauseReason.UserRequested, string.Empty));
    }

    public void RequestResume(int jobNumber)
    {
        _commandStates.AddOrUpdate(
            jobNumber,
            _ => CreateState(jobNumber, BackupControlAction.Resume, BackupPauseReason.None, string.Empty),
            (_, _) => CreateState(jobNumber, BackupControlAction.Resume, BackupPauseReason.None, string.Empty));
    }

    public void RequestStop(int jobNumber)
    {
        _commandStates.AddOrUpdate(
            jobNumber,
            _ => CreateState(jobNumber, BackupControlAction.Stop, BackupPauseReason.None, string.Empty),
            (_, _) => CreateState(jobNumber, BackupControlAction.Stop, BackupPauseReason.None, string.Empty));
    }

    public void RequestAutomaticPause(int jobNumber, string reasonDetails)
    {
        _commandStates.AddOrUpdate(
            jobNumber,
            _ => CreateState(jobNumber, BackupControlAction.Pause, BackupPauseReason.BusinessSoftwareDetected, reasonDetails),
            (_, _) => CreateState(jobNumber, BackupControlAction.Pause, BackupPauseReason.BusinessSoftwareDetected, reasonDetails));
    }

    public BackupExecutionCommandState GetCommandState(int jobNumber)
    {
        return _commandStates.TryGetValue(jobNumber, out BackupExecutionCommandState? state)
            ? state
            : CreateState(jobNumber, BackupControlAction.None, BackupPauseReason.None, string.Empty);
    }

    public void CompleteJob(int jobNumber)
    {
        _commandStates.TryRemove(jobNumber, out _);
    }

    private static BackupExecutionCommandState CreateState(
        int jobNumber,
        BackupControlAction action,
        BackupPauseReason pauseReason,
        string reasonDetails)
    {
        return new BackupExecutionCommandState
        {
            JobNumber = jobNumber,
            RequestedAction = action,
            PauseReason = pauseReason,
            PauseReasonDetails = reasonDetails ?? string.Empty,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
