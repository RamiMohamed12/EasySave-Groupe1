using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ScheduleRegistry
{
    private readonly string _schedulesFilePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly AtomicRuntimeFileStore _fileStore;

    public ScheduleRegistry()
    {
        _schedulesFilePath = RuntimeStoragePaths.SchedulesFilePath;
        _fileStore = new AtomicRuntimeFileStore();
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public IReadOnlyList<BackupSchedule> LoadSchedules()
    {
        List<BackupSchedule>? schedules = _fileStore.ReadJson(
            _schedulesFilePath,
            _serializerOptions,
            static () => new List<BackupSchedule>());

        List<BackupSchedule> normalizedSchedules = NormalizeSchedules(schedules ?? new List<BackupSchedule>());
        SaveSchedules(normalizedSchedules);
        return normalizedSchedules;
    }

    public BackupSchedule GetSchedule(string scheduleId)
    {
        BackupSchedule? schedule = LoadSchedules()
            .FirstOrDefault(current => string.Equals(current.Id, scheduleId, StringComparison.OrdinalIgnoreCase));

        return schedule ?? throw new InvalidOperationException($"Schedule '{scheduleId}' was not found.");
    }

    public ScheduleValidationResult ValidateSchedule(BackupSchedule schedule, IReadOnlyList<BackupJob> jobs)
    {
        var result = new ScheduleValidationResult();
        if (schedule is null)
        {
            result.Errors.Add("Schedule is required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(schedule.Name))
        {
            result.Errors.Add("Schedule name is required.");
        }

        if (!TimeOnly.TryParseExact(schedule.LocalRunTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            result.Errors.Add("Schedule time must use HH:mm.");
        }

        if (schedule.Weekdays is null || schedule.Weekdays.Count == 0)
        {
            result.Errors.Add("Select at least one weekday.");
        }

        if (schedule.TargetJobIds is null || schedule.TargetJobIds.Count == 0)
        {
            result.Errors.Add("Select at least one backup job.");
        }
        else
        {
            var configuredJobIds = jobs
                .Select(job => job.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string jobId in schedule.TargetJobIds)
            {
                if (!configuredJobIds.Contains(jobId))
                {
                    result.Errors.Add($"Backup job '{jobId}' does not exist.");
                }
            }
        }

        return result;
    }

    public BackupSchedule SaveSchedule(BackupSchedule schedule, IReadOnlyList<BackupJob> jobs)
    {
        ScheduleValidationResult validationResult = ValidateSchedule(schedule, jobs);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, validationResult.Errors), nameof(schedule));
        }

        List<BackupSchedule> schedules = LoadSchedules().ToList();
        BackupSchedule normalizedSchedule = NormalizeSchedule(schedule);
        int existingIndex = schedules.FindIndex(current =>
            string.Equals(current.Id, normalizedSchedule.Id, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            schedules[existingIndex] = normalizedSchedule;
        }
        else
        {
            schedules.Add(normalizedSchedule);
        }

        SaveSchedules(schedules);
        return normalizedSchedule;
    }

    public BackupSchedule UpdateScheduleRunMetadata(
        string scheduleId,
        DateTime? startedAtUtc,
        DateTime completedAtUtc,
        string status,
        string message)
    {
        List<BackupSchedule> schedules = LoadSchedules().ToList();
        int scheduleIndex = schedules.FindIndex(current => string.Equals(current.Id, scheduleId, StringComparison.OrdinalIgnoreCase));
        if (scheduleIndex < 0)
        {
            throw new InvalidOperationException($"Schedule '{scheduleId}' was not found.");
        }

        BackupSchedule schedule = schedules[scheduleIndex];
        schedule.LastRunStartedAtUtc = startedAtUtc;
        schedule.LastRunCompletedAtUtc = completedAtUtc;
        schedule.LastRunStatus = status?.Trim() ?? string.Empty;
        schedule.LastRunMessage = message?.Trim() ?? string.Empty;
        schedules[scheduleIndex] = NormalizeSchedule(schedule);
        SaveSchedules(schedules);
        return schedules[scheduleIndex];
    }

    public BackupSchedule DeleteSchedule(string scheduleId)
    {
        List<BackupSchedule> schedules = LoadSchedules().ToList();
        int scheduleIndex = schedules.FindIndex(current => string.Equals(current.Id, scheduleId, StringComparison.OrdinalIgnoreCase));
        if (scheduleIndex < 0)
        {
            throw new InvalidOperationException($"Schedule '{scheduleId}' was not found.");
        }

        BackupSchedule removedSchedule = schedules[scheduleIndex];
        schedules.RemoveAt(scheduleIndex);
        SaveSchedules(schedules);
        return removedSchedule;
    }

    private void SaveSchedules(List<BackupSchedule> schedules)
    {
        _fileStore.WriteJson(_schedulesFilePath, schedules, _serializerOptions);
    }

    private static List<BackupSchedule> NormalizeSchedules(IReadOnlyList<BackupSchedule> schedules)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedSchedules = new List<BackupSchedule>(schedules.Count);

        foreach (BackupSchedule schedule in schedules)
        {
            BackupSchedule normalizedSchedule = NormalizeSchedule(schedule);
            if (string.IsNullOrWhiteSpace(normalizedSchedule.Id) || !usedIds.Add(normalizedSchedule.Id))
            {
                normalizedSchedule.Id = CreateScheduleId();
                normalizedSchedule.WindowsTaskName = CreateTaskName(normalizedSchedule.Id, normalizedSchedule.Name);
                usedIds.Add(normalizedSchedule.Id);
            }

            normalizedSchedules.Add(normalizedSchedule);
        }

        return normalizedSchedules;
    }

    private static BackupSchedule NormalizeSchedule(BackupSchedule schedule)
    {
        string id = string.IsNullOrWhiteSpace(schedule.Id)
            ? CreateScheduleId()
            : schedule.Id.Trim();
        string name = schedule.Name?.Trim() ?? string.Empty;

        List<string> targetJobIds = (schedule.TargetJobIds ?? new List<string>())
            .Select(jobId => jobId?.Trim() ?? string.Empty)
            .Where(jobId => !string.IsNullOrWhiteSpace(jobId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<DayOfWeek> weekdays = (schedule.Weekdays ?? new List<DayOfWeek>())
            .Distinct()
            .OrderBy(GetTaskSchedulerWeekdayOrder)
            .ToList();

        string localRunTime = TimeOnly.TryParse(schedule.LocalRunTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly runTime)
            ? runTime.ToString("HH:mm", CultureInfo.InvariantCulture)
            : schedule.LocalRunTime?.Trim() ?? string.Empty;

        string windowsTaskName = string.IsNullOrWhiteSpace(schedule.WindowsTaskName)
            ? CreateTaskName(id, name)
            : schedule.WindowsTaskName.Trim();

        return new BackupSchedule
        {
            Id = id,
            Name = name,
            IsEnabled = schedule.IsEnabled,
            TargetJobIds = targetJobIds,
            LocalRunTime = localRunTime,
            Weekdays = weekdays,
            LastRunStartedAtUtc = schedule.LastRunStartedAtUtc,
            LastRunCompletedAtUtc = schedule.LastRunCompletedAtUtc,
            LastRunStatus = schedule.LastRunStatus?.Trim() ?? string.Empty,
            LastRunMessage = schedule.LastRunMessage?.Trim() ?? string.Empty,
            WindowsTaskName = windowsTaskName
        };
    }

    private static int GetTaskSchedulerWeekdayOrder(DayOfWeek weekday)
    {
        return weekday == DayOfWeek.Sunday ? 7 : (int)weekday;
    }

    private static string CreateScheduleId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string CreateTaskName(string scheduleId, string scheduleName)
    {
        string safeName = new string((scheduleName ?? string.Empty)
            .Where(character => char.IsLetterOrDigit(character) || character == '-' || character == '_')
            .Take(32)
            .ToArray());

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "Schedule";
        }

        string suffix = string.IsNullOrWhiteSpace(scheduleId)
            ? Guid.NewGuid().ToString("N")[..8]
            : scheduleId[..Math.Min(8, scheduleId.Length)];

        return $"{safeName}-{suffix}";
    }
}
