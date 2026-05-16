using System.Net;
using System.Text.Json;

namespace EasySave.Tests;

public class LoggerServiceCentralizationTests
{
    [Fact]
    public void WriteLog_EnrichesEntryWithUserMachineAndClientId()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetCentralLogUserName("alice");
        var logger = new LoggerService();
        var entry = new LogEntry
        {
            Timestamp = new DateTime(2026, 05, 16, 12, 0, 0),
            BackupName = "Job1",
            ActionType = "FileTransfer"
        };

        logger.WriteLog(entry);

        Assert.Equal("alice", entry.UserName);
        Assert.Equal(Environment.MachineName, entry.MachineName);
        Assert.Equal(RuntimeStoragePaths.GetClientId(), entry.ClientId);
    }

    [Fact]
    public void WriteLog_WhenCentralizedOnly_SendsToServerWithoutCreatingLocalFile()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLogStorageMode(RuntimeStoragePaths.CentralizedLogStorageMode);
        RuntimeStoragePaths.SetCentralLogServerUrl("http://localhost:5080");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var logger = CreateLogger(handler);
        DateTime timestamp = new(2026, 05, 16, 12, 0, 0);

        logger.WriteLog(new LogEntry { Timestamp = timestamp, BackupName = "Job1", ActionType = "FileTransfer" });

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("http://localhost:5080/api/logs", handler.Requests[0].RequestUri?.ToString());
        Assert.False(File.Exists(RuntimeStoragePaths.GetDailyLogFilePath(timestamp)));
    }

    [Fact]
    public void WriteLog_WhenBoth_WritesLocalAndSendsToServer()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLogStorageMode(RuntimeStoragePaths.BothLogStorageMode);
        RuntimeStoragePaths.SetCentralLogServerUrl("http://localhost:5080");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var logger = CreateLogger(handler);
        DateTime timestamp = new(2026, 05, 16, 12, 0, 0);

        logger.WriteLog(new LogEntry { Timestamp = timestamp, BackupName = "Job1", ActionType = "FileTransfer" });

        Assert.Single(handler.Requests);
        Assert.True(File.Exists(RuntimeStoragePaths.GetDailyLogFilePath(timestamp)));
    }

    [Fact]
    public void WriteLog_WhenCentralServerIsUnavailable_DoesNotCreateLocalFileInCentralizedMode()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLogStorageMode(RuntimeStoragePaths.CentralizedLogStorageMode);
        RuntimeStoragePaths.SetCentralLogServerUrl("http://localhost:5080");
        var handler = new CapturingHandler(_ => throw new HttpRequestException("server unavailable"));
        var logger = CreateLogger(handler);
        DateTime timestamp = new(2026, 05, 16, 12, 0, 0);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            logger.WriteLog(new LogEntry { Timestamp = timestamp, BackupName = "Job1", ActionType = "FileTransfer" }));

        Assert.Contains("Central log write failed", exception.Message);
        Assert.False(File.Exists(RuntimeStoragePaths.GetDailyLogFilePath(timestamp)));
    }

    [Fact]
    public void WriteLog_WhenCentralApiKeyIsConfigured_SendsHeader()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLogStorageMode(RuntimeStoragePaths.CentralizedLogStorageMode);
        RuntimeStoragePaths.SetCentralLogServerUrl("http://localhost:5080");
        RuntimeStoragePaths.SetCentralLogApiKey("secret");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var logger = CreateLogger(handler);

        logger.WriteLog(new LogEntry { Timestamp = new DateTime(2026, 05, 16, 12, 0, 0), BackupName = "Job1", ActionType = "FileTransfer" });

        Assert.True(handler.Requests[0].Headers.TryGetValues(CentralLogClient.ApiKeyHeaderName, out IEnumerable<string>? values));
        Assert.Equal("secret", Assert.Single(values));
    }

    [Fact]
    public void LogEntry_DeserializesOldLogsWithoutCentralFields()
    {
        string json = """
        {
          "Timestamp": "2026-05-16T12:00:00",
          "BackupName": "Job1",
          "ActionType": "FileTransfer"
        }
        """;

        LogEntry? entry = JsonSerializer.Deserialize<LogEntry>(json);

        Assert.NotNull(entry);
        Assert.Equal(string.Empty, entry.UserName);
        Assert.Equal(string.Empty, entry.MachineName);
        Assert.Equal(string.Empty, entry.ClientId);
    }

    private static LoggerService CreateLogger(CapturingHandler handler)
    {
        return new LoggerService(new CentralLogClient(new HttpClient(handler)));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responseFactory(request));
        }
    }
}
