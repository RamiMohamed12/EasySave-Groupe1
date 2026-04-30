using System.Globalization;
using System.Resources;

public class ApplicationTextService
{
    public const string EnglishLanguageCode = "en";
    public const string FrenchLanguageCode = "fr";

    private const string ResourceBaseName = "EasySave.Core.Resources.EasySaveStrings";
    private static readonly ResourceManager Resources = new(ResourceBaseName, typeof(ApplicationTextService).Assembly);

    private readonly CultureInfo _culture;

    private ApplicationTextService(string languageCode)
    {
        _culture = CultureInfo.GetCultureInfo(languageCode);
    }

    public static ApplicationTextService Create()
    {
        string? languageOverride = Environment.GetEnvironmentVariable("EASYSAVE_LANGUAGE");
        string? configuredLanguage = RuntimeStoragePaths.GetLanguageCode();

        return Create(!string.IsNullOrWhiteSpace(languageOverride) ? languageOverride : configuredLanguage);
    }

    public static ApplicationTextService Create(string? languageCode)
    {
        return new ApplicationTextService(ResolveLanguageCode(languageCode));
    }

    public static string ResolveLanguageCode(string? languageCode)
    {
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            return languageCode.StartsWith(FrenchLanguageCode, StringComparison.OrdinalIgnoreCase)
                ? FrenchLanguageCode
                : EnglishLanguageCode;
        }

        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals(FrenchLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? FrenchLanguageCode
            : EnglishLanguageCode;
    }

    public string GetText(string key)
    {
        return Resources.GetString(key, _culture) ?? $"[[{key}]]";
    }

    public string FormatText(string key, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, GetText(key), args);
    }

    public string GetUsageMessage() => GetText("UsageMessage");

    public string GetUsageExamples() => GetText("UsageExamples");

    public string GetNoConfiguredJobsMessage() => GetText("NoConfiguredJobsMessage");

    public string GetJobNotConfiguredMessage(int jobNumber) => FormatText("JobNotConfiguredMessage", jobNumber);

    public string GetSourceDirectoryMissingMessage() => GetText("SourceDirectoryMissingMessage");

    public string GetSourceDirectoryMissingMessage(SelectedBackupJob selectedBackupJob)
    {
        return FormatText(
            "SourceDirectoryMissingForJobMessage",
            selectedBackupJob.JobNumber,
            selectedBackupJob.Job.Name,
            selectedBackupJob.Job.Source);
    }

    public string GetSourcePathRequiredMessage() => GetText("SourcePathRequiredMessage");

    public string GetTargetPathRequiredMessage() => GetText("TargetPathRequiredMessage");

    public string GetSelectionRequiredMessage() => GetText("SelectionRequiredMessage");

    public string GetInvalidRangeFormatMessage() => GetText("InvalidRangeFormatMessage");

    public string GetInvalidRangeOrderMessage() => GetText("InvalidRangeOrderMessage");

    public string GetInvalidJobNumberMessage() => GetText("InvalidJobNumberMessage");

    public string GetSingleArgumentExpectedMessage() => GetText("SingleArgumentExpectedMessage");

    public string GetInvalidCommandMessage() => GetText("InvalidCommandMessage");

    public string GetInvalidConfigureCommandMessage() => GetText("InvalidConfigureCommandMessage");

    public string GetInvalidConfigureFieldMessage() => GetText("InvalidConfigureFieldMessage");

    public string GetPathValueRequiredMessage() => GetText("PathValueRequiredMessage");

    public string GetInvalidStorageDirectoryCommandMessage() => GetText("InvalidStorageDirectoryCommandMessage");

    public string GetInvalidLanguageCommandMessage() => GetText("InvalidLanguageCommandMessage");

    public string GetInvalidLanguageCodeMessage() => GetText("InvalidLanguageCodeMessage");

    public IReadOnlyList<string> GetHelpLines()
    {
        return
        [
            GetUsageMessage(),
            string.Empty,
            GetText("HelpNoArgumentLine"),
            GetText("HelpDisplayLine"),
            GetText("HelpJobNumbersLine"),
            GetText("HelpSelectionLine"),
            GetText("HelpConfigureLine"),
            GetText("HelpStorageLine"),
            GetText("HelpLanguageLine"),
            GetUsageExamples(),
            GetText("HelpInvalidExampleLine")
        ];
    }

    public string GetConfiguredJobsHeader() => GetText("ConfiguredJobsHeader");

    public string GetJobSummaryLine(int jobNumber, BackupJob job) => FormatText("JobSummaryLine", jobNumber, job.Name);

    public string GetJobSourceLine(string sourcePath) => FormatText("JobSourceLine", FormatConfiguredPath(sourcePath));

    public string GetJobTargetLine(string targetPath) => FormatText("JobTargetLine", FormatConfiguredPath(targetPath));

    public string GetJobTypeLine(BackupType backupType) => FormatText("JobTypeLine", GetBackupTypeDisplayName(backupType));

    public string GetJobConfigurationStatusLine(BackupJob job)
    {
        string status = IsConfigured(job) ? GetText("ConfiguredLabel") : GetText("IncompleteLabel");
        return FormatText("JobConfigurationStatusLine", status);
    }

    public string GetJobPathUpdatedMessage(int jobNumber, BackupJob job, JobPathField pathField)
    {
        string fieldName = GetPathFieldDisplayName(pathField);
        string pathValue = pathField == JobPathField.Source ? job.Source : job.Target;
        return FormatText("JobPathUpdatedMessage", jobNumber, fieldName, pathValue);
    }

    public string GetStorageDirectoryUpdatedMessage(string path) => FormatText("StorageDirectoryUpdatedMessage", path);

    public string GetLanguageUpdatedMessage() => GetText("LanguageUpdatedMessage");

    public string FormatBackupResult(BackupResult result)
    {
        if (result.Status == BackupExecutionStatus.Finished)
        {
            return FormatText(
                "BackupResultFinished",
                result.TransferredFileCount,
                FormatBytes(result.TransferredBytes),
                FormatDuration(result.ElapsedTime));
        }

        return FormatText("BackupResultFailed", result.JobNumber, result.BackupName, result.ErrorMessage);
    }

    public string GetBackupSuccessMessage() => GetText("BackupSuccessMessage");

    public string GetBackupTypeDisplayName(BackupType backupType)
    {
        return backupType switch
        {
            BackupType.Full => GetText("BackupTypeFull"),
            BackupType.Differential => GetText("BackupTypeDifferential"),
            _ => backupType.ToString()
        };
    }

    public string GetJobStartedTitle(SelectedBackupJob selectedBackupJob)
    {
        return FormatText("JobStartedTitle", selectedBackupJob.JobNumber, selectedBackupJob.Job.Name);
    }

    public string GetJobCompletedTitle(SelectedBackupJob selectedBackupJob)
    {
        return FormatText("JobCompletedTitle", selectedBackupJob.JobNumber, selectedBackupJob.Job.Name);
    }

    public string GetJobProgressTitle(SelectedBackupJob selectedBackupJob)
    {
        return FormatText("JobProgressTitle", selectedBackupJob.JobNumber, selectedBackupJob.Job.Name);
    }

    public string GetEligibleFilesLine(long totalEligibleFileCount) => FormatText("EligibleFilesLine", totalEligibleFileCount);

    public string GetTotalBytesLine(long totalBytes) => FormatText("TotalBytesLine", FormatBytes(totalBytes));

    public string GetTransferredFilesLine(long transferredFileCount, long totalEligibleFileCount)
    {
        return FormatText("TransferredFilesLine", transferredFileCount, totalEligibleFileCount);
    }

    public string GetTransferredBytesLine(long transferredBytes, long totalEligibleBytes)
    {
        return FormatText("TransferredBytesLine", FormatBytes(transferredBytes), FormatBytes(totalEligibleBytes));
    }

    public string GetProgressLine(double progress) => FormatText("ProgressLine", progress);

    public string GetCurrentFileLine(string sourcePath) => FormatText("CurrentFileLine", sourcePath);

    public string GetCurrentDestinationLine(string destinationPath) => FormatText("CurrentDestinationLine", destinationPath);

    public string GetCompletionStatusLine() => GetText("CompletionStatusLine");

    public string GetSkippedFileMessage(SelectedBackupJob selectedBackupJob, string filePath, string reason)
    {
        return FormatText(
            "SkippedFileMessage",
            selectedBackupJob.JobNumber,
            selectedBackupJob.Job.Name,
            filePath,
            reason);
    }

    public string GetEmptyFileSkipReason() => GetText("EmptyFileSkipReason");

    public string GetSuspiciousExtensionSkipReason(string extension) => FormatText("SuspiciousExtensionSkipReason", extension);

    public string GetElapsedTimeLine(TimeSpan elapsedTime) => FormatText("ElapsedTimeLine", FormatDuration(elapsedTime));

    public string GetLanguageCode() => _culture.TwoLetterISOLanguageName;

    public string GetPathFieldDisplayName(JobPathField pathField)
    {
        return pathField == JobPathField.Source ? GetText("SourceFieldName") : GetText("TargetFieldName");
    }

    public string GetLanguageDisplayName(string languageCode)
    {
        return ResolveLanguageCode(languageCode) == FrenchLanguageCode
            ? GetText("FrenchLanguageDisplayName")
            : GetText("EnglishLanguageDisplayName");
    }

    public string GetLogFileFormatDisplayName(string logFileFormat)
    {
        return logFileFormat == RuntimeStoragePaths.XmlLogFileFormat ? GetText("XmlLogFormat") : GetText("JsonLogFormat");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int suffixIndex = 0;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return $"{value:0.##} {suffixes[suffixIndex]}";
    }

    private static string FormatDuration(TimeSpan elapsedTime)
    {
        if (elapsedTime.TotalHours >= 1)
        {
            return $"{(int)elapsedTime.TotalHours}h {elapsedTime.Minutes}m {elapsedTime.Seconds}s";
        }

        if (elapsedTime.TotalMinutes >= 1)
        {
            return $"{(int)elapsedTime.TotalMinutes}m {elapsedTime.Seconds}s";
        }

        if (elapsedTime.TotalSeconds >= 1)
        {
            return $"{elapsedTime.TotalSeconds:0.##}s";
        }

        return $"{elapsedTime.TotalMilliseconds:0}ms";
    }

    private string FormatConfiguredPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? GetText("NotConfiguredLabel") : path;
    }

    private static bool IsConfigured(BackupJob job)
    {
        return !string.IsNullOrWhiteSpace(job.Source)
            && !string.IsNullOrWhiteSpace(job.Target);
    }
}
