using System.Text.Json;

namespace EasySave.Tests;

public class BackupServiceTests
{
    [Fact]
    public void StartBackup_ReturnsError_WhenSourcePathIsNotConfigured()
    {
        using var workspace = new TestWorkspace();
        BackupJob job = PrepareConfiguredSlot(1, source: "", target: workspace.CreateDirectory("target"));

        BackupResult result = CreateBackupService().StartBackup(CreateSelectedJob(1, job));

        Assert.Equal(BackupExecutionStatus.Error, result.Status);
        Assert.Contains("Source", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartBackup_FullBackupCopiesFiles_AndWritesRuntimeArtifacts()
    {
        using var workspace = new TestWorkspace();
        string sourceDirectory = workspace.CreateDirectory("source", "nested");
        string targetDirectory = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(sourceDirectory, "report.txt"), "monthly report");
        BackupJob job = PrepareConfiguredSlot(1, workspace.GetPath("source"), targetDirectory);

        BackupResult result = CreateBackupService().StartBackup(CreateSelectedJob(1, job));

        Assert.Equal(BackupExecutionStatus.Finished, result.Status);
        Assert.True(File.Exists(Path.Combine(targetDirectory, "nested", "report.txt")));

        List<LogEntry> logs = LoadLogEntries();
        Assert.Contains(logs, entry => entry.ActionType == "CreateDirectory");
        Assert.Contains(logs, entry => entry.ActionType == "FileTransfer");

        List<BackupState> states = LoadStates();
        Assert.Equal(BackupJobRegistry.MaximumJobs, states.Count);
        Assert.Contains(states, state => state.BackupName == "Job1" && state.Status == BackupExecutionStatus.Finished);
    }

    [Fact]
    public void StartBackup_DifferentialBackupCopiesOnlyChangedFiles()
    {
        using var workspace = new TestWorkspace();
        string sourceDirectory = workspace.CreateDirectory("source");
        string targetDirectory = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(sourceDirectory, "same.txt"), "same");
        File.WriteAllText(Path.Combine(sourceDirectory, "changed.txt"), "before");

        BackupJob fullJob = PrepareConfiguredSlot(1, sourceDirectory, targetDirectory);
        BackupService service = CreateBackupService();
        service.StartBackup(CreateSelectedJob(1, fullJob));

        Thread.Sleep(1100);
        File.WriteAllText(Path.Combine(sourceDirectory, "changed.txt"), "after");
        File.WriteAllText(Path.Combine(sourceDirectory, "new.txt"), "new");

        BackupJob differentialJob = new BackupJob
        {
            Name = fullJob.Name,
            Source = fullJob.Source,
            Target = fullJob.Target,
            Type = BackupType.Differential
        };

        BackupResult result = service.StartBackup(CreateSelectedJob(1, differentialJob));

        Assert.Equal(BackupExecutionStatus.Finished, result.Status);
        Assert.Equal("same", File.ReadAllText(Path.Combine(targetDirectory, "same.txt")));
        Assert.Equal("after", File.ReadAllText(Path.Combine(targetDirectory, "changed.txt")));
        Assert.Equal("new", File.ReadAllText(Path.Combine(targetDirectory, "new.txt")));
        Assert.Equal(2, result.TransferredFileCount);
    }

    [Fact]
    public void StartBackup_ErrorDuringCopy_WritesNegativeTransferTime()
    {
        using var workspace = new TestWorkspace();
        string sourceDirectory = workspace.CreateDirectory("source");
        string targetDirectory = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(sourceDirectory, "blocked.txt"), "content");
        Directory.CreateDirectory(Path.Combine(targetDirectory, "blocked.txt"));
        BackupJob job = PrepareConfiguredSlot(1, sourceDirectory, targetDirectory);

        BackupResult result = CreateBackupService().StartBackup(CreateSelectedJob(1, job));

        Assert.Equal(BackupExecutionStatus.Error, result.Status);
        LogEntry errorEntry = LoadLogEntries().Last(entry => entry.ActionType == "Error");
        Assert.True(errorEntry.TransferTimeMilliseconds < 0);
    }

    [Fact]
    public void StartBackup_WhenFileExtensionIsConfigured_LogsEncryptionTime()
    {
        using var workspace = new TestWorkspace();
        string sourceDirectory = workspace.CreateDirectory("source");
        string targetDirectory = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(sourceDirectory, "secret.txt"), "content");
        RuntimeStoragePaths.SetEncryptedExtensions([".txt"]);
        BackupJob job = PrepareConfiguredSlot(1, sourceDirectory, targetDirectory);
        var cryptoService = new FakeCryptoService(_ => 23);

        BackupResult result = CreateBackupService(cryptoService).StartBackup(CreateSelectedJob(1, job));

        Assert.Equal(BackupExecutionStatus.Finished, result.Status);
        LogEntry fileTransferEntry = LoadLogEntries().Last(entry => entry.ActionType == "FileTransfer");
        Assert.Equal(23, fileTransferEntry.EncryptionTimeMilliseconds);
        Assert.Equal(Path.Combine(targetDirectory, "secret.txt"), cryptoService.EncryptedFilePaths.Single());
    }

    [Fact]
    public void StartBackup_WhenCryptoSoftReturnsError_LogsNegativeEncryptionTime()
    {
        using var workspace = new TestWorkspace();
        string sourceDirectory = workspace.CreateDirectory("source");
        string targetDirectory = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(sourceDirectory, "secret.txt"), "content");
        RuntimeStoragePaths.SetEncryptedExtensions([".txt"]);
        BackupJob job = PrepareConfiguredSlot(1, sourceDirectory, targetDirectory);

        BackupResult result = CreateBackupService(new FakeCryptoService(_ => -97)).StartBackup(CreateSelectedJob(1, job));

        Assert.Equal(BackupExecutionStatus.Error, result.Status);
        LogEntry errorEntry = LoadLogEntries().Last(entry => entry.ActionType == "Error");
        Assert.Equal(-97, errorEntry.EncryptionTimeMilliseconds);
        Assert.True(errorEntry.TransferTimeMilliseconds >= 0);
    }

    [Fact]
    public void StartBackup_FullSuccess_UpdatesBackupHistory()
    {
        using var workspace = new TestWorkspace();
        string sourceDirectory = workspace.CreateDirectory("source");
        string targetDirectory = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(sourceDirectory, "a.txt"), "A");
        BackupJob job = PrepareConfiguredSlot(1, sourceDirectory, targetDirectory);

        BackupResult result = CreateBackupService().StartBackup(CreateSelectedJob(1, job));

        Assert.Equal(BackupExecutionStatus.Finished, result.Status);
        Assert.NotNull(new BackupHistoryService().GetLastFullBackupUtc("Job1"));
    }

    private static BackupService CreateBackupService(ICryptoService? cryptoService = null)
    {
        return new BackupService(
            new LoggerService(),
            new StateService(),
            new BackupHistoryService(),
            ApplicationTextService.Create(),
            cryptoService ?? new FakeCryptoService(_ => 0));
    }

    private static BackupJob PrepareConfiguredSlot(int jobNumber, string source, string target)
    {
        var registry = new BackupJobRegistry();
        registry.UpdateJobPath(jobNumber, JobPathField.Source, source);
        registry.UpdateJobPath(jobNumber, JobPathField.Target, target);
        return registry.LoadJobs()[jobNumber - 1];
    }

    private static SelectedBackupJob CreateSelectedJob(int jobNumber, BackupJob job)
    {
        return new SelectedBackupJob
        {
            JobNumber = jobNumber,
            Job = job
        };
    }

    private static List<LogEntry> LoadLogEntries()
    {
        string logFilePath = RuntimeStoragePaths.GetDailyLogFilePath(DateTime.Now);
        return ReadJsonBlocks(File.ReadAllLines(logFilePath))
            .Select(json => JsonSerializer.Deserialize<LogEntry>(json)!)
            .ToList();
    }

    private static IEnumerable<string> ReadJsonBlocks(IEnumerable<string> lines)
    {
        var blockLines = new List<string>();
        int depth = 0;

        foreach (string line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            blockLines.Add(line);
            depth += line.Count(character => character == '{');
            depth -= line.Count(character => character == '}');

            if (depth == 0 && blockLines.Count > 0)
            {
                yield return string.Join(Environment.NewLine, blockLines);
                blockLines.Clear();
            }
        }
    }

    private static List<BackupState> LoadStates()
    {
        string json = File.ReadAllText(RuntimeStoragePaths.StateFilePath);
        return JsonSerializer.Deserialize<List<BackupState>>(json, JsonTestHelper.SerializerOptions) ?? new List<BackupState>();
    }

    private sealed class FakeCryptoService : ICryptoService
    {
        private readonly Func<string, long> _encrypt;

        public FakeCryptoService(Func<string, long> encrypt)
        {
            _encrypt = encrypt;
            EncryptedFilePaths = new List<string>();
        }

        public List<string> EncryptedFilePaths { get; }

        public long EncryptIfRequired(string filePath)
        {
            if (!RuntimeStoragePaths.GetEncryptedExtensions().Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase))
            {
                return 0;
            }

            EncryptedFilePaths.Add(filePath);
            return _encrypt(filePath);
        }
    }
}
