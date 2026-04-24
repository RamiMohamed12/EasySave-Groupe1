using System.Text.Json;

public class BackupJobRegistry
{
    public const int MaximumJobs = 5;

    private readonly string _jobsFilePath;
    private readonly JsonSerializerOptions _serializerOptions;

    public BackupJobRegistry()
    {
        _jobsFilePath = RuntimeStoragePaths.JobsFilePath;
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public IReadOnlyList<BackupJob> LoadJobs()
    {
        EnsureJobsFileExists();

        string json = File.ReadAllText(_jobsFilePath);
        List<BackupJob>? jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);
        List<BackupJob> normalizedJobs = NormalizeJobs(jobs ?? new List<BackupJob>());

        SaveJobs(normalizedJobs);
        return normalizedJobs;
    }

    public BackupJob UpdateJobPath(int jobNumber, JobPathField pathField, string pathValue)
    {
        List<BackupJob> jobs = LoadJobs().ToList();
        int jobIndex = jobNumber - 1;

        if (jobIndex < 0 || jobIndex >= MaximumJobs)
        {
            throw new ArgumentOutOfRangeException(nameof(jobNumber));
        }

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
        List<BackupJob> defaultJobs = CreateDefaultJobs();

        for (int index = 0; index < MaximumJobs; index++)
        {
            if (index >= jobs.Count)
            {
                continue;
            }

            BackupJob existingJob = jobs[index] ?? new BackupJob();
            defaultJobs[index].Source = existingJob.Source ?? string.Empty;
            defaultJobs[index].Target = existingJob.Target ?? string.Empty;
        }

        return defaultJobs;
    }

    private static List<BackupJob> CreateDefaultJobs()
    {
        return new List<BackupJob>
        {
            new BackupJob { Name = "Job1", Source = string.Empty, Target = string.Empty, Type = BackupType.Full },
            new BackupJob { Name = "Job2", Source = string.Empty, Target = string.Empty, Type = BackupType.Differential },
            new BackupJob { Name = "Job3", Source = string.Empty, Target = string.Empty, Type = BackupType.Full },
            new BackupJob { Name = "Job4", Source = string.Empty, Target = string.Empty, Type = BackupType.Differential },
            new BackupJob { Name = "Job5", Source = string.Empty, Target = string.Empty, Type = BackupType.Full }
        };
    }
}
