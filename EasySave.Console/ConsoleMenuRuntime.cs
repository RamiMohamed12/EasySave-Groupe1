public sealed class ConsoleMenuRuntime
{
    public ConsoleMenuRuntime(
        ApplicationTextService textService,
        ArgumentParser argumentParser,
        BackupController backupController)
    {
        TextService = textService;
        ArgumentParser = argumentParser;
        BackupController = backupController;
    }

    public ApplicationTextService TextService { get; }

    public ArgumentParser ArgumentParser { get; }

    public BackupController BackupController { get; }
}
