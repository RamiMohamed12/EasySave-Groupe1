namespace EasySave.Tests;

public class ApplicationViewModelTests
{
    [Fact]
    public void Load_WithoutArguments_ShowsConfiguredSlots()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(Array.Empty<string>());

        Assert.True(viewModel.ShowJobList);
        Assert.False(viewModel.ShowHelp);
        Assert.Empty(viewModel.Messages);
        Assert.Equal(BackupJobRegistry.MaximumJobs, viewModel.AvailableJobs.Count);
    }

    [Fact]
    public void Load_WithHelpArgument_ShowsHelpOnly()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(["--help"]);

        Assert.True(viewModel.ShowHelp);
        Assert.False(viewModel.ShowJobList);
    }

    [Fact]
    public void Load_WithConfigureCommand_UpdatesRequestedSlot()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(["--configure", "1", "source", @"C:\Desktop"]);

        Assert.True(viewModel.ShowJobList);
        Assert.Single(viewModel.Messages);
        Assert.Equal(@"C:\Desktop", viewModel.AvailableJobs[0].Source);
    }

    [Fact]
    public void Load_WithConfigureCommand_BeyondDefaultSlots_CreatesNewSlot()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(["--configure", "6", "source", @"E:\Extra"]);

        Assert.True(viewModel.ShowJobList);
        Assert.Single(viewModel.Messages);
        Assert.Equal(6, viewModel.AvailableJobs.Count);
        Assert.Equal(@"E:\Extra", viewModel.AvailableJobs[5].Source);
    }

    [Fact]
    public void Load_WithStorageDirectoryCommand_RelocatesRuntimeFiles()
    {
        using var workspace = new TestWorkspace();
        string usbPath = workspace.CreateDirectory("usb-storage");
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(["--storage-dir", usbPath]);

        Assert.True(viewModel.ShowJobList);
        Assert.Single(viewModel.Messages);
        Assert.Equal(Path.GetFullPath(usbPath), RuntimeStoragePaths.BackupStateDirectory);
        Assert.True(File.Exists(Path.Combine(usbPath, "jobs.json")));
    }

    [Fact]
    public void Load_WithLanguageCommand_PersistsLanguageAndUsesNewMessages()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(["--lang", "fr"]);

        Assert.True(viewModel.ShowJobList);
        Assert.True(viewModel.IsConfigurationMessage);
        Assert.Single(viewModel.Messages);
        Assert.Equal("fr", RuntimeStoragePaths.GetLanguageCode());
        Assert.Equal("fr", viewModel.TextService.GetLanguageCode());
        Assert.Contains("langue", viewModel.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_WithInvalidSelection_ShowsErrorAndHelp()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out _);

        viewModel.Load(["6"]);

        Assert.True(viewModel.ShowHelp);
        Assert.True(viewModel.ShowJobList);
        Assert.Single(viewModel.Messages);
    }

    [Fact]
    public void StartBackups_AfterSelection_FormatsControllerResults()
    {
        using var workspace = new TestWorkspace();
        ApplicationViewModel viewModel = CreateViewModel(out FakeBackupService fakeBackupService);

        viewModel.Load(["1;3"]);
        viewModel.StartBackups();

        Assert.Equal([1, 3], fakeBackupService.ReceivedJobs.Select(job => job.JobNumber).OrderBy(jobNumber => jobNumber).ToArray());
        Assert.Equal(3, viewModel.Messages.Count);
        Assert.StartsWith("Transferred files:", viewModel.Messages[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Backup completed successfully!", viewModel.Messages[^1]);
    }

    [Fact]
    public void StartBackups_WhenAJobFails_DoesNotAppendSuccessMessage()
    {
        using var workspace = new TestWorkspace();
        var textService = ApplicationTextService.Create();
        var viewModel = new ApplicationViewModel(
            new ArgumentParser(textService),
            new BackupJobRegistry(),
            new BackupController(new FailingBackupService()),
            new StateService(),
            textService);

        viewModel.Load(["1"]);
        viewModel.StartBackups();

        Assert.Single(viewModel.Messages);
        Assert.DoesNotContain("completed successfully", viewModel.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationViewModel CreateViewModel(out FakeBackupService fakeBackupService)
    {
        var textService = ApplicationTextService.Create(ApplicationTextService.EnglishLanguageCode);
        fakeBackupService = new FakeBackupService();

        return new ApplicationViewModel(
            new ArgumentParser(textService),
            new BackupJobRegistry(),
            new BackupController(fakeBackupService),
            new StateService(),
            textService);
    }

    private sealed class FakeBackupService : IBackupService
    {
        private readonly object _syncRoot = new();
        public List<SelectedBackupJob> ReceivedJobs { get; } = new();

        public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
        {
            lock (_syncRoot)
            {
                ReceivedJobs.Add(selectedBackupJob);
            }
            return new BackupResult
            {
                JobNumber = selectedBackupJob.JobNumber,
                BackupName = selectedBackupJob.Job.Name,
                Status = BackupExecutionStatus.Finished,
                TransferredFileCount = 1,
                TransferredBytes = 10
            };
        }
    }

    private sealed class FailingBackupService : IBackupService
    {
        public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
        {
            return new BackupResult
            {
                JobNumber = selectedBackupJob.JobNumber,
                BackupName = selectedBackupJob.Job.Name,
                Status = BackupExecutionStatus.Error,
                ErrorMessage = "failure"
            };
        }
    }
}
