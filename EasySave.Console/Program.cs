var loggerService = new LoggerService();
var stateService = new StateService();
var backupHistoryService = new BackupHistoryService();
var jobRegistry = new BackupJobRegistry();

var menuManager = new MenuManager(
    CreateRuntime,
    jobRegistry,
    stateService);

menuManager.Start();

ConsoleMenuRuntime CreateRuntime(string? languageCode)
{
    var textService = string.IsNullOrWhiteSpace(languageCode)
        ? ApplicationTextService.Create()
        : ApplicationTextService.Create(languageCode);

    IBackupService backupService = new BackupService(
        loggerService,
        stateService,
        backupHistoryService,
        textService);

    return new ConsoleMenuRuntime(
        textService,
        new ArgumentParser(textService),
        new BackupController(backupService));
}
