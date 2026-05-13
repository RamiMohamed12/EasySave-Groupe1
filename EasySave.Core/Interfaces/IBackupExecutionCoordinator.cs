public interface IBackupExecutionCoordinator
{
    void RegisterPendingWork(IReadOnlyList<TransferWorkItem> workItems);
    Task AcquireTransferSlotAsync(TransferWorkItem workItem, CancellationToken cancellationToken);
    void MarkWorkCompleted(TransferWorkItem workItem);
    void ReleaseTransferSlot(TransferWorkItem workItem);
    bool HasPendingPriorityWork();
}
