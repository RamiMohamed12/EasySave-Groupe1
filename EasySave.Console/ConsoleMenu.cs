public class ConsoleMenu
{
    private readonly Func<string?, ConsoleMenuRuntime> _runtimeFactory;
    private readonly ConsoleTranslationService _translationService;
    private readonly BackupConsoleFeatures _backupFeatures;

    private ConsoleMenuRuntime _runtime;

    public ConsoleMenu(
        Func<string?, ConsoleMenuRuntime> runtimeFactory,
        BackupJobRegistry jobRegistry,
        StateService stateService)
    {
        _runtimeFactory = runtimeFactory;
        _runtime = _runtimeFactory(null);
        _translationService = new ConsoleTranslationService();
        _backupFeatures = new BackupConsoleFeatures(
            jobRegistry,
            stateService,
            _translationService,
            GetRuntime,
            SetLanguage);
    }

    public void Start()
    {
        while (true)
        {
            Console.Clear();
            DisplayMainMenu();

            Console.Write("\n> ");
            string? choice = Console.ReadLine();

            switch (choice?.Trim().ToLowerInvariant())
            {
                case "1":
                    _backupFeatures.ViewJobs();
                    break;
                case "2":
                    _backupFeatures.ConfigureJobSource();
                    break;
                case "3":
                    _backupFeatures.ConfigureJobTarget();
                    break;
                case "4":
                    _backupFeatures.RunBackups();
                    break;
                case "5":
                    _backupFeatures.ChangeLanguage();
                    break;
                case "6":
                    return;
                default:
                    WriteError(_translationService.GetInvalidMenuChoiceMessage(_runtime.TextService));
                    Pause();
                    break;
            }
        }
    }

    private ConsoleMenuRuntime GetRuntime()
    {
        return _runtime;
    }

    private void SetLanguage(string languageCode)
    {
        RuntimeStoragePaths.SetLanguageCode(languageCode);
        _runtime = _runtimeFactory(languageCode);
    }

    private void DisplayMainMenu()
    {
        WriteMenuBorder();
        WriteMenuLine(_translationService.GetMainMenuTitle(_runtime.TextService));
        WriteMenuBorder();
        WriteMenuLine(_translationService.GetMenuOptionLabel(1, _translationService.GetViewJobsLabel(_runtime.TextService)));
        WriteMenuLine(_translationService.GetMenuOptionLabel(2, _translationService.GetConfigureSourceLabel(_runtime.TextService)));
        WriteMenuLine(_translationService.GetMenuOptionLabel(3, _translationService.GetConfigureTargetLabel(_runtime.TextService)));
        WriteMenuLine(_translationService.GetMenuOptionLabel(4, _translationService.GetRunBackupsLabel(_runtime.TextService)));
        WriteMenuLine(_translationService.GetMenuOptionLabel(5, _translationService.GetChangeLanguageLabel(_runtime.TextService)));
        WriteMenuLine(_translationService.GetMenuOptionLabel(6, _translationService.GetExitLabel(_runtime.TextService)));
        WriteMenuBorder();
        Console.WriteLine(_translationService.GetCurrentLanguageLine(_runtime.TextService));
    }

    private void Pause()
    {
        Console.WriteLine(_translationService.GetPauseMessage(_runtime.TextService));
        Console.ReadKey();
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteMenuBorder()
    {
        const int menuInnerWidth = 42;
        Console.WriteLine($"+{new string('-', menuInnerWidth + 2)}+");
    }

    private static void WriteMenuLine(string content)
    {
        const int menuInnerWidth = 42;

        string paddedContent = content.Length > menuInnerWidth
            ? content[..menuInnerWidth]
            : content.PadRight(menuInnerWidth);

        Console.WriteLine($"| {paddedContent} |");
    }
}
