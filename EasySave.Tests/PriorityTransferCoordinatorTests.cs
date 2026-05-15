namespace EasySave.Tests;

public class PriorityTransferCoordinatorTests
{
    [Fact]
    public async Task AcquireTransferSlotAsync_BlocksNormalTransfer_WhilePriorityWorkIsPending()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetMaxConcurrentJobs(2);
        RuntimeStoragePaths.SetLargeFileThresholdKb(0);

        var coordinator = new PriorityTransferCoordinator();
        TransferWorkItem priorityItem = new()
        {
            JobNumber = 1,
            SourcePath = @"C:\priority.txt",
            DestinationPath = @"D:\priority.txt",
            Priority = FileTransferPriority.Priority,
            FileSizeBytes = 100
        };
        TransferWorkItem normalItem = new()
        {
            JobNumber = 2,
            SourcePath = @"C:\normal.txt",
            DestinationPath = @"D:\normal.txt",
            Priority = FileTransferPriority.Normal,
            FileSizeBytes = 100
        };

        coordinator.RegisterPendingWork([priorityItem, normalItem]);
        Task normalAcquireTask = coordinator.AcquireTransferSlotAsync(normalItem, CancellationToken.None);
        await Task.Delay(100);

        Assert.False(normalAcquireTask.IsCompleted);

        await coordinator.AcquireTransferSlotAsync(priorityItem, CancellationToken.None);
        coordinator.MarkWorkCompleted(priorityItem);
        coordinator.ReleaseTransferSlot(priorityItem);

        await normalAcquireTask;
    }

    [Fact]
    public async Task AcquireTransferSlotAsync_AllowsOnlyOneLargeTransferAtATime()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLargeFileThresholdKb(1);
        RuntimeStoragePaths.SetMaxConcurrentJobs(3);

        var coordinator = new PriorityTransferCoordinator();
        TransferWorkItem firstLargeItem = new()
        {
            JobNumber = 1,
            SourcePath = @"C:\large-a.bin",
            DestinationPath = @"D:\large-a.bin",
            Priority = FileTransferPriority.Normal,
            FileSizeBytes = 4 * 1024
        };
        TransferWorkItem secondLargeItem = new()
        {
            JobNumber = 2,
            SourcePath = @"C:\large-b.bin",
            DestinationPath = @"D:\large-b.bin",
            Priority = FileTransferPriority.Normal,
            FileSizeBytes = 8 * 1024
        };

        coordinator.RegisterPendingWork([firstLargeItem, secondLargeItem]);
        await coordinator.AcquireTransferSlotAsync(firstLargeItem, CancellationToken.None);

        Task secondAcquireTask = coordinator.AcquireTransferSlotAsync(secondLargeItem, CancellationToken.None);
        await Task.Delay(100);
        Assert.False(secondAcquireTask.IsCompleted);

        coordinator.MarkWorkCompleted(firstLargeItem);
        coordinator.ReleaseTransferSlot(firstLargeItem);

        await secondAcquireTask;
    }

    [Fact]
    public async Task AcquireTransferSlotAsync_RespectsMaxConcurrentJobs()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLargeFileThresholdKb(0);
        RuntimeStoragePaths.SetMaxConcurrentJobs(1);

        var coordinator = new PriorityTransferCoordinator();
        TransferWorkItem firstItem = new()
        {
            JobNumber = 1,
            SourcePath = @"C:\first.txt",
            DestinationPath = @"D:\first.txt",
            Priority = FileTransferPriority.Normal,
            FileSizeBytes = 100
        };
        TransferWorkItem secondItem = new()
        {
            JobNumber = 2,
            SourcePath = @"C:\second.txt",
            DestinationPath = @"D:\second.txt",
            Priority = FileTransferPriority.Normal,
            FileSizeBytes = 100
        };

        coordinator.RegisterPendingWork([firstItem, secondItem]);
        await coordinator.AcquireTransferSlotAsync(firstItem, CancellationToken.None);

        Task secondAcquireTask = coordinator.AcquireTransferSlotAsync(secondItem, CancellationToken.None);
        await Task.Delay(100);
        Assert.False(secondAcquireTask.IsCompleted);

        coordinator.MarkWorkCompleted(firstItem);
        coordinator.ReleaseTransferSlot(firstItem);

        await secondAcquireTask;
    }

    [Fact]
    public async Task HasPendingPriorityWork_BecomesFalse_AfterPriorityWorkStarts()
    {
        using var workspace = new TestWorkspace();
        RuntimeStoragePaths.SetLargeFileThresholdKb(0);
        RuntimeStoragePaths.SetMaxConcurrentJobs(2);

        var coordinator = new PriorityTransferCoordinator();
        TransferWorkItem priorityItem = new()
        {
            JobNumber = 1,
            SourcePath = @"C:\priority.txt",
            DestinationPath = @"D:\priority.txt",
            Priority = FileTransferPriority.Priority,
            FileSizeBytes = 100
        };

        coordinator.RegisterPendingWork([priorityItem]);
        Assert.True(coordinator.HasPendingPriorityWork());

        await coordinator.AcquireTransferSlotAsync(priorityItem, CancellationToken.None);
        Assert.False(coordinator.HasPendingPriorityWork());

        coordinator.MarkWorkCompleted(priorityItem);
        coordinator.ReleaseTransferSlot(priorityItem);
        Assert.False(coordinator.HasPendingPriorityWork());
    }
}
