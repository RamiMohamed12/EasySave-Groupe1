using System.Diagnostics;
using System.Text.RegularExpressions;

public class BackupService : IBackupService
{
    private readonly BackupHistoryService _backupHistoryService;
    private readonly LoggerService _loggerService;
    private readonly StateService _stateService;
    private readonly IBackupProgressReporter _progressReporter;
    private readonly ApplicationTextService _textService;

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        IBackupProgressReporter progressReporter,
        ApplicationTextService textService)
    {
        _loggerService = loggerService;
        _stateService = stateService;
        _backupHistoryService = backupHistoryService;
        _progressReporter = progressReporter;
        _textService = textService;
    }

    public void StartBackup(SelectedBackupJob selectedBackupJob)
    {
        BackupJob backupJob = selectedBackupJob.Job;
        var jobStopwatch = Stopwatch.StartNew();

        if (!Directory.Exists(backupJob.Source))
        {
            var missingSourceState = new BackupState
            {
                BackupName = backupJob.Name,
                CurrentSourcePath = backupJob.Source,
                CurrentTargetPath = backupJob.Target,
                IsRunning = false,
                CurrentFileSize = 0,
                LastBackupUpdateTime = DateTime.Now,
                TransferredBytes = 0,
                TotalEligibleFileCount = 0,
                RemainingFileCount = 0,
                TotalEligibleBytes = 0,
                RemainingBytes = 0,
                LastRunStartedAt = DateTime.Now,
                LastRunCompletedAt = DateTime.Now,
                LastRunTransferredFiles = new List<BackupTransferredFile>()
            };

            _stateService.WriteState(missingSourceState);
            _progressReporter.ReportSourceDirectoryMissing(selectedBackupJob);
            return;
        }

        Directory.CreateDirectory(backupJob.Target);

        string[] sourceFiles = Directory.GetFiles(backupJob.Source, "*", SearchOption.AllDirectories);
        DateTime? lastFullBackupUtc = backupJob.Type == BackupType.Differential
            ? _backupHistoryService.GetLastFullBackupUtc(backupJob.Name)
            : null;
        var candidateFiles = FilterFilesToCopy(sourceFiles, backupJob, lastFullBackupUtc);
        var filesToCopy = FilterUnsupportedFiles(candidateFiles, selectedBackupJob);

        long totalBytes = filesToCopy.Sum(filePath => new FileInfo(filePath).Length);
        var state = new BackupState
        {
            BackupName = backupJob.Name,
            CurrentSourcePath = string.Empty,
            CurrentTargetPath = string.Empty,
            IsRunning = true,
            LastBackupUpdateTime = DateTime.Now,
            TotalEligibleFileCount = filesToCopy.Count,
            RemainingFileCount = filesToCopy.Count,
            TotalEligibleBytes = totalBytes,
            RemainingBytes = totalBytes,
            TransferredBytes = 0,
            LastRunStartedAt = DateTime.Now,
            LastRunCompletedAt = null,
            LastRunTransferredFiles = new List<BackupTransferredFile>()
        };

        _stateService.WriteState(state);
        _progressReporter.ReportJobStarted(selectedBackupJob, state);

        foreach (string sourceFilePath in filesToCopy)
        {
            string relativePath = Path.GetRelativePath(backupJob.Source, sourceFilePath);
            string destinationFilePath = Path.Combine(backupJob.Target, relativePath);
            string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);

            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            long currentFileSize = new FileInfo(sourceFilePath).Length;
            var stopwatch = Stopwatch.StartNew();

            File.Copy(sourceFilePath, destinationFilePath, true);

            stopwatch.Stop();

            DateTime transferTimestamp = DateTime.Now;
            state.CurrentSourcePath = sourceFilePath;
            state.CurrentTargetPath = destinationFilePath;
            state.CurrentFileSize = currentFileSize;
            state.LastBackupUpdateTime = transferTimestamp;
            state.TransferredBytes += currentFileSize;
            state.RemainingBytes -= currentFileSize;
            state.RemainingFileCount -= 1;
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
                FileSizeBytes = currentFileSize,
                TransferTimeMilliseconds = stopwatch.ElapsedMilliseconds
            });
            _progressReporter.ReportFileCopied(selectedBackupJob, state, transferredFile);
        }

        state.IsRunning = false;
        state.LastBackupUpdateTime = DateTime.Now;
        state.LastRunCompletedAt = state.LastBackupUpdateTime;
        _stateService.WriteState(state);
        jobStopwatch.Stop();
        _progressReporter.ReportJobCompleted(selectedBackupJob, state, jobStopwatch.Elapsed);

        if (backupJob.Type == BackupType.Full)
        {
            _backupHistoryService.SetLastFullBackupUtc(backupJob.Name, DateTime.UtcNow);
        }
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

    private List<string> FilterUnsupportedFiles(IEnumerable<string> sourceFiles, SelectedBackupJob selectedBackupJob)
    {
        var filesToCopy = new List<string>();

        foreach (string sourceFilePath in sourceFiles)
        {
            if (TryGetSkipReason(sourceFilePath, out string skipReason))
            {
                _progressReporter.ReportFileSkipped(selectedBackupJob, sourceFilePath, skipReason);
                continue;
            }

            filesToCopy.Add(sourceFilePath);
        }

        return filesToCopy;
    }

    private bool TryGetSkipReason(string sourceFilePath, out string skipReason)
    {
        long fileSize = new FileInfo(sourceFilePath).Length;

        if (fileSize == 0)
        {
            skipReason = _textService.GetEmptyFileSkipReason();
            return true;
        }

        string extension = Path.GetExtension(sourceFilePath);

        if (IsSuspiciousExtension(extension))
        {
            skipReason = _textService.GetSuspiciousExtensionSkipReason(
                string.IsNullOrWhiteSpace(extension) ? "<none>" : extension);
            return true;
        }

        skipReason = string.Empty;
        return false;
    }

    private static bool IsSuspiciousExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return true;
        }

        string extensionBody = extension.TrimStart('.');

        if (string.IsNullOrWhiteSpace(extensionBody))
        {
            return true;
        }

        if (extensionBody.Length > 10)
        {
            return true;
        }

        return !Regex.IsMatch(extensionBody, "^[a-zA-Z0-9]+$");
    }
}
