namespace EasySave.Tests;

public class BackupHistoryServiceTests
{
    [Fact]
    public void GetLastFullBackupUtc_ReturnsNull_WhenHistoryDoesNotExist()
    {
        using var workspace = new TestWorkspace();
        var service = new BackupHistoryService();

        DateTime? timestamp = service.GetLastFullBackupUtc("Job1");

        Assert.Null(timestamp);
    }

    [Fact]
    public void SetLastFullBackupUtc_PersistsTimestamp()
    {
        using var workspace = new TestWorkspace();
        var service = new BackupHistoryService();
        DateTime expected = new(2026, 04, 24, 12, 00, 00, DateTimeKind.Utc);

        service.SetLastFullBackupUtc("Job1", expected);

        Assert.Equal(expected, service.GetLastFullBackupUtc("Job1"));
    }
}
