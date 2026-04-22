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

        string sampleRootDirectory = GetSampleRootDirectory();
        var sampleJobs = new List<BackupJob>
        {
            new BackupJob { Name = "Job1", Source = Path.Combine(sampleRootDirectory, "Source1"), Target = Path.Combine(sampleRootDirectory, "Target1"), Type = BackupType.Full },
            new BackupJob { Name = "Job2", Source = Path.Combine(sampleRootDirectory, "Source2"), Target = Path.Combine(sampleRootDirectory, "Target2"), Type = BackupType.Differential },
            new BackupJob { Name = "Job3", Source = Path.Combine(sampleRootDirectory, "Source3"), Target = Path.Combine(sampleRootDirectory, "Target3"), Type = BackupType.Full },
            new BackupJob { Name = "Job4", Source = Path.Combine(sampleRootDirectory, "Source4"), Target = Path.Combine(sampleRootDirectory, "Target4"), Type = BackupType.Differential },
            new BackupJob { Name = "Job5", Source = Path.Combine(sampleRootDirectory, "Source5"), Target = Path.Combine(sampleRootDirectory, "Target5"), Type = BackupType.Full }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        try
        {
            using FileStream stream = new FileStream(_jobsFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(stream, sampleJobs, options);
        }
        catch (IOException) when (File.Exists(_jobsFilePath))
        {
            // Another process created the default configuration first.
        }
    }

    private static string GetSampleRootDirectory()
    {
        string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = RuntimeStoragePaths.BackupStateDirectory;
        }

        return Path.Combine(baseDirectory, "EasySaveSamples");
    }
}
