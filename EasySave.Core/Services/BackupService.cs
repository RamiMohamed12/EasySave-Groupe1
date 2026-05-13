using System.Diagnostics;

public class BackupService : IBackupService
{
    private const int CopyBufferSize = 64 * 1024;

    private readonly BackupHistoryService _backupHistoryService;
    private readonly LoggerService _loggerService;
    private readonly StateService _stateService;
    private readonly ApplicationTextService _textService;
    private readonly ICryptoService _cryptoService;
    private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;
    private readonly IBackupExecutionController _executionController;
    private readonly IBackupExecutionCoordinator _executionCoordinator;

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService)
        : this(
            loggerService,
            stateService,
            backupHistoryService,
            textService,
            new CryptoSoftService(),
            new BusinessSoftwareMonitor(),
            new InMemoryBackupExecutionController(),
            new PriorityTransferCoordinator())
    {
    }

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService,
        ICryptoService cryptoService)
        : this(
            loggerService,
            stateService,
            backupHistoryService,
            textService,
            cryptoService,
            new BusinessSoftwareMonitor(),
            new InMemoryBackupExecutionController(),
            new PriorityTransferCoordinator())
    {
    }

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService,
        IBusinessSoftwareMonitor businessSoftwareMonitor)
        : this(
            loggerService,
            stateService,
            backupHistoryService,
            textService,
            new CryptoSoftService(),
            businessSoftwareMonitor,
            new InMemoryBackupExecutionController(),
            new PriorityTransferCoordinator())
    {
    }

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService,
        ICryptoService cryptoService,
        IBusinessSoftwareMonitor businessSoftwareMonitor)
        : this(
            loggerService,
            stateService,
            backupHistoryService,
            textService,
            cryptoService,
            businessSoftwareMonitor,
            new InMemoryBackupExecutionController(),
            new PriorityTransferCoordinator())
    {
    }

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService,
        ICryptoService cryptoService,
        IBusinessSoftwareMonitor businessSoftwareMonitor,
        IBackupExecutionController executionController,
        IBackupExecutionCoordinator executionCoordinator)
    {
        _loggerService = loggerService;
        _stateService = stateService;
        _backupHistoryService = backupHistoryService;
        _textService = textService;
        _cryptoService = cryptoService;
        _businessSoftwareMonitor = businessSoftwareMonitor;
        _executionController = executionController;
        _executionCoordinator = executionCoordinator;
    }

    public IBackupExecutionController ExecutionController => _executionController;

    public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
    {
        var globalStopwatch = Stopwatch.StartNew();
        BackupJob backupJob = selectedBackupJob.Job;
        var result = new BackupResult
        {
            JobNumber = selectedBackupJob.JobNumber,
            BackupName = backupJob.Name
        };

        _executionController.BeginJobRun(selectedBackupJob.JobNumber);

        try
        {
            if (string.IsNullOrWhiteSpace(backupJob.Source))
            {
                return CompleteWithValidationError(result, backupJob, _textService.GetSourcePathRequiredMessage());
            }

            if (string.IsNullOrWhiteSpace(backupJob.Target))
            {
                return CompleteWithValidationError(result, backupJob, _textService.GetTargetPathRequiredMessage());
            }

            if (!Directory.Exists(backupJob.Source))
            {
                return CompleteWithValidationError(result, backupJob, _textService.GetSourceDirectoryMissingMessage(selectedBackupJob));
            }

            string[] sourceFiles = Directory.GetFiles(backupJob.Source, "*", SearchOption.AllDirectories);
            DateTime? lastFullBackupUtc = backupJob.Type == BackupType.Differential
                ? _backupHistoryService.GetLastFullBackupUtc(backupJob.Name)
                : null;
            List<TransferWorkItem> workItems = BuildTransferWorkItems(selectedBackupJob, sourceFiles, lastFullBackupUtc);
            long totalBytes = workItems.Sum(item => item.FileSizeBytes);

            var state = CreateInitialState(backupJob, totalBytes, workItems.Count);
            _executionCoordinator.RegisterPendingWork(workItems);
            _stateService.WriteState(state);

            var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasErrors = false;

            foreach (TransferWorkItem workItem in workItems)
            {
                BackupControlAction controlAction = WaitUntilJobCanProceed(selectedBackupJob, state);
                if (controlAction == BackupControlAction.Stop)
                {
                    globalStopwatch.Stop();
                    return CompleteWithStop(result, state, globalStopwatch.Elapsed, selectedBackupJob, manualStop: true);
                }

                bool slotAcquired = false;
                long fileTransferredBytes = 0;
                bool fileCopiedCompletely = false;
                long transferTimeMilliseconds = 0;
                long encryptionTimeMilliseconds = 0;
                var fileStopwatch = Stopwatch.StartNew();

                try
                {
                    _executionCoordinator.AcquireTransferSlotAsync(workItem, CancellationToken.None).GetAwaiter().GetResult();
                    slotAcquired = true;
                    state.IsPriorityWorkPending = _executionCoordinator.HasPendingPriorityWork();
                    state.CurrentSourcePath = workItem.SourcePath;
                    state.CurrentTargetPath = workItem.DestinationPath;
                    state.CurrentFileSize = workItem.FileSizeBytes;
                    state.CurrentFilePriority = workItem.Priority;
                    state.IsLargeFileTransfer = workItem.IsLargeFile;
                    state.LastBackupUpdateTime = DateTime.Now;
                    state.Status = BackupExecutionStatus.Active;
                    state.IsRunning = true;
                    state.RequestedAction = BackupControlAction.Run;
                    state.PauseReason = BackupPauseReason.None;
                    state.PauseReasonDetails = string.Empty;
                    _stateService.WriteState(state);

                    EnsureDestinationDirectoryExists(
                        backupJob.Name,
                        workItem.SourcePath,
                        Path.GetDirectoryName(workItem.DestinationPath),
                        createdDirectories);

                    CopyFileWithRuntimeControl(selectedBackupJob, workItem, state, ref fileTransferredBytes);
                    fileCopiedCompletely = true;
                    fileStopwatch.Stop();
                    transferTimeMilliseconds = fileStopwatch.ElapsedMilliseconds;
                    encryptionTimeMilliseconds = _cryptoService.EncryptIfRequired(workItem.DestinationPath);

                    if (encryptionTimeMilliseconds < 0)
                    {
                        throw new InvalidOperationException($"CryptoSoft failed with code {encryptionTimeMilliseconds}.");
                    }

                    DateTime transferTimestamp = DateTime.Now;
                    state.RemainingFileCount -= 1;
                    state.ErrorMessage = string.Empty;
                    state.LastBackupUpdateTime = transferTimestamp;
                    var transferredFile = new BackupTransferredFile
                    {
                        Timestamp = transferTimestamp,
                        SourcePath = workItem.SourcePath,
                        DestinationPath = workItem.DestinationPath,
                        FileSizeBytes = workItem.FileSizeBytes,
                        TransferTimeMilliseconds = transferTimeMilliseconds,
                        EncryptionTimeMilliseconds = encryptionTimeMilliseconds
                    };
                    state.LastRunTransferredFiles.Add(transferredFile);

                    _stateService.WriteState(state);
                    _loggerService.WriteLog(new LogEntry
                    {
                        Timestamp = transferTimestamp,
                        BackupName = backupJob.Name,
                        SourcePath = workItem.SourcePath,
                        DestinationPath = workItem.DestinationPath,
                        ActionType = "FileTransfer",
                        FileSizeBytes = workItem.FileSizeBytes,
                        TransferTimeMilliseconds = transferTimeMilliseconds,
                        EncryptionTimeMilliseconds = encryptionTimeMilliseconds
                    });
                }
                catch (OperationCanceledException)
                {
                    fileStopwatch.Stop();
                    RollBackCurrentFileProgress(state, fileTransferredBytes);
                    DeletePartialFile(workItem.DestinationPath);
                    _executionCoordinator.MarkWorkCompleted(workItem);
                    if (slotAcquired)
                    {
                        _executionCoordinator.ReleaseTransferSlot(workItem);
                    }

                    globalStopwatch.Stop();
                    return CompleteWithStop(result, state, globalStopwatch.Elapsed, selectedBackupJob, manualStop: true);
                }
                catch (Exception exception)
                {
                    if (fileStopwatch.IsRunning)
                    {
                        fileStopwatch.Stop();
                    }

                    if (!fileCopiedCompletely)
                    {
                        RollBackCurrentFileProgress(state, fileTransferredBytes);
                        DeletePartialFile(workItem.DestinationPath);
                    }

                    hasErrors = true;
                    DateTime transferTimestamp = DateTime.Now;
                    long loggedTransferTime = fileCopiedCompletely
                        ? Math.Max(0, fileStopwatch.ElapsedMilliseconds)
                        : -Math.Max(1, fileStopwatch.ElapsedMilliseconds);
                    long loggedEncryptionTime = encryptionTimeMilliseconds < 0
                        ? encryptionTimeMilliseconds
                        : 0;

                    state.LastBackupUpdateTime = transferTimestamp;
                    state.RemainingFileCount = Math.Max(0, state.RemainingFileCount - 1);
                    state.Status = BackupExecutionStatus.Error;
                    state.IsRunning = false;
                    state.ErrorMessage = exception.Message;
                    state.IsLargeFileTransfer = false;
                    state.CurrentFilePriority = workItem.Priority;

                    _stateService.WriteState(state);
                    _loggerService.WriteLog(new LogEntry
                    {
                        Timestamp = transferTimestamp,
                        BackupName = backupJob.Name,
                        SourcePath = workItem.SourcePath,
                        DestinationPath = workItem.DestinationPath,
                        ActionType = "Error",
                        ErrorMessage = exception.Message,
                        FileSizeBytes = workItem.FileSizeBytes,
                        TransferTimeMilliseconds = loggedTransferTime,
                        EncryptionTimeMilliseconds = loggedEncryptionTime
                    });
                }
                finally
                {
                    _executionCoordinator.MarkWorkCompleted(workItem);
                    if (slotAcquired)
                    {
                        _executionCoordinator.ReleaseTransferSlot(workItem);
                    }

                    state.IsPriorityWorkPending = _executionCoordinator.HasPendingPriorityWork();
                    state.IsLargeFileTransfer = false;
                }
            }

            state.IsRunning = false;
            state.LastBackupUpdateTime = DateTime.Now;
            state.LastRunCompletedAt = state.LastBackupUpdateTime;
            state.Status = hasErrors ? BackupExecutionStatus.Error : BackupExecutionStatus.Finished;
            state.RequestedAction = BackupControlAction.Run;
            state.PauseReason = BackupPauseReason.None;
            state.PauseReasonDetails = string.Empty;
            _stateService.WriteState(state);

            if (backupJob.Type == BackupType.Full && !hasErrors)
            {
                _backupHistoryService.SetLastFullBackupUtc(backupJob.Name, DateTime.UtcNow);
            }

            globalStopwatch.Stop();
            result.Status = state.Status;
            result.TransferredFileCount = state.LastRunTransferredFiles.Count;
            result.TransferredBytes = state.TransferredBytes;
            result.ErrorMessage = state.ErrorMessage;
            result.ElapsedTime = globalStopwatch.Elapsed;
            return result;
        }
        finally
        {
            _executionController.CompleteJob(selectedBackupJob.JobNumber);
        }
    }

    private List<TransferWorkItem> BuildTransferWorkItems(
        SelectedBackupJob selectedBackupJob,
        IEnumerable<string> sourceFiles,
        DateTime? lastFullBackupUtc)
    {
        IEnumerable<string> filesToCopy = FilterFilesToCopy(sourceFiles, selectedBackupJob.Job, lastFullBackupUtc);

        return filesToCopy
            .Select(sourceFilePath =>
            {
                string relativePath = Path.GetRelativePath(selectedBackupJob.Job.Source, sourceFilePath);
                string destinationFilePath = Path.Combine(selectedBackupJob.Job.Target, relativePath);
                return new TransferWorkItem
                {
                    JobNumber = selectedBackupJob.JobNumber,
                    BackupName = selectedBackupJob.Job.Name,
                    SourcePath = sourceFilePath,
                    DestinationPath = destinationFilePath,
                    FileSizeBytes = new FileInfo(sourceFilePath).Length,
                    Priority = GetFilePriority(sourceFilePath)
                };
            })
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private BackupState CreateInitialState(BackupJob backupJob, long totalBytes, int fileCount)
    {
        return new BackupState
        {
            BackupName = backupJob.Name,
            CurrentSourcePath = string.Empty,
            CurrentTargetPath = string.Empty,
            IsRunning = true,
            Status = BackupExecutionStatus.Active,
            LastBackupUpdateTime = DateTime.Now,
            TotalEligibleFileCount = fileCount,
            RemainingFileCount = fileCount,
            TotalEligibleBytes = totalBytes,
            RemainingBytes = totalBytes,
            TransferredBytes = 0,
            ProcessedBytes = 0,
            LastRunStartedAt = DateTime.Now,
            LastRunCompletedAt = null,
            LastRunTransferredFiles = new List<BackupTransferredFile>(),
            IsPriorityWorkPending = false,
            CurrentFilePriority = FileTransferPriority.Normal,
            IsLargeFileTransfer = false,
            PauseReason = BackupPauseReason.None,
            RequestedAction = BackupControlAction.Run,
            PauseReasonDetails = string.Empty
        };
    }

    private void CopyFileWithRuntimeControl(
        SelectedBackupJob selectedBackupJob,
        TransferWorkItem workItem,
        BackupState state,
        ref long fileTransferredBytes)
    {
        using FileStream sourceStream = new FileStream(workItem.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream destinationStream = new FileStream(workItem.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[CopyBufferSize];

        while (true)
        {
            BackupControlAction controlAction = WaitUntilJobCanProceed(selectedBackupJob, state);
            if (controlAction == BackupControlAction.Stop)
            {
                throw new OperationCanceledException("Backup job was stopped.");
            }

            int bytesRead = sourceStream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                break;
            }

            destinationStream.Write(buffer, 0, bytesRead);
            destinationStream.Flush();

            fileTransferredBytes += bytesRead;
            state.TransferredBytes += bytesRead;
            state.ProcessedBytes += bytesRead;
            state.RemainingBytes = Math.Max(0, state.RemainingBytes - bytesRead);
            state.LastBackupUpdateTime = DateTime.Now;
            state.IsRunning = true;
            state.Status = BackupExecutionStatus.Active;
            state.RequestedAction = BackupControlAction.Run;
            state.PauseReason = BackupPauseReason.None;
            state.PauseReasonDetails = string.Empty;
            _stateService.WriteState(state);
        }
    }

    private BackupControlAction WaitUntilJobCanProceed(SelectedBackupJob selectedBackupJob, BackupState state)
    {
        while (true)
        {
            BackupExecutionCommandState commandState = _executionController.GetCommandState(selectedBackupJob.JobNumber);

            if (commandState.RequestedAction == BackupControlAction.Stop)
            {
                state.IsRunning = false;
                state.Status = BackupExecutionStatus.Stopping;
                state.RequestedAction = BackupControlAction.Stop;
                state.LastBackupUpdateTime = DateTime.Now;
                _stateService.WriteState(state);
                return BackupControlAction.Stop;
            }

            if (_businessSoftwareMonitor.TryGetRunningBlockedProcess(out string blockingProcessName))
            {
                _executionController.RequestAutomaticPause(
                    selectedBackupJob.JobNumber,
                    _textService.GetBackupBlockedByBusinessSoftwareMessage(blockingProcessName));
                commandState = _executionController.GetCommandState(selectedBackupJob.JobNumber);
            }

            if (commandState.RequestedAction == BackupControlAction.Pause)
            {
                state.IsRunning = false;
                state.Status = commandState.PauseReason == BackupPauseReason.BusinessSoftwareDetected
                    ? BackupExecutionStatus.PausedByBusinessSoftware
                    : BackupExecutionStatus.Paused;
                state.RequestedAction = BackupControlAction.Pause;
                state.PauseReason = commandState.PauseReason;
                state.PauseReasonDetails = commandState.PauseReasonDetails;
                state.LastBackupUpdateTime = DateTime.Now;
                _stateService.WriteState(state);

                if (commandState.PauseReason == BackupPauseReason.BusinessSoftwareDetected
                    && !_businessSoftwareMonitor.TryGetRunningBlockedProcess(out _))
                {
                    _executionController.RequestResume(selectedBackupJob.JobNumber);
                }

                Thread.Sleep(50);
                continue;
            }

            state.IsRunning = true;
            state.Status = BackupExecutionStatus.Active;
            state.RequestedAction = BackupControlAction.Run;
            state.PauseReason = BackupPauseReason.None;
            state.PauseReasonDetails = string.Empty;
            return BackupControlAction.Run;
        }
    }

    private IEnumerable<string> FilterFilesToCopy(
        IEnumerable<string> sourceFiles,
        BackupJob backupJob,
        DateTime? lastFullBackupUtc)
    {
        if (backupJob.Type == BackupType.Full || !lastFullBackupUtc.HasValue)
        {
            return sourceFiles;
        }

        return sourceFiles.Where(sourceFilePath =>
        {
            string relativePath = Path.GetRelativePath(backupJob.Source, sourceFilePath);
            string destinationFilePath = Path.Combine(backupJob.Target, relativePath);

            if (!File.Exists(destinationFilePath))
            {
                return true;
            }

            DateTime sourceWriteTime = File.GetLastWriteTimeUtc(sourceFilePath);
            return sourceWriteTime > lastFullBackupUtc.Value;
        });
    }

    private FileTransferPriority GetFilePriority(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return RuntimeStoragePaths.GetPriorityExtensions()
            .Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? FileTransferPriority.Priority
            : FileTransferPriority.Normal;
    }

    private BackupResult CompleteWithValidationError(BackupResult result, BackupJob backupJob, string errorMessage)
    {
        DateTime timestamp = DateTime.Now;
        var state = new BackupState
        {
            BackupName = backupJob.Name,
            CurrentSourcePath = backupJob.Source,
            CurrentTargetPath = backupJob.Target,
            IsRunning = false,
            Status = BackupExecutionStatus.Error,
            ErrorMessage = errorMessage,
            CurrentFileSize = 0,
            LastBackupUpdateTime = timestamp,
            TransferredBytes = 0,
            ProcessedBytes = 0,
            TotalEligibleFileCount = 0,
            RemainingFileCount = 0,
            TotalEligibleBytes = 0,
            RemainingBytes = 0,
            LastRunStartedAt = timestamp,
            LastRunCompletedAt = timestamp,
            LastRunTransferredFiles = new List<BackupTransferredFile>(),
            RequestedAction = BackupControlAction.None
        };

        _stateService.WriteState(state);
        _loggerService.WriteLog(new LogEntry
        {
            Timestamp = timestamp,
            BackupName = backupJob.Name,
            SourcePath = backupJob.Source,
            DestinationPath = backupJob.Target,
            ActionType = "Error",
            ErrorMessage = errorMessage,
            FileSizeBytes = 0,
            TransferTimeMilliseconds = -1
        });

        result.Status = BackupExecutionStatus.Error;
        result.ErrorMessage = errorMessage;
        return result;
    }

    private BackupResult CompleteWithStop(
        BackupResult result,
        BackupState state,
        TimeSpan elapsedTime,
        SelectedBackupJob selectedBackupJob,
        bool manualStop)
    {
        state.IsRunning = false;
        state.Status = BackupExecutionStatus.Stopped;
        state.RequestedAction = BackupControlAction.Stop;
        state.LastBackupUpdateTime = DateTime.Now;
        state.LastRunCompletedAt = state.LastBackupUpdateTime;
        state.ErrorMessage = manualStop
            ? $"Backup job {selectedBackupJob.JobNumber} was stopped."
            : state.PauseReasonDetails;
        _stateService.WriteState(state);

        _loggerService.WriteLog(new LogEntry
        {
            Timestamp = state.LastBackupUpdateTime,
            BackupName = selectedBackupJob.Job.Name,
            SourcePath = state.CurrentSourcePath,
            DestinationPath = state.CurrentTargetPath,
            ActionType = "Stopped",
            ErrorMessage = state.ErrorMessage,
            FileSizeBytes = state.CurrentFileSize,
            TransferTimeMilliseconds = 0
        });

        result.Status = BackupExecutionStatus.Stopped;
        result.TransferredFileCount = state.LastRunTransferredFiles.Count;
        result.TransferredBytes = state.TransferredBytes;
        result.ErrorMessage = state.ErrorMessage;
        result.ElapsedTime = elapsedTime;
        return result;
    }

    private void EnsureDestinationDirectoryExists(
        string backupName,
        string sourceFilePath,
        string? destinationDirectory,
        ISet<string> createdDirectories)
    {
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            return;
        }

        if (Directory.Exists(destinationDirectory))
        {
            return;
        }

        Directory.CreateDirectory(destinationDirectory);

        if (!createdDirectories.Add(destinationDirectory))
        {
            return;
        }

        _loggerService.WriteLog(new LogEntry
        {
            Timestamp = DateTime.Now,
            BackupName = backupName,
            SourcePath = sourceFilePath,
            DestinationPath = destinationDirectory,
            ActionType = "CreateDirectory",
            FileSizeBytes = 0,
            TransferTimeMilliseconds = 0
        });
    }

    private static void RollBackCurrentFileProgress(BackupState state, long fileTransferredBytes)
    {
        if (fileTransferredBytes <= 0)
        {
            return;
        }

        state.TransferredBytes = Math.Max(0, state.TransferredBytes - fileTransferredBytes);
        state.ProcessedBytes = Math.Max(0, state.ProcessedBytes - fileTransferredBytes);
        state.RemainingBytes += fileTransferredBytes;
    }

    private static void DeletePartialFile(string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }
        catch
        {
            // Best effort cleanup for interrupted transfers.
        }
    }
}
