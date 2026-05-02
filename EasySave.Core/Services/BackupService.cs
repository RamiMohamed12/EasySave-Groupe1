using System.Diagnostics;

public class BackupService : IBackupService
{
    private readonly BackupHistoryService _backupHistoryService;
    private readonly LoggerService _loggerService;
    private readonly StateService _stateService;
    private readonly ApplicationTextService _textService;
    private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService,
        IBusinessSoftwareMonitor businessSoftwareMonitor)
    {
        _loggerService = loggerService;
        _stateService = stateService;
        _backupHistoryService = backupHistoryService;
        _textService = textService;
        _businessSoftwareMonitor = businessSoftwareMonitor;
    }

    public BackupResult StartBackup(SelectedBackupJob selectedBackupJob)
    {
        var globalStopwatch = Stopwatch.StartNew();
        BackupJob backupJob = selectedBackupJob.Job;
        var result = new BackupResult
        {
            JobNumber = selectedBackupJob.JobNumber,
            BackupName = backupJob.Name
        };

        if (_businessSoftwareMonitor.TryGetRunningBlockedProcess(out string startupBlockedProcess))
        {
            return CompleteWithBusinessSoftwareStop(result, backupJob, startupBlockedProcess);
        }

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
        var filesToCopy = FilterFilesToCopy(sourceFiles, backupJob, lastFullBackupUtc);

        long totalBytes = filesToCopy.Sum(filePath => new FileInfo(filePath).Length);
        var state = new BackupState
        {
            BackupName = backupJob.Name,
            CurrentSourcePath = string.Empty,
            CurrentTargetPath = string.Empty,
            IsRunning = true,
            Status = BackupExecutionStatus.Active,
            LastBackupUpdateTime = DateTime.Now,
            TotalEligibleFileCount = filesToCopy.Count,
            RemainingFileCount = filesToCopy.Count,
            TotalEligibleBytes = totalBytes,
            RemainingBytes = totalBytes,
            TransferredBytes = 0,
            ProcessedBytes = 0,
            LastRunStartedAt = DateTime.Now,
            LastRunCompletedAt = null,
            LastRunTransferredFiles = new List<BackupTransferredFile>()
        };

        _stateService.WriteState(state);
        var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool hasErrors = false;

        foreach (string sourceFilePath in filesToCopy)
        {
            string relativePath = Path.GetRelativePath(backupJob.Source, sourceFilePath);
            string destinationFilePath = Path.Combine(backupJob.Target, relativePath);
            string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            long currentFileSize = new FileInfo(sourceFilePath).Length;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                EnsureDestinationDirectoryExists(backupJob.Name, sourceFilePath, destinationDirectory, createdDirectories);
                File.Copy(sourceFilePath, destinationFilePath, true);
                stopwatch.Stop();

                DateTime transferTimestamp = DateTime.Now;
                state.CurrentSourcePath = sourceFilePath;
                state.CurrentTargetPath = destinationFilePath;
                state.CurrentFileSize = currentFileSize;
                state.LastBackupUpdateTime = transferTimestamp;
                state.TransferredBytes += currentFileSize;
                state.ProcessedBytes += currentFileSize;
                state.RemainingBytes -= currentFileSize;
                state.RemainingFileCount -= 1;
                state.ErrorMessage = string.Empty;
                var transferredFile = new BackupTransferredFile
                {
                    Timestamp = transferTimestamp,
                    SourcePath = sourceFilePath,
                    DestinationPath = destinationFilePath,
                    FileSizeBytes = currentFileSize,
                    TransferTimeMilliseconds = stopwatch.ElapsedMilliseconds
                };
                state.LastRunTransferredFiles.Add(transferredFile);

                _stateService.WriteState(state);
                _loggerService.WriteLog(new LogEntry
                {
                    Timestamp = transferTimestamp,
                    BackupName = backupJob.Name,
                    SourcePath = sourceFilePath,
                    DestinationPath = destinationFilePath,
                    ActionType = "FileTransfer",
                    FileSizeBytes = currentFileSize,
                    TransferTimeMilliseconds = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                hasErrors = true;
                DateTime transferTimestamp = DateTime.Now;
                long negativeTransferTime = -Math.Max(1, stopwatch.ElapsedMilliseconds);

                state.CurrentSourcePath = sourceFilePath;
                state.CurrentTargetPath = destinationFilePath;
                state.CurrentFileSize = currentFileSize;
                state.LastBackupUpdateTime = transferTimestamp;
                state.ProcessedBytes += currentFileSize;
                state.RemainingBytes -= currentFileSize;
                state.RemainingFileCount -= 1;
                state.Status = BackupExecutionStatus.Error;
                state.ErrorMessage = exception.Message;

                _stateService.WriteState(state);
                _loggerService.WriteLog(new LogEntry
                {
                    Timestamp = transferTimestamp,
                    BackupName = backupJob.Name,
                    SourcePath = sourceFilePath,
                    DestinationPath = destinationFilePath,
                    ActionType = "Error",
                    ErrorMessage = exception.Message,
                    FileSizeBytes = currentFileSize,
                    TransferTimeMilliseconds = negativeTransferTime
                });
            }

            if (_businessSoftwareMonitor.TryGetRunningBlockedProcess(out string runningBlockedProcess))
            {
                state.IsRunning = false;
                state.LastBackupUpdateTime = DateTime.Now;
                state.LastRunCompletedAt = state.LastBackupUpdateTime;
                state.Status = BackupExecutionStatus.Stopped;
                state.ErrorMessage = _textService.GetBackupBlockedByBusinessSoftwareMessage(runningBlockedProcess);
                _stateService.WriteState(state);

                _loggerService.WriteLog(new LogEntry
                {
                    Timestamp = state.LastBackupUpdateTime,
                    BackupName = backupJob.Name,
                    SourcePath = state.CurrentSourcePath,
                    DestinationPath = state.CurrentTargetPath,
                    ActionType = "BusinessSoftwareDetected",
                    ErrorMessage = state.ErrorMessage,
                    FileSizeBytes = 0,
                    TransferTimeMilliseconds = 0
                });

                globalStopwatch.Stop();
                result.Status = BackupExecutionStatus.Stopped;
                result.TransferredFileCount = state.LastRunTransferredFiles.Count;
                result.TransferredBytes = state.TransferredBytes;
                result.ErrorMessage = state.ErrorMessage;
                result.ElapsedTime = globalStopwatch.Elapsed;
                result.StoppedByBusinessSoftware = true;
                result.BlockingProcessName = runningBlockedProcess;
                return result;
            }
        }

        state.IsRunning = false;
        state.LastBackupUpdateTime = DateTime.Now;
        state.LastRunCompletedAt = state.LastBackupUpdateTime;
        state.Status = hasErrors ? BackupExecutionStatus.Error : BackupExecutionStatus.Finished;
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

    private List<string> FilterFilesToCopy(
        IEnumerable<string> sourceFiles,
        BackupJob backupJob,
        DateTime? lastFullBackupUtc)
    {
        if (backupJob.Type == BackupType.Full)
        {
            return sourceFiles.ToList();
        }

        if (!lastFullBackupUtc.HasValue)
        {
            return sourceFiles.ToList();
        }

        var filesToCopy = new List<string>();

        foreach (string sourceFilePath in sourceFiles)
        {
            string relativePath = Path.GetRelativePath(backupJob.Source, sourceFilePath);
            string destinationFilePath = Path.Combine(backupJob.Target, relativePath);

            if (!File.Exists(destinationFilePath))
            {
                filesToCopy.Add(sourceFilePath);
                continue;
            }

            DateTime sourceWriteTime = File.GetLastWriteTimeUtc(sourceFilePath);

            if (sourceWriteTime > lastFullBackupUtc.Value)
            {
                filesToCopy.Add(sourceFilePath);
            }
        }

        return filesToCopy;
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
            LastRunTransferredFiles = new List<BackupTransferredFile>()
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

    private BackupResult CompleteWithBusinessSoftwareStop(BackupResult result, BackupJob backupJob, string processName)
    {
        DateTime timestamp = DateTime.Now;
        string errorMessage = _textService.GetBackupBlockedByBusinessSoftwareMessage(processName);
        var state = new BackupState
        {
            BackupName = backupJob.Name,
            CurrentSourcePath = backupJob.Source,
            CurrentTargetPath = backupJob.Target,
            IsRunning = false,
            Status = BackupExecutionStatus.Stopped,
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
            LastRunTransferredFiles = new List<BackupTransferredFile>()
        };

        _stateService.WriteState(state);
        _loggerService.WriteLog(new LogEntry
        {
            Timestamp = timestamp,
            BackupName = backupJob.Name,
            SourcePath = backupJob.Source,
            DestinationPath = backupJob.Target,
            ActionType = "BusinessSoftwareDetected",
            ErrorMessage = errorMessage,
            FileSizeBytes = 0,
            TransferTimeMilliseconds = 0
        });

        result.Status = BackupExecutionStatus.Stopped;
        result.ErrorMessage = errorMessage;
        result.StoppedByBusinessSoftware = true;
        result.BlockingProcessName = processName;
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
}
