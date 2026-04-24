using System.Text.Json;

namespace EasySave.Tests;

public class BackupJobRegistryTests
{
    [Fact]
    public void LoadJobs_CreatesFiveDefaultSlots()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();

        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();

        Assert.Equal(BackupJobRegistry.MaximumJobs, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal(BackupType.Full, jobs[0].Type);
        Assert.Equal("Job2", jobs[1].Name);
        Assert.Equal(BackupType.Differential, jobs[1].Type);
        Assert.All(jobs, job =>
        {
            Assert.Equal(string.Empty, job.Source);
            Assert.Equal(string.Empty, job.Target);
        });
    }

    [Fact]
    public void UpdateJobPath_PersistsConfiguredValues()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();

        registry.UpdateJobPath(1, JobPathField.Source, @"C:\Docs");
        registry.UpdateJobPath(1, JobPathField.Target, @"D:\Backup");

        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();
        Assert.Equal(@"C:\Docs", jobs[0].Source);
        Assert.Equal(@"D:\Backup", jobs[0].Target);
    }

    [Fact]
    public void LoadJobs_NormalizesUnexpectedJobFileContent()
    {
        using var workspace = new TestWorkspace();
        File.WriteAllText(
            RuntimeStoragePaths.JobsFilePath,
            JsonSerializer.Serialize(
                new[]
                {
                    new BackupJob { Name = "Custom", Source = @"C:\A", Target = @"D:\A", Type = BackupType.Differential }
                },
                JsonTestHelper.SerializerOptions));

        IReadOnlyList<BackupJob> jobs = new BackupJobRegistry().LoadJobs();

        Assert.Equal(BackupJobRegistry.MaximumJobs, jobs.Count);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal(@"C:\A", jobs[0].Source);
        Assert.Equal(@"D:\A", jobs[0].Target);
        Assert.Equal(BackupType.Full, jobs[0].Type);
        Assert.Equal("Job5", jobs[4].Name);
    }
}
