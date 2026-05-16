using System.Globalization;

namespace EasySave.Tests;

public class ApplicationTextServiceTests : IDisposable
{
    private readonly string? _originalLanguage;
    private readonly CultureInfo _originalCurrentCulture;
    private readonly CultureInfo _originalCurrentUiCulture;

    public ApplicationTextServiceTests()
    {
        _originalLanguage = Environment.GetEnvironmentVariable("EASYSAVE_LANGUAGE");
        _originalCurrentCulture = CultureInfo.CurrentCulture;
        _originalCurrentUiCulture = CultureInfo.CurrentUICulture;
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

        Assert.StartsWith("Transferred files: 2", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("128", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Job 1", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\n  ", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_UsesPersistedLanguage_WhenNoEnvironmentOverrideExists()
    {
        using var workspace = new TestWorkspace();
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", null);
        RuntimeStoragePaths.SetLanguageCode("fr");

        ApplicationTextService textService = ApplicationTextService.Create();

        Assert.Equal("fr", textService.GetLanguageCode());
        Assert.Contains("Utilisation", textService.GetUsageMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_UsesEnvironmentLanguageBeforePersistedLanguage()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLanguageCode("fr");
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", "en");

        ApplicationTextService textService = ApplicationTextService.Create();

        Assert.Equal("en", textService.GetLanguageCode());
        Assert.Contains("Usage", textService.GetUsageMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_UsesCurrentUiCulture_WhenNoOverrideOrPersistedLanguageExists()
    {
        using var workspace = new TestWorkspace();
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", null);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        ApplicationTextService textService = ApplicationTextService.Create();

        Assert.Equal("fr", textService.GetLanguageCode());
    }

    [Fact]
    public void FormatBackupResult_UsesFrenchResourceFormat()
    {
        ApplicationTextService textService = ApplicationTextService.Create("fr");

        string message = textService.FormatBackupResult(new BackupResult
        {
            JobNumber = 1,
            BackupName = "Job1",
            Status = BackupExecutionStatus.Error,
            ErrorMessage = "boom"
        });

        Assert.Equal("Tache 1 en erreur : Job1 | boom", message);
    }

    [Fact]
    public void GetText_ReturnsWpfResourceText()
    {
        ApplicationTextService textService = ApplicationTextService.Create("en");

        Assert.Equal("Run selected", textService.GetText("Wpf.RunSelectedButton"));
    }

    [Fact]
    public void ConsoleTranslationService_UsesSharedResources()
    {
        using var workspace = new TestWorkspace();
        var translationService = new ConsoleTranslationService();
        ApplicationTextService french = ApplicationTextService.Create("fr");
        ApplicationTextService english = ApplicationTextService.Create("en");

        Assert.Equal("Menu principal", translationService.GetMainMenuTitle(french));
        Assert.Equal("Main menu", translationService.GetMainMenuTitle(english));
        Assert.Equal("Langue actuelle : francais", translationService.GetCurrentLanguageLine(french));
        Assert.Equal("Current log format: JSON", translationService.GetCurrentLogFormatLine(english));
        Assert.Equal("Choix de langue invalide.", translationService.GetInvalidLanguageSelectionMessage(french));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("EASYSAVE_LANGUAGE", _originalLanguage);
        CultureInfo.CurrentCulture = _originalCurrentCulture;
        CultureInfo.CurrentUICulture = _originalCurrentUiCulture;
    }
}
