public class CommandLineBackupRunner
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private readonly LoggerService _loggerService;
    private readonly Func<string?, ConsoleMenuRuntime> _runtimeFactory;
    private readonly InteractiveConsole _interactiveConsole = new();

    public CommandLineBackupRunner(
        BackupJobRegistry jobRegistry,
        StateService stateService,
        LoggerService loggerService,
        Func<string?, ConsoleMenuRuntime> runtimeFactory)
    {
        _jobRegistry = jobRegistry;
        _stateService = stateService;
        _loggerService = loggerService;
        _runtimeFactory = runtimeFactory;
    }

    public void Run(string[] args)
    {
        ConsoleMenuRuntime runtime = _runtimeFactory(null);
        if (args.Length == 1 && IsHelpArgument(args[0]))
        {
            _interactiveConsole.RenderOutputScreen(
                "EasySave",
                runtime.TextService.GetHelpLines().Select(line => new InteractiveConsole.ScreenLine(line)).ToArray());
            return;
        }

        if (args.Length == 2 && string.Equals(args[0], "--run-schedule", StringComparison.OrdinalIgnoreCase))
        {
            RunSchedule(args[1], runtime);
            return;
        }

        string selection = BuildSelection(args);

        try
        {
            IReadOnlyList<int> selectedJobNumbers = runtime.ArgumentParser.ParseJobSelection(selection);
            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            _stateService.SynchronizeConfiguredJobs(jobs);

            var selectedJobs = new List<SelectedBackupJob>();
            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber >= 1 && jobNumber <= jobs.Count)
                {
                    selectedJobs.Add(new SelectedBackupJob
                    {
                        JobNumber = jobNumber,
                        Job = jobs[jobNumber - 1]
                    });
                }
            }

            if (selectedJobs.Count == 0)
            {
                _interactiveConsole.RenderOutputScreen(
                    "EasySave",
                    [ErrorLine(runtime.TextService.GetNoConfiguredJobsMessage())]);
                return;
            }

            IReadOnlyList<BackupResult> results = runtime.BackupController.StartBackups(selectedJobs);
            var outputLines = new List<InteractiveConsole.ScreenLine>();

            for (int index = 0; index < results.Count; index++)
            {
                BackupResult result = results[index];
                bool showHeader = results.Count > 1;

                if (result.Status == BackupExecutionStatus.Finished)
                {
                    outputLines.AddRange(BuildMessageLines(
                        BuildBackupSuccessMessage(runtime.TextService, result, showHeader),
                        InteractiveConsole.ScreenLineKind.Success));
                }
                else
                {
                    outputLines.AddRange(BuildMessageLines(
                        BuildBackupErrorMessage(runtime.TextService, result, showHeader),
                        InteractiveConsole.ScreenLineKind.Error));
                }

                if (index < results.Count - 1)
                {
                    outputLines.Add(BlankLine());
                }
            }

            _interactiveConsole.RenderOutputScreen("Run backups", outputLines);
        }
        catch (Exception ex)
        {
            _interactiveConsole.RenderOutputScreen(
                "EasySave",
                [ErrorLine(BuildErrorMessage(runtime.TextService, ex.Message))]);
        }
    }

    private void RunSchedule(string scheduleId, ConsoleMenuRuntime runtime)
    {
        try
        {
            var schedulerService = new SchedulerService(
                new ScheduleRegistry(),
                _jobRegistry,
                _stateService,
                _loggerService,
                new WindowsTaskSchedulerAdapter(),
                runtime.BackupController);

            ScheduledRunResult result = schedulerService.RunSchedule(scheduleId);
            Console.WriteLine($"{result.Status}: {result.Message}");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(BuildErrorMessage(runtime.TextService, exception.Message));
            Environment.ExitCode = 1;
        }
    }

    private static string BuildSelection(string[] args)
    {
        return string.Concat(args).Replace(" ", string.Empty);
    }

    private static bool IsHelpArgument(string argument)
    {
        return argument.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("-h", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildBackupSuccessMessage(ApplicationTextService textService, BackupResult result, bool showHeader)
    {
        string details = textService.FormatBackupResult(result);
        string successMessage = textService.GetBackupSuccessMessage();

        if (!showHeader)
        {
            return $"{details}\n{successMessage}";
        }

        return $"{GetJobHeader(textService, result)}\n{details}\n{successMessage}";
    }

    private static string BuildBackupErrorMessage(ApplicationTextService textService, BackupResult result, bool showHeader)
    {
        string details = textService.FormatBackupResult(result);
        return showHeader
            ? $"{GetJobHeader(textService, result)}\n{details}"
            : details;
    }

    private static string GetJobHeader(ApplicationTextService textService, BackupResult result)
    {
        return textService.FormatText("Console.JobHeader", result.JobNumber, result.BackupName);
    }

    private static string BuildErrorMessage(ApplicationTextService textService, string details)
    {
        return textService.FormatText("Console.ErrorMessage", details);
    }

    private static IEnumerable<InteractiveConsole.ScreenLine> BuildMessageLines(
        string message,
        InteractiveConsole.ScreenLineKind kind)
    {
        return message.ReplaceLineEndings().Split(Environment.NewLine).Select(line => new InteractiveConsole.ScreenLine(line, kind));
    }

    private static InteractiveConsole.ScreenLine ErrorLine(string text)
    {
        return new InteractiveConsole.ScreenLine(text, InteractiveConsole.ScreenLineKind.Error);
    }

    private static InteractiveConsole.ScreenLine BlankLine()
    {
        return new InteractiveConsole.ScreenLine(string.Empty);
    }
}
