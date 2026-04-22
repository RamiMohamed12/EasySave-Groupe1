var loggerService = new LoggerService();
var stateService = new StateService();
var backupHistoryService = new BackupHistoryService();
IBackupService backupService = new BackupService(loggerService, stateService, backupHistoryService);
var controller = new BackupController(backupService);
var argumentParser = new ArgumentParser();
var jobRegistry = new BackupJobRegistry();
var viewModel = new ApplicationViewModel(argumentParser, jobRegistry, controller);
var view = new ConsoleApplicationView();

viewModel.Load(args);
view.Render(viewModel);
viewModel.StartBackups();
