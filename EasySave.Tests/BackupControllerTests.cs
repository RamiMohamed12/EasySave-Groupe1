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
