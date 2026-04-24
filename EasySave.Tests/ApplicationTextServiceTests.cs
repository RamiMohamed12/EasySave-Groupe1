namespace EasySave.Tests;

public class ApplicationTextServiceTests : IDisposable
{
    private readonly string? _originalLanguage;

    public ApplicationTextServiceTests()
    {
        _originalLanguage = Environment.GetEnvironmentVariable("EASYSAVE_LANGUAGE");
    }

    [Fact]
    public void Create_UsesFrenchMessages_WhenLanguageOverrideIsFr()
    {
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", "fr");
        ApplicationTextService textService = ApplicationTextService.Create();

        Assert.Contains("Utilisation", textService.GetUsageMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_UsesEnglishMessages_WhenLanguageOverrideIsEn()
    {
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", "en");
        ApplicationTextService textService = ApplicationTextService.Create();

        Assert.Contains("Usage", textService.GetUsageMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatBackupResult_FormatsFinishedResult()
    {
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", "en");
        ApplicationTextService textService = ApplicationTextService.Create();

        string message = textService.FormatBackupResult(new BackupResult
        {
            JobNumber = 1,
            BackupName = "Job1",
            Status = BackupExecutionStatus.Finished,
            TransferredFileCount = 2,
            TransferredBytes = 128
        });

        Assert.Contains("completed", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("128", message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", _originalLanguage);
    }
}
