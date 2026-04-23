public class ConsoleBackupProgressReporter : IBackupProgressReporter
{
    private readonly ApplicationTextService _textService;

    public ConsoleBackupProgressReporter(ApplicationTextService textService)
    {
        _textService = textService;
    }

    public void ReportJobStarted(SelectedBackupJob selectedBackupJob, BackupState state)
    {
        Console.WriteLine(_textService.GetJobStartedTitle(selectedBackupJob));
        Console.WriteLine(_textService.GetJobTypeLine(selectedBackupJob.Job.Type));
        Console.WriteLine(_textService.GetJobSourceLine(selectedBackupJob.Job.Source));
        Console.WriteLine(_textService.GetJobTargetLine(selectedBackupJob.Job.Target));
        Console.WriteLine(_textService.GetEligibleFilesLine(state.TotalEligibleFileCount));
        Console.WriteLine(_textService.GetTotalBytesLine(state.TotalEligibleBytes));
        Console.WriteLine();
    }

    public void ReportFileCopied(SelectedBackupJob selectedBackupJob, BackupState state, BackupTransferredFile transferredFile)
    {
        long transferredFileCount = state.TotalEligibleFileCount - state.RemainingFileCount;

        Console.WriteLine(_textService.GetJobProgressTitle(selectedBackupJob));
        Console.WriteLine(_textService.GetCurrentFileLine(transferredFile.SourcePath));
        Console.WriteLine(_textService.GetCurrentDestinationLine(transferredFile.DestinationPath));
        Console.WriteLine(_textService.GetTransferredFilesLine(transferredFileCount, state.TotalEligibleFileCount));
        Console.WriteLine(_textService.GetTransferredBytesLine(state.TransferredBytes, state.TotalEligibleBytes));
        Console.WriteLine(_textService.GetProgressLine(state.Progress));
        Console.WriteLine();
    }

    public void ReportJobCompleted(SelectedBackupJob selectedBackupJob, BackupState state, TimeSpan elapsedTime)
    {
        long transferredFileCount = state.TotalEligibleFileCount - state.RemainingFileCount;

        Console.WriteLine(_textService.GetJobCompletedTitle(selectedBackupJob));
        Console.WriteLine(_textService.GetTransferredFilesLine(transferredFileCount, state.TotalEligibleFileCount));
        Console.WriteLine(_textService.GetTransferredBytesLine(state.TransferredBytes, state.TotalEligibleBytes));
        Console.WriteLine(_textService.GetElapsedTimeLine(elapsedTime));
        Console.WriteLine(_textService.GetCompletionStatusLine());
        Console.WriteLine();
    }

    public void ReportFileSkipped(SelectedBackupJob selectedBackupJob, string filePath, string reason)
    {
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(_textService.GetSkippedFileMessage(selectedBackupJob, filePath, reason));
        Console.ForegroundColor = previousColor;
        Console.WriteLine();
    }

    public void ReportSourceDirectoryMissing(SelectedBackupJob selectedBackupJob)
    {
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(_textService.GetSourceDirectoryMissingMessage(selectedBackupJob));
        Console.ForegroundColor = previousColor;
        Console.WriteLine();
    }
}
