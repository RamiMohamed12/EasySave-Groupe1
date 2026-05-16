public class BackupSchedule
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public List<string> TargetJobIds { get; set; }
    public string LocalRunTime { get; set; }
    public List<DayOfWeek> Weekdays { get; set; }
    public DateTime? LastRunStartedAtUtc { get; set; }
    public DateTime? LastRunCompletedAtUtc { get; set; }
    public string LastRunStatus { get; set; }
    public string LastRunMessage { get; set; }
    public string WindowsTaskName { get; set; }

    public BackupSchedule()
    {
        Id = string.Empty;
        Name = string.Empty;
        IsEnabled = true;
        TargetJobIds = new List<string>();
        LocalRunTime = "04:00";
        Weekdays = new List<DayOfWeek>();
        LastRunStatus = string.Empty;
        LastRunMessage = string.Empty;
        WindowsTaskName = string.Empty;
    }
}
