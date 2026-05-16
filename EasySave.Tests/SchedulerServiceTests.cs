namespace EasySave.Tests;

public class SchedulerServiceTests
{
    [Fact]
    public void RunSchedule_ResolvesStableJobIdsAndSkipsMissingAndBusyJobs()
    {
        using var workspace = new TestWorkspace();
        var jobRegistry = new BackupJobRegistry();
        IReadOnlyList<BackupJob> jobs = jobRegistry.LoadJobs();
        BackupJob runnableJob = jobs[0];
        BackupJob busyJob = jobs[1];
        var stateService = new StateService();
        stateService.WriteState(new BackupState
        {
            BackupName = busyJob.Name,
            IsRunning = true,
            Status = BackupExecutionStatus.Active,
            LastBackupUpdateTime = DateTime.Now
        });

        var scheduleRegistry = new ScheduleRegistry();
        BackupSchedule schedule = scheduleRegistry.SaveSchedule(
            new BackupSchedule
            {
                Name = "Morning",
                LocalRunTime = "04:00",
                TargetJobIds = new List<string> { runnableJob.Id, busyJob.Id },
                Weekdays = new List<DayOfWeek> { DayOfWeek.Monday }
            },
            jobs);
        schedule.TargetJobIds.Add("missing-job-id");
        File.WriteAllText(
            RuntimeStoragePaths.SchedulesFilePath,
            System.Text.Json.JsonSerializer.Serialize(new[] { schedule }, JsonTestHelper.SerializerOptions));

        var backupService = new RecordingBackupService();
        var scheduler = new SchedulerService(
            scheduleRegistry,
            jobRegistry,
            stateService,
            new LoggerService(),
            new NoOpTaskSchedulerAdapter(),
            new BackupController(backupService, jobRegistry));

        ScheduledRunResult result = scheduler.RunSchedule(schedule.Id);

        Assert.Equal(1, result.StartedJobCount);
        Assert.Equal(2, result.SkippedJobCount);
        Assert.Single(backupService.StartedJobNumbers);
        Assert.Equal(1, backupService.StartedJobNumbers[0]);
        Assert.Equal("CompletedWithSkips", result.Status);
        Assert.Equal("CompletedWithSkips", scheduleRegistry.GetSchedule(schedule.Id).LastRunStatus);
    }

    private sealed class RecordingBackupService : IBackupService
    {
        public List<int> StartedJobNumbers { get; } = new();

        public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
        {
            lock (StartedJobNumbers)
            {
                StartedJobNumbers.Add(selectedBackupJob.JobNumber);
            }

            return new BackupResult
            {
                JobNumber = selectedBackupJob.JobNumber,
                BackupName = selectedBackupJob.Job.Name,
                Status = BackupExecutionStatus.Finished
            };
        }
    }

    private sealed class NoOpTaskSchedulerAdapter : IWindowsTaskSchedulerAdapter
    {
        public void UpsertScheduleTask(BackupSchedule schedule, string consoleRunnerPath)
        {
        }

        public void DeleteScheduleTask(BackupSchedule schedule)
        {
        }
    }
}
