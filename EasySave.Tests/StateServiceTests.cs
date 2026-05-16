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

    private static List<BackupState> LoadStates()
    {
        string json = File.ReadAllText(RuntimeStoragePaths.StateFilePath);
        return JsonSerializer.Deserialize<List<BackupState>>(json, JsonTestHelper.SerializerOptions) ?? new List<BackupState>();
    }
}
