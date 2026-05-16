public sealed class ScheduledRunResult
{
    public string ScheduleId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StartedJobCount { get; set; }
    public int SkippedJobCount { get; set; }
    public IReadOnlyList<BackupResult> BackupResults { get; set; } = Array.Empty<BackupResult>();
}
