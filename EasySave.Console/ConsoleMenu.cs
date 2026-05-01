public class ConsoleMenu
{
    private readonly Func<string?, ConsoleMenuRuntime> _runtimeFactory;
    private readonly ConsoleTranslationService _translationService;
    private readonly BackupConsoleFeatures _backupFeatures;
    private readonly InteractiveConsole _interactiveConsole;

    private ConsoleMenuRuntime _runtime;

    public ConsoleMenu(
        Func<string?, ConsoleMenuRuntime> runtimeFactory,
        BackupJobRegistry jobRegistry,
        StateService stateService)
    {
        _runtimeFactory = runtimeFactory;
        _runtime = _runtimeFactory(null);
        _translationService = new ConsoleTranslationService();
        _interactiveConsole = new InteractiveConsole();
        _backupFeatures = new BackupConsoleFeatures(
            jobRegistry,
            stateService,
            _translationService,
            _interactiveConsole,
            GetRuntime,
            SetLanguage);
    }

    public void Start()
    {
        int selectedIndex = 0;

        while (true)
        {
            IReadOnlyList<string> options =
            [
                _translationService.GetViewJobsLabel(_runtime.TextService),
                _translationService.GetManageJobsLabel(_runtime.TextService),
                _translationService.GetRunBackupsLabel(_runtime.TextService),
                _translationService.GetViewStateLabel(_runtime.TextService),
                _translationService.GetViewLogsLabel(_runtime.TextService),
                _translationService.GetChangeLogFormatLabel(_runtime.TextService),
                _translationService.GetChangeLanguageLabel(_runtime.TextService),
                _translationService.GetExitLabel(_runtime.TextService)
            ];

            IReadOnlyList<string> contextLines =
            [
                _translationService.GetCurrentLanguageLine(_runtime.TextService),
                _translationService.GetCurrentLogFormatLine(_runtime.TextService)
            ];

            int selection = _interactiveConsole.SelectOption(
                _translationService.GetMainMenuTitle(_runtime.TextService),
                options,
                contextLines,
                _translationService.GetNavigationHelp(_runtime.TextService),
                allowBack: false,
                initialIndex: selectedIndex) ?? selectedIndex;

            selectedIndex = selection;

            switch (selection)
            {
                case 0:
                    _backupFeatures.ViewJobs();
                    break;
                case 1:
                    _backupFeatures.ManageJobs();
                    break;
                case 2:
                    _backupFeatures.RunBackups();
                    break;
                case 3:
                    _backupFeatures.ViewState();
                    break;
                case 4:
                    _backupFeatures.ViewLogs();
                    break;
                case 5:
                    _backupFeatures.ChangeLogFormat();
                    break;
                case 6:
                    _backupFeatures.ChangeLanguage();
                    break;
                case 7:
                    return;
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
}
