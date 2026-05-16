namespace EasySave.Tests;

public class WindowsTaskSchedulerAdapterTests
{
    [Fact]
    public void BuildTaskXml_UsesWeeklyTriggersAndStartWhenAvailable()
    {
        var schedule = new BackupSchedule
        {
            Id = "schedule-1",
            Name = "Morning",
            IsEnabled = true,
            LocalRunTime = "04:00",
            Weekdays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
            WindowsTaskName = "Morning-schedule"
        };

        string xml = WindowsTaskSchedulerAdapter.BuildTaskXml(schedule, @"C:\Apps\EasySave.exe");

        Assert.Contains("<StartWhenAvailable>true</StartWhenAvailable>", xml);
        Assert.Contains("<MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>", xml);
        Assert.Contains("<Monday", xml);
        Assert.Contains("<Wednesday", xml);
        Assert.Contains("--run-schedule \"schedule-1\"", xml);
    }

    [Fact]
    public void BuildCreateArguments_TargetsEasySaveTaskFolder()
    {
        var schedule = new BackupSchedule
        {
            WindowsTaskName = "Morning-schedule"
        };

        string arguments = WindowsTaskSchedulerAdapter.BuildCreateArguments(schedule, @"C:\Temp\task.xml");

        Assert.Contains(@"/TN ""\EasySave\Morning-schedule""", arguments);
        Assert.Contains(@"/XML ""C:\Temp\task.xml""", arguments);
        Assert.Contains("/F", arguments);
    }
}
