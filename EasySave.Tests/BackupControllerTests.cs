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

        Assert.Equal([1, 3], fakeService.ReceivedJobNumbers);
        Assert.Equal(["Job1", "Job3"], results.Select(result => result.BackupName).ToArray());
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

        Assert.Equal("Job4", created.Name);
        Assert.Equal("Job4", updated.Name);
        Assert.Equal("Job4", deleted.Name);
        Assert.Equal(4, jobs.Count);
        Assert.DoesNotContain(jobs, job => string.Equals(job.Source, @"E:\Media", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(jobs, job => string.Equals(job.Target, @"F:\Media", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeBackupService : IBackupService
    {
        public List<int> ReceivedJobNumbers { get; } = new();

        public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
        {
            ReceivedJobNumbers.Add(selectedBackupJob.JobNumber);
            return new BackupResult
            {
                JobNumber = selectedBackupJob.JobNumber,
                BackupName = selectedBackupJob.Job.Name,
                Status = BackupExecutionStatus.Finished
            };
        }
    }
}
