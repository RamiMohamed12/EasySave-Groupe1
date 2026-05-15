using System.Text.Json;
using System.Text.Json.Serialization;

public class BackupJobRegistry
{
    public const int DefaultJobCount = 5;
    public const int MaximumJobs = DefaultJobCount;

    private readonly string _jobsFilePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly AtomicRuntimeFileStore _fileStore;

    public BackupJobRegistry()
    {
        _jobsFilePath = RuntimeStoragePaths.JobsFilePath;
        _fileStore = new AtomicRuntimeFileStore();
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public IReadOnlyList<BackupJob> LoadJobs()
    {
        EnsureJobsFileExists();

        List<BackupJob>? jobs = _fileStore.ReadJson(_jobsFilePath, _serializerOptions, static () => new List<BackupJob>());
        List<BackupJob> normalizedJobs = NormalizeJobs(jobs ?? new List<BackupJob>());

        SaveJobs(normalizedJobs);
        return normalizedJobs;
    }

    public BackupJob UpdateJobPath(int jobNumber, JobPathField pathField, string pathValue)
    {
        List<BackupJob> jobs = LoadJobs().ToList();
        int jobIndex = GetJobIndex(jobNumber);
        EnsureJobCapacity(jobs, jobIndex);

        BackupJob job = jobs[jobIndex];

        if (pathField == JobPathField.Source)
        {
            job.Source = pathValue;
        }
        else
        {
            job.Target = pathValue;
        }

        SaveJobs(jobs);
        return job;
    }

    public BackupJob CreateJob(int jobNumber, BackupJob job)
    {
        return SaveJob(jobNumber, job);
    }

    public BackupJob UpdateJob(int jobNumber, BackupJob job)
    {
        return SaveJob(jobNumber, job);
    }

    public BackupJob DeleteJob(int jobNumber)
    {
        List<BackupJob> jobs = LoadJobs().ToList();
        int jobIndex = GetJobIndex(jobNumber);
        if (jobIndex >= jobs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(jobNumber));
        }

        BackupJob removedJob = jobs[jobIndex];
        jobs.RemoveAt(jobIndex);
        SaveJobs(jobs);
        return removedJob;
    }

    private void EnsureJobsFileExists()
    {
        string? existingJson = _fileStore.ReadAllText(_jobsFilePath);
        if (!string.IsNullOrWhiteSpace(existingJson))
        {
            return;
        }

        List<BackupJob> defaultJobs = CreateDefaultJobs();
        _fileStore.WriteJson(_jobsFilePath, defaultJobs, _serializerOptions);
    }

    private void SaveJobs(List<BackupJob> jobs)
    {
        _fileStore.WriteJson(_jobsFilePath, jobs, _serializerOptions);
    }

    private static List<BackupJob> NormalizeJobs(IReadOnlyList<BackupJob> jobs)
    {
        int targetCount = jobs.Count;
        var normalizedJobs = new List<BackupJob>(targetCount);
        for (int index = 0; index < targetCount; index++)
        {
            if (index < jobs.Count)
            {
                BackupJob existingJob = jobs[index] ?? CreateDefaultJob(index);
                normalizedJobs.Add(NormalizeJob(existingJob, index));
                continue;
            }

            normalizedJobs.Add(CreateDefaultJob(index));
        }

        return normalizedJobs;
    }

    private static List<BackupJob> CreateDefaultJobs()
    {
        var jobs = new List<BackupJob>(DefaultJobCount);
        for (int index = 0; index < DefaultJobCount; index++)
        {
            jobs.Add(CreateDefaultJob(index));
        }

        return jobs;
    }

    private static BackupJob CreateDefaultJob(int jobIndex)
    {
        return new BackupJob
        {
            Name = $"Job{jobIndex + 1}",
            Source = string.Empty,
            Target = string.Empty,
            Type = jobIndex % 2 == 0 ? BackupType.Full : BackupType.Differential
        };
    }

    private static int GetJobIndex(int jobNumber)
    {
        int jobIndex = jobNumber - 1;
        if (jobIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jobNumber));
        }

        return jobIndex;
    }

    private BackupJob SaveJob(int jobNumber, BackupJob job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        List<BackupJob> jobs = LoadJobs().ToList();
        int jobIndex = GetJobIndex(jobNumber);
        EnsureJobCapacity(jobs, jobIndex);
        BackupJob savedJob = NormalizeJob(job, jobIndex);
        jobs[jobIndex] = savedJob;
        SaveJobs(jobs);
        return savedJob;
    }

    private static BackupJob NormalizeJob(BackupJob job, int jobIndex)
    {
        BackupJob template = CreateDefaultJob(jobIndex);
        string name = string.IsNullOrWhiteSpace(job.Name) ? template.Name : job.Name.Trim();

        return new BackupJob
        {
            Name = name,
            Source = job.Source?.Trim() ?? string.Empty,
            Target = job.Target?.Trim() ?? string.Empty,
            Type = job.Type
        };
    }

    private static void EnsureJobCapacity(List<BackupJob> jobs, int jobIndex)
    {
        while (jobs.Count <= jobIndex)
        {
            jobs.Add(CreateDefaultJob(jobs.Count));
        }
    }
}
