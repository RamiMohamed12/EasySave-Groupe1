namespace EasySave.Tests;

public class ScheduleRegistryTests
{
    [Fact]
    public void ValidateSchedule_ReturnsErrorsForRequiredFields()
    {
        using var workspace = new TestWorkspace();
        var registry = new ScheduleRegistry();

        ScheduleValidationResult result = registry.ValidateSchedule(
            new BackupSchedule
            {
                Name = " ",
                LocalRunTime = "25:00",
                TargetJobIds = new List<string>(),
                Weekdays = new List<DayOfWeek>()
            },
            Array.Empty<BackupJob>());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("time", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("weekday", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("job", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateSchedule_ReturnsErrorForMissingJobIds()
    {
        using var workspace = new TestWorkspace();
        BackupJob job = new BackupJobRegistry().LoadJobs()[0];
        var registry = new ScheduleRegistry();

        ScheduleValidationResult result = registry.ValidateSchedule(
            new BackupSchedule
            {
                Name = "Morning",
                LocalRunTime = "04:00",
                TargetJobIds = new List<string> { job.Id, "missing" },
                Weekdays = new List<DayOfWeek> { DayOfWeek.Monday }
            },
            new[] { job });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SaveSchedule_PersistsScheduleAndLastRunMetadata()
    {
        using var workspace = new TestWorkspace();
        BackupJob job = new BackupJobRegistry().LoadJobs()[0];
        var registry = new ScheduleRegistry();

        BackupSchedule saved = registry.SaveSchedule(
            new BackupSchedule
            {
                Name = "Morning",
                LocalRunTime = "04:00",
                TargetJobIds = new List<string> { job.Id },
                Weekdays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday }
            },
            new[] { job });

        registry.UpdateScheduleRunMetadata(saved.Id, DateTime.UtcNow, DateTime.UtcNow, "Completed", "Done");
        BackupSchedule reloaded = registry.GetSchedule(saved.Id);

        Assert.False(string.IsNullOrWhiteSpace(saved.Id));
        Assert.False(string.IsNullOrWhiteSpace(saved.WindowsTaskName));
        Assert.Equal("Completed", reloaded.LastRunStatus);
        Assert.Equal("Done", reloaded.LastRunMessage);
    }
}
