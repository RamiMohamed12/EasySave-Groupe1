namespace EasySave.Tests;

public class BackupControllerTests
{
    [Fact]
    public void StartBackups_ReturnsResultsInExecutionOrder()
    {
        var fakeService = new FakeBackupService();
        var controller = new BackupController(fakeService);
        SelectedBackupJob[] jobs =
        {
            new() { JobNumber = 1, Job = new BackupJob { Name = "Job1" } },
            new() { JobNumber = 3, Job = new BackupJob { Name = "Job3" } }
        };

        IReadOnlyList<BackupResult> results = controller.StartBackups(jobs);

        Assert.Equal([1, 3], fakeService.ReceivedJobNumbers.OrderBy(jobNumber => jobNumber).ToArray());
        Assert.Equal(["Job1", "Job3"], results.Select(result => result.BackupName).ToArray());
    }

    [Fact]
    public void PauseResumeStop_ForwardCommandsToExecutionController()
    {
        var fakeController = new FakeExecutionController();
        var controller = new BackupController(new FakeBackupService(), new BackupJobRegistry(), fakeController);

        controller.PauseJob(2);
        controller.ResumeJob(2);
        controller.StopJob(2);

        Assert.Equal(
            [BackupControlAction.Pause, BackupControlAction.Resume, BackupControlAction.Stop],
            fakeController.RecordedActions);
    }

    [Fact]
    public void CreateUpdateDeleteJob_UsesRegistryFromCore()
    {
        using var workspace = new TestWorkspace();
        var controller = new BackupController(new FakeBackupService(), new BackupJobRegistry());

        BackupJob created = controller.CreateJob(4, new BackupJob
        {
            Name = "Media",
            Source = @"C:\Media",
            Target = @"D:\Media",
            Type = BackupType.Full
        });
        BackupJob updated = controller.UpdateJob(4, new BackupJob
        {
            Name = "MediaUpdated",
            Source = @"E:\Media",
            Target = @"F:\Media",
            Type = BackupType.Differential
        });
        BackupJob deleted = controller.DeleteJob(4);
        IReadOnlyList<BackupJob> jobs = new BackupJobRegistry().LoadJobs();

        Assert.Equal("Media", created.Name);
        Assert.Equal("MediaUpdated", updated.Name);
        Assert.Equal("MediaUpdated", deleted.Name);
        Assert.Equal(4, jobs.Count);
        Assert.DoesNotContain(jobs, job => string.Equals(job.Source, @"E:\Media", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(jobs, job => string.Equals(job.Target, @"F:\Media", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeBackupService : IBackupService
    {
        private readonly object _syncRoot = new();
        public List<int> ReceivedJobNumbers { get; } = new();

        public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
        {
            lock (_syncRoot)
            {
                ReceivedJobNumbers.Add(selectedBackupJob.JobNumber);
            }
            return new BackupResult
            {
                JobNumber = selectedBackupJob.JobNumber,
                BackupName = selectedBackupJob.Job.Name,
                Status = BackupExecutionStatus.Finished
            };
        }
    }

    private sealed class FakeExecutionController : IBackupExecutionController
    {
        public List<BackupControlAction> RecordedActions { get; } = new();

        public void BeginJobRun(int jobNumber)
        {
        }

        public void RequestPause(int jobNumber)
        {
            RecordedActions.Add(BackupControlAction.Pause);
        }

        public void RequestResume(int jobNumber)
        {
            RecordedActions.Add(BackupControlAction.Resume);
        }

        public void RequestStop(int jobNumber)
        {
            RecordedActions.Add(BackupControlAction.Stop);
        }

        public void RequestAutomaticPause(int jobNumber, string reasonDetails)
        {
            RecordedActions.Add(BackupControlAction.Pause);
        }

        public BackupExecutionCommandState GetCommandState(int jobNumber)
        {
            return new BackupExecutionCommandState
            {
                JobNumber = jobNumber,
                RequestedAction = BackupControlAction.None
            };
        }

        public void CompleteJob(int jobNumber)
        {
        }
    }
}
