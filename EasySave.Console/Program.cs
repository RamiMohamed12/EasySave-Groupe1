var textService = ApplicationTextService.Create();
var loggerService = new LoggerService();
var stateService = new StateService();
var backupHistoryService = new BackupHistoryService();
IBackupService backupService = new BackupService(
    loggerService,
    stateService,
    backupHistoryService,
    textService);
var controller = new BackupController(backupService);
var argumentParser = new ArgumentParser(textService);
var jobRegistry = new BackupJobRegistry();
var view = new ConsoleApplicationView();

var menuManager = new MenuManager(
    textService,
    jobRegistry,
    argumentParser,
    controller,
    stateService,
    view);

menuManager.Start();