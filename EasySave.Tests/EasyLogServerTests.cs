using System.Text.Json;

namespace EasySave.Tests;

public class EasyLogServerTests
{
    [Fact]
    public async Task AppendAsync_WritesOneJsonLineToDailyFile()
    {
        using var workspace = new TestWorkspace();
        string logsDirectory = workspace.CreateDirectory("central-logs");
        var store = new EasyLogFileStore(logsDirectory);

        await store.AppendAsync(CreateEntry("alice", "client-1"));

        string logPath = Path.Combine(logsDirectory, "2026-05-16.jsonl");
        Assert.True(File.Exists(logPath));
        string line = Assert.Single(File.ReadAllLines(logPath));
        using JsonDocument document = JsonDocument.Parse(line);
        Assert.Equal("alice", document.RootElement.GetProperty(nameof(LogEntry.UserName)).GetString());
    }

    [Fact]
    public async Task AppendAsync_WithConcurrentEntries_KeepsOneDailyFile()
    {
        using var workspace = new TestWorkspace();
        string logsDirectory = workspace.CreateDirectory("central-logs");
        var store = new EasyLogFileStore(logsDirectory);

        Task[] writes = Enumerable.Range(0, 50)
            .Select(index => store.AppendAsync(CreateEntry($"user-{index % 2}", $"client-{index}")))
            .ToArray();
        await Task.WhenAll(writes);

        string[] files = Directory.GetFiles(logsDirectory, "*.jsonl");
        Assert.Single(files);
        Assert.Equal(50, File.ReadAllLines(files[0]).Length);
        string content = File.ReadAllText(files[0]);
        Assert.Contains("\"UserName\":\"user-0\"", content);
        Assert.Contains("\"UserName\":\"user-1\"", content);
        Assert.Contains("\"ClientId\":\"client-49\"", content);
    }

    [Fact]
    public async Task ReadDailyLogAsync_ReturnsEmptyStringWhenFileDoesNotExist()
    {
        using var workspace = new TestWorkspace();
        var store = new EasyLogFileStore(workspace.CreateDirectory("central-logs"));

        string? content = await store.ReadDailyLogAsync("2026-05-16");

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task ReadDailyLogAsync_ReturnsNullForInvalidDate()
    {
        using var workspace = new TestWorkspace();
        var store = new EasyLogFileStore(workspace.CreateDirectory("central-logs"));

        string? content = await store.ReadDailyLogAsync("16-05-2026");

        Assert.Null(content);
    }

    [Fact]
    public void IsValid_RejectsInvalidPayloads()
    {
        bool isValid = EasyLogFileStore.IsValid(
            new LogEntry
            {
                Timestamp = new DateTime(2026, 05, 16, 12, 0, 0),
                UserName = "alice",
                MachineName = "PC-1"
            },
            out string errorMessage);

        Assert.False(isValid);
        Assert.Equal("ClientId is required.", errorMessage);
    }

    private static LogEntry CreateEntry(string userName, string clientId)
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 05, 16, 12, 0, 0),
            BackupName = "Job1",
            SourcePath = @"C:\Source\a.txt",
            DestinationPath = @"D:\Backup\a.txt",
            ActionType = "FileTransfer",
            UserName = userName,
            MachineName = "PC-1",
            ClientId = clientId,
            FileSizeBytes = 42,
            TransferTimeMilliseconds = 12,
            EncryptionTimeMilliseconds = 0
        };
    }
}
