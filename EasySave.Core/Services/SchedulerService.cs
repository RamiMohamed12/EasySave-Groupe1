public class SchedulerService
{
    private readonly ScheduleRegistry _scheduleRegistry;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private readonly LoggerService _loggerService;
    private readonly IWindowsTaskSchedulerAdapter _taskSchedulerAdapter;
    private readonly BackupController _backupController;

    public SchedulerService(
        ScheduleRegistry scheduleRegistry,
        BackupJobRegistry jobRegistry,
        StateService stateService,
        LoggerService loggerService,
        IWindowsTaskSchedulerAdapter taskSchedulerAdapter,
        BackupController backupController)
    {
        _scheduleRegistry = scheduleRegistry;
        _jobRegistry = jobRegistry;
        _stateService = stateService;
        _loggerService = loggerService;
        _taskSchedulerAdapter = taskSchedulerAdapter;
        _backupController = backupController;
    }

    public IReadOnlyList<BackupSchedule> LoadSchedules()
    {
        return _scheduleRegistry.LoadSchedules();
    }

    public BackupSchedule SaveSchedule(BackupSchedule schedule, string consoleRunnerPath)
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        BackupSchedule savedSchedule = _scheduleRegistry.SaveSchedule(schedule, jobs);

        if (savedSchedule.IsEnabled)
        {
            _taskSchedulerAdapter.UpsertScheduleTask(savedSchedule, consoleRunnerPath);
        }
        else
        {
            _taskSchedulerAdapter.DeleteScheduleTask(savedSchedule);
        }

        return savedSchedule;
    }

    public BackupSchedule DeleteSchedule(string scheduleId)
    {
        BackupSchedule removedSchedule = _scheduleRegistry.DeleteSchedule(scheduleId);
        _taskSchedulerAdapter.DeleteScheduleTask(removedSchedule);
        return removedSchedule;
    }

    public ScheduledRunResult RunSchedule(string scheduleId)
    {
        BackupSchedule schedule = _scheduleRegistry.GetSchedule(scheduleId);
        DateTime startedAtUtc = DateTime.UtcNow;

        if (!schedule.IsEnabled)
        {
            string disabledMessage = $"Schedule '{schedule.Name}' is disabled.";
            _scheduleRegistry.UpdateScheduleRunMetadata(schedule.Id, startedAtUtc, DateTime.UtcNow, "Skipped", disabledMessage);
            return new ScheduledRunResult
            {
                ScheduleId = schedule.Id,
                Status = "Skipped",
                Message = disabledMessage
            };
        }

        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        _stateService.SynchronizeConfiguredJobs(jobs);
        Dictionary<string, (BackupJob Job, int JobNumber)> jobsById = jobs
            .Select((job, index) => (Job: job, JobNumber: index + 1))
            .Where(item => !string.IsNullOrWhiteSpace(item.Job.Id))
            .ToDictionary(item => item.Job.Id, item => item, StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, BackupState> statesByJobName = _stateService.ReadAllStates()
            .ToDictionary(state => state.BackupName, StringComparer.OrdinalIgnoreCase);

        var selectedJobs = new List<SelectedBackupJob>();
        int skippedJobCount = 0;

        foreach (string jobId in schedule.TargetJobIds)
        {
            if (!jobsById.TryGetValue(jobId, out (BackupJob Job, int JobNumber) configuredJob))
            {
                skippedJobCount++;
                WriteScheduleLog(schedule, "ScheduleSkipped", $"Missing job id: {jobId}");
                continue;
            }

            if (IsJobBusy(configuredJob.Job, statesByJobName))
            {
                skippedJobCount++;
                WriteScheduleLog(schedule, "ScheduleSkipped", $"Job already active: {configuredJob.Job.Name}");
                continue;
            }

            selectedJobs.Add(new SelectedBackupJob
            {
                JobNumber = configuredJob.JobNumber,
                Job = configuredJob.Job
            });
        }

        IReadOnlyList<BackupResult> results = selectedJobs.Count == 0
            ? Array.Empty<BackupResult>()
            : _backupController.StartBackups(selectedJobs);

        string status = BuildStatus(results, skippedJobCount);
        string message = BuildMessage(schedule, selectedJobs.Count, skippedJobCount, results);
        _scheduleRegistry.UpdateScheduleRunMetadata(schedule.Id, startedAtUtc, DateTime.UtcNow, status, message);
        WriteScheduleLog(schedule, "ScheduleCompleted", message);

        return new ScheduledRunResult
        {
            ScheduleId = schedule.Id,
            Status = status,
            Message = message,
            StartedJobCount = selectedJobs.Count,
            SkippedJobCount = skippedJobCount,
            BackupResults = results
        };
    }

    private static bool IsJobBusy(BackupJob job, IReadOnlyDictionary<string, BackupState> statesByJobName)
    {
        if (!statesByJobName.TryGetValue(job.Name, out BackupState? state))
        {
            return false;
        }

        return state.IsRunning
            || state.Status is BackupExecutionStatus.Active
                or BackupExecutionStatus.Paused
                or BackupExecutionStatus.PausedByBusinessSoftware
                or BackupExecutionStatus.Stopping;
    }

    private static string BuildStatus(IReadOnlyList<BackupResult> results, int skippedJobCount)
    {
        if (results.Count == 0)
        {
            return skippedJobCount > 0 ? "Skipped" : "NoJobs";
        }

        return results.Any(result => result.Status != BackupExecutionStatus.Finished)
            ? "CompletedWithErrors"
            : skippedJobCount > 0 ? "CompletedWithSkips" : "Completed";
    }

    private static string BuildMessage(
        BackupSchedule schedule,
        int startedJobCount,
        int skippedJobCount,
        IReadOnlyList<BackupResult> results)
    {
        int errorCount = results.Count(result => result.Status != BackupExecutionStatus.Finished);
        return $"Schedule '{schedule.Name}' started {startedJobCount} job(s), skipped {skippedJobCount}, errors {errorCount}.";
    }

    private void WriteScheduleLog(BackupSchedule schedule, string actionType, string message)
    {
        _loggerService.WriteLog(new LogEntry
        {
            Timestamp = DateTime.Now,
            BackupName = schedule.Name,
            SourcePath = string.Empty,
            DestinationPath = string.Empty,
            ActionType = actionType,
            ErrorMessage = message,
            FileSizeBytes = 0,
            TransferTimeMilliseconds = 0,
            EncryptionTimeMilliseconds = 0
        });
    }
}
