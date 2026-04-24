namespace EasySave.Tests;

public class RuntimeStoragePathsTests
{
    [Fact]
    public void RuntimeStoragePaths_UsesBaseDirectoryByDefault_WhenNoConfigurationExists()
    {
        using var workspace = new TestWorkspace();
        File.Delete(RuntimeStoragePaths.ConfigurationFilePath);
        RuntimeStoragePaths.Reload();

        string baseDirectory = Path.GetFullPath(RuntimeStoragePaths.GetBaseDirectory());

        Assert.Equal(baseDirectory, RuntimeStoragePaths.BackupStateDirectory);
        Assert.Equal(Path.Combine(baseDirectory, "jobs.json"), RuntimeStoragePaths.JobsFilePath);
        Assert.Equal(Path.Combine(baseDirectory, "state.json"), RuntimeStoragePaths.StateFilePath);
        Assert.Equal(Path.Combine(baseDirectory, "backup-history.json"), RuntimeStoragePaths.BackupHistoryFilePath);
    }

    [Fact]
    public void SetStorageDirectory_RedirectsAllRuntimeFiles()
    {
        using var workspace = new TestWorkspace();
        string externalPath = workspace.CreateDirectory("usb");

        RuntimeStoragePaths.SetStorageDirectory(externalPath);

        Assert.Equal(Path.GetFullPath(externalPath), RuntimeStoragePaths.BackupStateDirectory);
        Assert.Equal(Path.Combine(Path.GetFullPath(externalPath), "jobs.json"), RuntimeStoragePaths.JobsFilePath);
        Assert.Equal(Path.Combine(Path.GetFullPath(externalPath), "state.json"), RuntimeStoragePaths.StateFilePath);
    }
}
