namespace EasySave.Wpf;

public sealed class ScheduleJobChoice
{
    public bool IsSelected { get; set; }
    public string JobId { get; set; } = string.Empty;
    public int JobNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
