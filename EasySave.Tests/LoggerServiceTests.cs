using System.Text.Json;
using System.Xml.Linq;

namespace EasySave.Tests;

public class LoggerServiceTests
{
    [Fact]
    public void WriteLog_WritesEntryToDailyLogFile()
    {
        using var workspace = new TestWorkspace();
        var logger = new LoggerService();
        DateTime timestamp = new(2026, 04, 24, 10, 30, 00);

        logger.WriteLog(new LogEntry
        {
            Timestamp = timestamp,
            BackupName = "Job1",
            SourcePath = @"C:\Source\a.txt",
            DestinationPath = @"D:\Backup\a.txt",
            ActionType = "FileTransfer",
            FileSizeBytes = 42,
            TransferTimeMilliseconds = 12
        });

        string logFilePath = RuntimeStoragePaths.GetDailyLogFilePath(timestamp);
        Assert.True(File.Exists(logFilePath));

        string logContent = File.ReadAllText(logFilePath).Trim();
        Assert.Contains(Environment.NewLine, logContent);
        Assert.Contains("  \"ActionType\": \"FileTransfer\"", logContent);

        LogEntry? entry = JsonSerializer.Deserialize<LogEntry>(logContent);
        Assert.NotNull(entry);
        Assert.Equal("FileTransfer", entry.ActionType);
        Assert.Equal(42, entry.FileSizeBytes);
    }

    [Fact]
    public void WriteLog_AppendsInsteadOfOverwriting()
    {
        using var workspace = new TestWorkspace();
        var logger = new LoggerService();
        DateTime timestamp = new(2026, 04, 24, 10, 30, 00);

        logger.WriteLog(new LogEntry { Timestamp = timestamp, BackupName = "Job1", ActionType = "CreateDirectory" });
        logger.WriteLog(new LogEntry { Timestamp = timestamp, BackupName = "Job1", ActionType = "FileTransfer" });

        string logContent = File.ReadAllText(RuntimeStoragePaths.GetDailyLogFilePath(timestamp));
        Assert.Equal(2, CountLogEntries(logContent));
    }

    [Fact]
    public void WriteLog_WritesXmlWhenConfigured()
    {
        using var workspace = new TestWorkspace();
        var logger = new LoggerService();
        DateTime timestamp = new(2026, 04, 24, 10, 30, 00);

        RuntimeStoragePaths.SetLogFileFormat("xml");

        logger.WriteLog(new LogEntry
        {
            Timestamp = timestamp,
            BackupName = "Job1",
            SourcePath = @"C:\Source\a.txt",
            DestinationPath = @"D:\Backup\a.txt",
            ActionType = "FileTransfer",
            FileSizeBytes = 42,
            TransferTimeMilliseconds = 12
        });

        string logFilePath = RuntimeStoragePaths.GetDailyLogFilePath(timestamp);
        Assert.EndsWith(".xml", logFilePath);
        Assert.True(File.Exists(logFilePath));

        XDocument document = XDocument.Load(logFilePath);
        XElement? entry = document.Root?.Element("LogEntry");

        Assert.NotNull(entry);
        Assert.Equal("FileTransfer", entry?.Element(nameof(LogEntry.ActionType))?.Value);
        Assert.Equal("42", entry?.Element(nameof(LogEntry.FileSizeBytes))?.Value);
    }

    private static int CountLogEntries(string logContent)
    {
        return logContent
            .Split('}', StringSplitOptions.RemoveEmptyEntries)
            .Count(entryBlock => entryBlock.Contains("\"ActionType\""));
    }
}
