namespace EasySave.Tests;

public class RuntimeStoragePathsTests
{
    [Fact]
    public void RuntimeStoragePaths_UsesSharedStorageByDefault_WhenNoConfigurationExists()
    {
        using var workspace = new TestWorkspace();
        File.Delete(RuntimeStoragePaths.ConfigurationFilePath);
        RuntimeStoragePaths.Reload();

        string sharedRootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasySave");
        string baseDirectory = Path.GetFullPath(Path.Combine(sharedRootDirectory, "runtime"));

        Assert.Equal(baseDirectory, RuntimeStoragePaths.BackupStateDirectory);
        Assert.Equal(Path.Combine(baseDirectory, "jobs.json"), RuntimeStoragePaths.JobsFilePath);
        Assert.Equal(Path.Combine(baseDirectory, "state.json"), RuntimeStoragePaths.StateFilePath);
        Assert.Equal(Path.Combine(baseDirectory, "backup-history.json"), RuntimeStoragePaths.BackupHistoryFilePath);
        Assert.Equal("json", RuntimeStoragePaths.GetLogFileFormat());
        Assert.Equal(RuntimeStoragePaths.DarkThemeMode, RuntimeStoragePaths.GetThemeMode());
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

    [Fact]
    public void SetLanguageCode_PersistsLanguageAcrossReload()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetLanguageCode("fr");
        RuntimeStoragePaths.Reload();

        Assert.Equal("fr", RuntimeStoragePaths.GetLanguageCode());
    }

    [Fact]
    public void SetLogFileFormat_PersistsFormatAcrossReload()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetLogFileFormat("xml");
        RuntimeStoragePaths.Reload();

        Assert.Equal("xml", RuntimeStoragePaths.GetLogFileFormat());
        Assert.EndsWith(".xml", RuntimeStoragePaths.GetDailyLogFilePath(new DateTime(2026, 04, 29)));
    }

    [Fact]
    public void SetThemeMode_PersistsThemeAcrossReload()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetThemeMode(RuntimeStoragePaths.DarkThemeMode);
        RuntimeStoragePaths.Reload();

        Assert.Equal(RuntimeStoragePaths.DarkThemeMode, RuntimeStoragePaths.GetThemeMode());
    }

    [Fact]
    public void SetThemeMode_NormalizesInvalidThemeToSystem()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetThemeMode("invalid");
        RuntimeStoragePaths.Reload();

        Assert.Equal(RuntimeStoragePaths.SystemThemeMode, RuntimeStoragePaths.GetThemeMode());
    }

    [Fact]
    public void SetStorageDirectory_PreservesConfiguredLanguage()
    {
        using var workspace = new TestWorkspace();
        string externalPath = workspace.CreateDirectory("usb");

        RuntimeStoragePaths.SetLanguageCode("fr");
        RuntimeStoragePaths.SetStorageDirectory(externalPath);

        Assert.Equal("fr", RuntimeStoragePaths.GetLanguageCode());
    }

    [Fact]
    public void SetLanguageCode_PreservesConfiguredLogFormat()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetLogFileFormat("xml");
        RuntimeStoragePaths.SetLanguageCode("fr");

        Assert.Equal("xml", RuntimeStoragePaths.GetLogFileFormat());
    }

    [Fact]
    public void SetEncryptedExtensions_NormalizesAndPersistsExtensions()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetEncryptedExtensions(["txt; .PDF, docx txt"]);
        RuntimeStoragePaths.Reload();

        Assert.Equal([".docx", ".pdf", ".txt"], RuntimeStoragePaths.GetEncryptedExtensions());
    }

    [Fact]
    public void SetCryptoSoftKey_PersistsKeyAcrossReload()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetCryptoSoftKey("secret");
        RuntimeStoragePaths.Reload();

        Assert.Equal("secret", RuntimeStoragePaths.GetCryptoSoftKey());
    }

    [Fact]
    public void SetBlockedProcessNames_PersistsAcrossReload()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetBlockedProcessNames(["calc.exe", "WINWORD"]);
        RuntimeStoragePaths.Reload();

        Assert.Equal(["calc", "winword"], RuntimeStoragePaths.GetBlockedProcessNames());
    }

    [Fact]
    public void SetPriorityExtensions_NormalizesAndPersistsExtensions()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetPriorityExtensions(["log; .TXT, csv"]);
        RuntimeStoragePaths.Reload();

        Assert.Equal([".csv", ".log", ".txt"], RuntimeStoragePaths.GetPriorityExtensions());
    }

    [Fact]
    public void SetLargeFileThresholdKb_PersistsAndNormalizesNegativeValues()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetLargeFileThresholdKb(-50);
        RuntimeStoragePaths.Reload();

        Assert.Equal(0, RuntimeStoragePaths.GetLargeFileThresholdKb());
    }

    [Fact]
    public void SetMaxConcurrentJobs_PersistsAndNormalizesInvalidValues()
    {
        using var workspace = new TestWorkspace();

        RuntimeStoragePaths.SetMaxConcurrentJobs(0);
        RuntimeStoragePaths.Reload();

        Assert.Equal(2, RuntimeStoragePaths.GetMaxConcurrentJobs());
    }
}
