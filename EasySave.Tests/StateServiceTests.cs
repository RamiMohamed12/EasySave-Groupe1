using System.Text.Json;

namespace EasySave.Tests;

public class StateServiceTests
{
    [Fact]
    public void SynchronizeConfiguredJobs_CreatesEntriesForAllFiveSlots()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();
        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();
        var stateService = new StateService();

        stateService.SynchronizeConfiguredJobs(jobs);

        List<BackupState> states = LoadStates();
        Assert.Equal(BackupJobRegistry.DefaultJobCount, states.Count);
        Assert.All(states, state => Assert.Equal(BackupExecutionStatus.Inactive, state.Status));
    }

    [Fact]
    public void WriteState_UpdatesOneSlotAndKeepsTheOthers()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();
        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();
        var stateService = new StateService();
        stateService.SynchronizeConfiguredJobs(jobs);

        stateService.WriteState(new BackupState
        {
            BackupName = "Job3",
            Status = BackupExecutionStatus.Active,
            IsRunning = true,
            LastBackupUpdateTime = DateTime.Now
        });

        List<BackupState> states = LoadStates();
        Assert.Equal(BackupJobRegistry.DefaultJobCount, states.Count);
        Assert.Contains(states, state => state.BackupName == "Job3" && state.Status == BackupExecutionStatus.Active);
        Assert.Contains(states, state => state.BackupName == "Job1" && state.Status == BackupExecutionStatus.Inactive);
    }

    [Fact]
    public void StateService_CanReloadExistingStateFile()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();
        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();
        var stateService = new StateService();
        stateService.SynchronizeConfiguredJobs(jobs);
        stateService.WriteState(new BackupState
        {
            BackupName = "Job2",
            Status = BackupExecutionStatus.Error,
            ErrorMessage = "copy failed",
            LastBackupUpdateTime = DateTime.Now
        });

        var reloadedStateService = new StateService();
        reloadedStateService.SynchronizeConfiguredJobs(jobs);

        List<BackupState> states = LoadStates();
        Assert.Contains(states, state => state.BackupName == "Job2" && state.Status == BackupExecutionStatus.Error);
    }

    [Fact]
    public void ConcurrentWritesFromMultipleInstances_KeepValidJson()
    {
        using var workspace = new TestWorkspace();
        IReadOnlyList<BackupJob> jobs = new BackupJobRegistry().LoadJobs();
        new StateService().SynchronizeConfiguredJobs(jobs);

        Parallel.For(0, 20, index =>
        {
            var stateService = new StateService();
            stateService.WriteState(new BackupState
            {
                BackupName = $"Job{index % BackupJobRegistry.DefaultJobCount + 1}",
                Status = BackupExecutionStatus.Active,
                IsRunning = true,
                LastBackupUpdateTime = DateTime.Now
            });
        });

        List<BackupState> states = LoadStates();
        Assert.Equal(BackupJobRegistry.DefaultJobCount, states.Count);
    }

    [Fact]
    public void RecoverInterruptedBackups_MarksRunningStatesStopped()
    {
        using var workspace = new TestWorkspace();
        new BackupJobRegistry().LoadJobs();
        var stateService = new StateService();
        stateService.WriteState(new BackupState
        {
            BackupName = "Job1",
            Status = BackupExecutionStatus.Active,
            IsRunning = true,
            RequestedAction = BackupControlAction.Run,
            CurrentSourcePath = workspace.GetPath("source.txt"),
            CurrentTargetPath = workspace.GetPath("target.txt"),
            LastBackupUpdateTime = DateTime.Now
        });

        int recovered = new StateService().RecoverInterruptedBackups(new LoggerService());

        BackupState state = LoadStates().Single(current => current.BackupName == "Job1");
        Assert.Equal(1, recovered);
        Assert.Equal(BackupExecutionStatus.Stopped, state.Status);
        Assert.False(state.IsRunning);
        Assert.Equal(BackupControlAction.Stop, state.RequestedAction);
        Assert.NotNull(state.LastRunCompletedAt);
        Assert.Equal(StateService.InterruptedShutdownMessage, state.ErrorMessage);
    }

    private static List<BackupState> LoadStates()
    {
        string json = File.ReadAllText(RuntimeStoragePaths.StateFilePath);
        return JsonSerializer.Deserialize<List<BackupState>>(json, JsonTestHelper.SerializerOptions) ?? new List<BackupState>();
    }
}
