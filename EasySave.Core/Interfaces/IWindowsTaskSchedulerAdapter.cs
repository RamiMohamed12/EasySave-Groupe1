public interface IWindowsTaskSchedulerAdapter
{
    void UpsertScheduleTask(BackupSchedule schedule, string consoleRunnerPath);

    void DeleteScheduleTask(BackupSchedule schedule);
}
