public class PriorityTransferCoordinator : IBackupExecutionCoordinator
{
    private readonly object _syncRoot = new();
    private readonly List<TransferWorkItem> _pendingWorkItems = new();
    private int _activeTransferCount;
    private int _activeLargeTransferCount;

    public void RegisterPendingWork(IReadOnlyList<TransferWorkItem> workItems)
    {
        lock (_syncRoot)
        {
            _pendingWorkItems.AddRange(workItems);
        }
    }

    public async Task AcquireTransferSlotAsync(TransferWorkItem workItem, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                if (CanStartTransfer(workItem))
                {
                    _activeTransferCount++;
                    if (workItem.IsLargeFile)
                    {
                        _activeLargeTransferCount++;
                    }

                    RemovePendingWork(workItem);
                    return;
                }
            }

            await Task.Delay(25, cancellationToken);
        }
    }

    public void MarkWorkCompleted(TransferWorkItem workItem)
    {
        lock (_syncRoot)
        {
            RemovePendingWork(workItem);
        }
    }

    public void ReleaseTransferSlot(TransferWorkItem workItem)
    {
        lock (_syncRoot)
        {
            _activeTransferCount = Math.Max(0, _activeTransferCount - 1);
            if (workItem.IsLargeFile)
            {
                _activeLargeTransferCount = Math.Max(0, _activeLargeTransferCount - 1);
            }
        }
    }

    public bool HasPendingPriorityWork()
    {
        lock (_syncRoot)
        {
            return _pendingWorkItems.Any(item => item.Priority == FileTransferPriority.Priority);
        }
    }

    private bool CanStartTransfer(TransferWorkItem workItem)
    {
        if (_activeTransferCount >= RuntimeStoragePaths.GetMaxConcurrentJobs())
        {
            return false;
        }

        if (workItem.Priority == FileTransferPriority.Normal
            && _pendingWorkItems.Any(item => item.Priority == FileTransferPriority.Priority && !IsSameWork(item, workItem)))
        {
            return false;
        }

        if (workItem.IsLargeFile && _activeLargeTransferCount > 0)
        {
            return false;
        }

        return true;
    }

    private void RemovePendingWork(TransferWorkItem workItem)
    {
        int index = _pendingWorkItems.FindIndex(item => IsSameWork(item, workItem));
        if (index >= 0)
        {
            _pendingWorkItems.RemoveAt(index);
        }
    }

    private static bool IsSameWork(TransferWorkItem left, TransferWorkItem right)
    {
        return left.JobNumber == right.JobNumber
            && string.Equals(left.SourcePath, right.SourcePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.DestinationPath, right.DestinationPath, StringComparison.OrdinalIgnoreCase);
    }
}
