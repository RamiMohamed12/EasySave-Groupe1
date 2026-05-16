var loggerService = new LoggerService();
var stateService = new StateService();
var backupHistoryService = new BackupHistoryService();
var jobRegistry = new BackupJobRegistry();
var businessSoftwareMonitor = new BusinessSoftwareMonitor();

var menuManager = new ConsoleMenu(
    CreateRuntime,
    jobRegistry,
    stateService);

if (args.Length == 0)
{
    menuManager.Start();
}
else
{
    var commandLineBackupRunner = new CommandLineBackupRunner(
        jobRegistry,
        stateService,
        loggerService,
        CreateRuntime);

    commandLineBackupRunner.Run(args);
}

ConsoleMenuRuntime CreateRuntime(string? languageCode)
{
    var textService = string.IsNullOrWhiteSpace(languageCode)
        ? ApplicationTextService.Create()
        : ApplicationTextService.Create(languageCode);

    IBackupService backupService = new BackupService(
        loggerService,
        stateService,
        backupHistoryService,
        textService,
        businessSoftwareMonitor);

    return new ConsoleMenuRuntime(
        textService,
        new ArgumentParser(textService),
        new BackupController(backupService));
}
