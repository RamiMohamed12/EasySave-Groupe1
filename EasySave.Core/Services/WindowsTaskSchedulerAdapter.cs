using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Xml.Linq;

public class WindowsTaskSchedulerAdapter : IWindowsTaskSchedulerAdapter
{
    public const string TaskFolder = @"\EasySave";

    public void UpsertScheduleTask(BackupSchedule schedule, string consoleRunnerPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        if (string.IsNullOrWhiteSpace(consoleRunnerPath) || !File.Exists(consoleRunnerPath))
        {
            throw new FileNotFoundException("The EasySave console runner was not found.", consoleRunnerPath);
        }

        string xml = BuildTaskXml(schedule, consoleRunnerPath);
        string temporaryXmlPath = Path.Combine(Path.GetTempPath(), $"easysave-{schedule.Id}.xml");
        File.WriteAllText(temporaryXmlPath, xml, Encoding.Unicode);

        try
        {
            RunSchTasks(BuildCreateArguments(schedule, temporaryXmlPath));
        }
        finally
        {
            try
            {
                File.Delete(temporaryXmlPath);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }

    public void DeleteScheduleTask(BackupSchedule schedule)
    {
        if (!OperatingSystem.IsWindows() || schedule is null || string.IsNullOrWhiteSpace(schedule.WindowsTaskName))
        {
            return;
        }

        RunSchTasks(BuildDeleteArguments(schedule), ignoreNotFound: true);
    }

    public static string BuildCreateArguments(BackupSchedule schedule, string taskXmlPath)
    {
        return $"/Create /TN \"{GetTaskPath(schedule)}\" /XML \"{taskXmlPath}\" /F";
    }

    public static string BuildDeleteArguments(BackupSchedule schedule)
    {
        return $"/Delete /TN \"{GetTaskPath(schedule)}\" /F";
    }

    public static string BuildTaskXml(BackupSchedule schedule, string consoleRunnerPath)
    {
        TimeOnly runTime = TimeOnly.ParseExact(schedule.LocalRunTime, "HH:mm", CultureInfo.InvariantCulture);
        XNamespace taskNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        string startBoundary = DateTime.Today
            .Add(runTime.ToTimeSpan())
            .ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

        var task = new XElement(taskNamespace + "Task",
            new XAttribute("version", "1.4"),
            new XElement(taskNamespace + "RegistrationInfo",
                new XElement(taskNamespace + "Description", $"Runs EasySave schedule '{schedule.Name}'.")),
            new XElement(taskNamespace + "Triggers",
                schedule.Weekdays.Select(weekday =>
                    new XElement(taskNamespace + "CalendarTrigger",
                        new XElement(taskNamespace + "StartBoundary", startBoundary),
                        new XElement(taskNamespace + "Enabled", schedule.IsEnabled.ToString().ToLowerInvariant()),
                        new XElement(taskNamespace + "ScheduleByWeek",
                            new XElement(taskNamespace + "WeeksInterval", "1"),
                            new XElement(taskNamespace + "DaysOfWeek",
                                new XElement(taskNamespace + GetTaskSchedulerWeekdayName(weekday))))))),
            new XElement(taskNamespace + "Principals",
                new XElement(taskNamespace + "Principal",
                    new XAttribute("id", "Author"),
                    new XElement(taskNamespace + "LogonType", "InteractiveToken"),
                    new XElement(taskNamespace + "RunLevel", "LeastPrivilege"))),
            new XElement(taskNamespace + "Settings",
                new XElement(taskNamespace + "MultipleInstancesPolicy", "IgnoreNew"),
                new XElement(taskNamespace + "DisallowStartIfOnBatteries", "false"),
                new XElement(taskNamespace + "StopIfGoingOnBatteries", "false"),
                new XElement(taskNamespace + "AllowHardTerminate", "true"),
                new XElement(taskNamespace + "StartWhenAvailable", "true"),
                new XElement(taskNamespace + "RunOnlyIfNetworkAvailable", "false"),
                new XElement(taskNamespace + "AllowStartOnDemand", "true"),
                new XElement(taskNamespace + "Enabled", schedule.IsEnabled.ToString().ToLowerInvariant()),
                new XElement(taskNamespace + "Hidden", "false"),
                new XElement(taskNamespace + "RunOnlyIfIdle", "false"),
                new XElement(taskNamespace + "WakeToRun", "false"),
                new XElement(taskNamespace + "ExecutionTimeLimit", "PT0S"),
                new XElement(taskNamespace + "Priority", "7")),
            new XElement(taskNamespace + "Actions",
                new XAttribute("Context", "Author"),
                new XElement(taskNamespace + "Exec",
                    new XElement(taskNamespace + "Command", consoleRunnerPath),
                    new XElement(taskNamespace + "Arguments", $"--run-schedule \"{schedule.Id}\""),
                    new XElement(taskNamespace + "WorkingDirectory", Path.GetDirectoryName(consoleRunnerPath) ?? string.Empty))));

        return new XDocument(new XDeclaration("1.0", "UTF-16", null), task).ToString();
    }

    public static string GetTaskPath(BackupSchedule schedule)
    {
        return $@"{TaskFolder}\{schedule.WindowsTaskName}";
    }

    private static string GetTaskSchedulerWeekdayName(DayOfWeek weekday)
    {
        return weekday switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            DayOfWeek.Sunday => "Sunday",
            _ => throw new ArgumentOutOfRangeException(nameof(weekday), weekday, null)
        };
    }

    private static void RunSchTasks(string arguments, bool ignoreNotFound = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start schtasks.exe.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return;
        }

        string message = string.Join(Environment.NewLine, new[] { output, error }.Where(text => !string.IsNullOrWhiteSpace(text)));
        if (ignoreNotFound && message.Contains("cannot find", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException($"Task Scheduler command failed: {message}");
    }
}
