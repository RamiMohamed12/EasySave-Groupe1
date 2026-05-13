public class BackupExecutionCommandState
{
    public int JobNumber { get; set; }
    public BackupControlAction RequestedAction { get; set; }
    public BackupPauseReason PauseReason { get; set; }
    public string PauseReasonDetails { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public BackupExecutionCommandState()
    {
        JobNumber = 0;
        RequestedAction = BackupControlAction.None;
        PauseReason = BackupPauseReason.None;
        PauseReasonDetails = string.Empty;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
