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

        Assert.Single(jobs);
        Assert.Equal("Job1", jobs[0].Name);
        Assert.Equal(@"C:\A", jobs[0].Source);
        Assert.Equal(@"D:\A", jobs[0].Target);
        Assert.Equal(BackupType.Differential, jobs[0].Type);
    }

    [Fact]
    public void CreateJob_PersistsJobDataInRequestedSlot()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();

        BackupJob created = registry.CreateJob(3, new BackupJob
        {
            Name = "Photos",
            Source = @" C:\Photos ",
            Target = @" D:\Archives ",
            Type = BackupType.Differential
        });

        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();
        BackupJob slot = jobs[2];

        Assert.Equal("Job3", created.Name);
        Assert.Equal(@"C:\Photos", created.Source);
        Assert.Equal(@"D:\Archives", created.Target);
        Assert.Equal(BackupType.Differential, created.Type);
        Assert.Equal("Job3", slot.Name);
        Assert.Equal(@"C:\Photos", slot.Source);
        Assert.Equal(@"D:\Archives", slot.Target);
        Assert.Equal(BackupType.Differential, slot.Type);
    }

    [Fact]
    public void UpdateJob_OverwritesExistingSlot()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();
        registry.CreateJob(1, new BackupJob
        {
            Name = "Docs",
            Source = @"C:\Docs",
            Target = @"D:\Docs",
            Type = BackupType.Full
        });

        BackupJob updated = registry.UpdateJob(1, new BackupJob
        {
            Name = "DocsUpdated",
            Source = @"E:\Src",
            Target = @"F:\Dst",
            Type = BackupType.Differential
        });

        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();
        BackupJob slot = jobs[0];

        Assert.Equal("Job1", updated.Name);
        Assert.Equal(@"E:\Src", slot.Source);
        Assert.Equal(@"F:\Dst", slot.Target);
        Assert.Equal(BackupType.Differential, slot.Type);
    }

    [Fact]
    public void DeleteJob_ResetsSlotToDefaultValues()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();
        registry.CreateJob(2, new BackupJob
        {
            Name = "ToDelete",
            Source = @"C:\Temp",
            Target = @"D:\Temp",
            Type = BackupType.Full
        });

        BackupJob deleted = registry.DeleteJob(2);
        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();

        Assert.Equal("Job2", deleted.Name);
        Assert.Equal(@"C:\Temp", deleted.Source);
        Assert.Equal(@"D:\Temp", deleted.Target);
        Assert.Equal(BackupType.Full, deleted.Type);
        Assert.Equal(4, jobs.Count);
        Assert.DoesNotContain(jobs, job => string.Equals(job.Source, @"C:\Temp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(jobs, job => string.Equals(job.Target, @"D:\Temp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateJob_AllowsCreatingJobsBeyondDefaultFiveSlots()
    {
        using var workspace = new TestWorkspace();
        var registry = new BackupJobRegistry();

        BackupJob created = registry.CreateJob(7, new BackupJob
        {
            Source = @"C:\S7",
            Target = @"D:\T7",
            Type = BackupType.Full
        });

        IReadOnlyList<BackupJob> jobs = registry.LoadJobs();

        Assert.Equal(7, jobs.Count);
        Assert.Equal("Job7", created.Name);
        Assert.Equal(@"C:\S7", jobs[6].Source);
        Assert.Equal(@"D:\T7", jobs[6].Target);
        Assert.Equal(BackupType.Full, jobs[6].Type);
    }
}
