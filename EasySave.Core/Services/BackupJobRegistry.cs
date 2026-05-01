using System.Text.Json;
using System.Text.Json.Serialization;

public class BackupJobRegistry
{
    public const int DefaultJobCount = 5;
    public const int MaximumJobs = DefaultJobCount;

    private readonly string _jobsFilePath;
    private readonly JsonSerializerOptions _serializerOptions;

    public BackupJobRegistry()
    {
        _jobsFilePath = RuntimeStoragePaths.JobsFilePath;
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public IReadOnlyList<BackupJob> LoadJobs()
    {
        EnsureJobsFileExists();

        string json = File.ReadAllText(_jobsFilePath);
        List<BackupJob>? jobs = JsonSerializer.Deserialize<List<BackupJob>>(json, _serializerOptions);
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
        if (File.Exists(_jobsFilePath))
        {
            return;
        }

        List<BackupJob> defaultJobs = CreateDefaultJobs();

        try
        {
            using FileStream stream = new FileStream(_jobsFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(stream, defaultJobs, _serializerOptions);
        }
        catch (IOException) when (File.Exists(_jobsFilePath))
        {
            // Another process created the default configuration first.
        }
    }

    private void SaveJobs(List<BackupJob> jobs)
    {
        string json = JsonSerializer.Serialize(jobs, _serializerOptions);
        File.WriteAllText(_jobsFilePath, json);
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

        return new BackupJob
        {
            Name = template.Name,
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
