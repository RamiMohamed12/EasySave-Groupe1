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
var viewModel = new ApplicationViewModel(argumentParser, jobRegistry, controller, stateService, textService);
var view = new ConsoleApplicationView(textService);

viewModel.Load(args);
view.Render(viewModel);
viewModel.StartBackups();
