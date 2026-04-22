using System.Diagnostics;

public class BackupService : IBackupService
{
    private readonly BackupHistoryService _backupHistoryService;
    private readonly LoggerService _loggerService;
    private readonly StateService _stateService;
    private readonly ApplicationTextService _textService;

    public BackupService(
        LoggerService loggerService,
        StateService stateService,
        BackupHistoryService backupHistoryService,
        ApplicationTextService textService)
    {
        _loggerService = loggerService;
        _stateService = stateService;
        _backupHistoryService = backupHistoryService;
        _textService = textService;
    }

    public void StartBackup(BackupJob backupJob)
    {
        if (!Directory.Exists(backupJob.Source))
        {
            Console.WriteLine(_textService.GetSourceDirectoryMissingMessage());
            return;
        }

        Directory.CreateDirectory(backupJob.Target);

        string[] sourceFiles = Directory.GetFiles(backupJob.Source, "*", SearchOption.AllDirectories);
        DateTime? lastFullBackupUtc = backupJob.Type == BackupType.Differential
            ? _backupHistoryService.GetLastFullBackupUtc(backupJob.Name)
            : null;
        var filesToCopy = FilterFilesToCopy(sourceFiles, backupJob, lastFullBackupUtc);

        long totalBytes = filesToCopy.Sum(filePath => new FileInfo(filePath).Length);
        var state = new BackupState
        {
            BackupName = backupJob.Name,
            IsRunning = true,
            LastBackupUpdateTime = DateTime.Now,
            TotalEligibleFileCount = filesToCopy.Count,
            RemainingFileCount = filesToCopy.Count,
            TotalEligibleBytes = totalBytes,
            RemainingBytes = totalBytes,
            TransferredBytes = 0
        };

        _stateService.WriteState(state);

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

            state.CurrentSourcePath = sourceFilePath;
            state.CurrentTargetPath = destinationFilePath;
            state.CurrentFileSize = currentFileSize;
            state.LastBackupUpdateTime = DateTime.Now;
            state.TransferredBytes += currentFileSize;
            state.RemainingBytes -= currentFileSize;
            state.RemainingFileCount -= 1;

            _stateService.WriteState(state);
            _loggerService.WriteLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupJob.Name,
                SourcePath = sourceFilePath,
                DestinationPath = destinationFilePath,
                FileSizeBytes = currentFileSize,
                TransferTimeMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }

        state.IsRunning = false;
        state.LastBackupUpdateTime = DateTime.Now;
        _stateService.WriteState(state);

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
}
