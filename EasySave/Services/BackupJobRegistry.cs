using System.Text.Json;

public class BackupJobRegistry
{
    private readonly string _jobsFilePath;

    public BackupJobRegistry()
    {
        _jobsFilePath = Path.Combine(RuntimeStoragePaths.BackupStateDirectory, "jobs.json");
    }

    public IReadOnlyList<BackupJob> LoadJobs()
    {
        EnsureJobsFileExists();

        string json = File.ReadAllText(_jobsFilePath);
        List<BackupJob>? jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);

        return jobs ?? new List<BackupJob>();
    }

    private void EnsureJobsFileExists()
    {
        if (File.Exists(_jobsFilePath))
        {
            return;
        }

        var sampleJobs = new List<BackupJob>
        {
            new BackupJob { Name = "Job1", Source = "/home/user/source1", Target = "/home/user/target1", Type = BackupType.Full },
            new BackupJob { Name = "Job2", Source = "/home/user/source2", Target = "/home/user/target2", Type = BackupType.Differential },
            new BackupJob { Name = "Job3", Source = "/home/user/source3", Target = "/home/user/target3", Type = BackupType.Full },
            new BackupJob { Name = "Job4", Source = "/home/user/source4", Target = "/home/user/target4", Type = BackupType.Differential },
            new BackupJob { Name = "Job5", Source = "/home/user/source5", Target = "/home/user/target5", Type = BackupType.Full }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(sampleJobs, options);
        File.WriteAllText(_jobsFilePath, json);
    }
}