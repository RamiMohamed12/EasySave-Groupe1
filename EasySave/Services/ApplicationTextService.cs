using System.Globalization;

public class ApplicationTextService
{
    private readonly bool _useFrench;

    private ApplicationTextService(bool useFrench)
    {
        _useFrench = useFrench;
    }

    public static ApplicationTextService Create()
    {
        string? languageOverride = Environment.GetEnvironmentVariable("EASYSAVE_LANGUAGE");

        if (!string.IsNullOrWhiteSpace(languageOverride))
        {
            return new ApplicationTextService(languageOverride.StartsWith("fr", StringComparison.OrdinalIgnoreCase));
        }

        return new ApplicationTextService(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase));
    }

    public string GetUsageMessage()
    {
        return _useFrench
            ? "Utilisation : EasySave | EasySave --help | EasySave <selection-des-taches> | EasySave --configure <job> source|target <chemin> | EasySave --storage-dir <chemin>"
            : "Usage: EasySave | EasySave --help | EasySave <job-selection> | EasySave --configure <job> source|target <path> | EasySave --storage-dir <path>";
    }

    public string GetUsageExamples()
    {
        return _useFrench
            ? "Exemples : EasySave 1-3 | EasySave 1;3 | EasySave 2"
            : "Examples: EasySave 1-3 | EasySave 1;3 | EasySave 2";
    }

    public string GetNoConfiguredJobsMessage()
    {
        return _useFrench
            ? "Aucune tache de sauvegarde n'est configuree."
            : "No backup jobs are configured.";
    }

    public string GetJobNotConfiguredMessage(int jobNumber)
    {
        return _useFrench
            ? $"La tache {jobNumber} n'est pas configuree dans jobs.json."
            : $"Job {jobNumber} is not configured in jobs.json.";
    }

    public string GetSourceDirectoryMissingMessage()
    {
        return _useFrench
            ? "Le dossier source n'existe pas."
            : "Source directory does not exist.";
    }

    public string GetSourceDirectoryMissingMessage(SelectedBackupJob selectedBackupJob)
    {
        return _useFrench
            ? $"Echec de la tache {selectedBackupJob.JobNumber} ({selectedBackupJob.Job.Name}) : le dossier source n'existe pas : {selectedBackupJob.Job.Source}"
            : $"Job {selectedBackupJob.JobNumber} ({selectedBackupJob.Job.Name}) failed: source directory does not exist: {selectedBackupJob.Job.Source}";
    }

    public string GetSourcePathRequiredMessage()
    {
        return _useFrench
            ? "Le chemin source doit etre configure avant l'execution."
            : "Source path must be configured before execution.";
    }

    public string GetTargetPathRequiredMessage()
    {
        return _useFrench
            ? "Le chemin cible doit etre configure avant l'execution."
            : "Target path must be configured before execution.";
    }

    public string GetSelectionRequiredMessage()
    {
        return _useFrench
            ? "Une selection de tache est requise."
            : "A job selection is required.";
    }

    public string GetInvalidRangeFormatMessage()
    {
        return _useFrench
            ? "Format de plage invalide. Utilisez des valeurs comme 1-3."
            : "Invalid range format. Use values like 1-3.";
    }

    public string GetInvalidRangeOrderMessage()
    {
        return _useFrench
            ? "Le debut de la plage doit etre inferieur ou egal a la fin."
            : "Range start must be less than or equal to range end.";
    }

    public string GetInvalidJobNumberMessage()
    {
        return _useFrench
            ? "Les numeros de tache doivent etre compris entre 1 et 5."
            : "Job numbers must be between 1 and 5.";
    }

    public string GetSingleArgumentExpectedMessage()
    {
        return _useFrench
            ? "Un seul argument est attendu. Utilisez --help pour afficher l'aide."
            : "Expected a single argument. Use --help to display help.";
    }

    public string GetInvalidCommandMessage()
    {
        return _useFrench
            ? "Commande invalide. Utilisez --help pour afficher l'aide."
            : "Invalid command. Use --help to display help.";
    }

    public string GetInvalidConfigureCommandMessage()
    {
        return _useFrench
            ? "Commande de configuration invalide. Utilisez : EasySave --configure <job> source|target <chemin>."
            : "Invalid configure command. Use: EasySave --configure <job> source|target <path>.";
    }

    public string GetInvalidConfigureFieldMessage()
    {
        return _useFrench
            ? "Le champ de configuration doit etre source ou target."
            : "Configuration field must be source or target.";
    }

    public string GetPathValueRequiredMessage()
    {
        return _useFrench
            ? "Un chemin non vide est requis."
            : "A non-empty path is required.";
    }

    public string GetInvalidStorageDirectoryCommandMessage()
    {
        return _useFrench
            ? "Commande storage-dir invalide. Utilisez : EasySave --storage-dir <chemin>."
            : "Invalid storage-dir command. Use: EasySave --storage-dir <path>.";
    }

    public IReadOnlyList<string> GetHelpLines()
    {
        return new[]
        {
            GetUsageMessage(),
            string.Empty,
            _useFrench
                ? "EasySave sans argument affiche les 5 taches configurees."
                : "EasySave without arguments displays the 5 configured jobs.",
            _useFrench
                ? "Utilisez --help pour afficher cette aide."
                : "Use --help to display this help.",
            _useFrench
                ? "Les numeros de tache vont de 1 a 5."
                : "Job numbers range from 1 to 5.",
            _useFrench
                ? "Vous pouvez lancer une seule tache, une plage ou une liste separee par des points-virgules."
                : "You can run a single job, a range, or a semicolon-separated list.",
            _useFrench
                ? "Utilisez --configure <job> source <chemin> ou --configure <job> target <chemin> pour modifier un slot."
                : "Use --configure <job> source <path> or --configure <job> target <path> to update a slot.",
            _useFrench
                ? "Utilisez --storage-dir <chemin> pour deplacer jobs.json, state.json, backup-history.json et les logs."
                : "Use --storage-dir <path> to relocate jobs.json, state.json, backup-history.json, and the logs.",
            GetUsageExamples(),
            _useFrench
                ? "Exemple invalide : EasySave 2-1, car le debut de plage doit etre inferieur ou egal a la fin."
                : "Invalid example: EasySave 2-1, because the range start must be less than or equal to the end."
        };
    }

    public string GetConfiguredJobsHeader()
    {
        return _useFrench
            ? "Taches configurees :"
            : "Configured jobs:";
    }

    public string GetJobSummaryLine(int jobNumber, BackupJob job)
    {
        return _useFrench
            ? $"[{jobNumber}] {job.Name}"
            : $"[{jobNumber}] {job.Name}";
    }

    public string GetJobSourceLine(string sourcePath)
    {
        return _useFrench
            ? $"  Source : {FormatConfiguredPath(sourcePath)}"
            : $"  Source: {FormatConfiguredPath(sourcePath)}";
    }

    public string GetJobTargetLine(string targetPath)
    {
        return _useFrench
            ? $"  Cible : {FormatConfiguredPath(targetPath)}"
            : $"  Target: {FormatConfiguredPath(targetPath)}";
    }

    public string GetJobTypeLine(BackupType backupType)
    {
        return _useFrench
            ? $"  Type : {GetBackupTypeDisplayName(backupType)}"
            : $"  Type: {GetBackupTypeDisplayName(backupType)}";
    }

    public string GetJobConfigurationStatusLine(BackupJob job)
    {
        string status = IsConfigured(job)
            ? (_useFrench ? "Configure" : "Configured")
            : (_useFrench ? "Incomplet" : "Incomplete");

        return _useFrench
            ? $"  Statut : {status}"
            : $"  Status: {status}";
    }

    public string GetJobPathUpdatedMessage(int jobNumber, BackupJob job, JobPathField pathField)
    {
        string fieldName = pathField == JobPathField.Source
            ? (_useFrench ? "source" : "source")
            : (_useFrench ? "cible" : "target");
        string pathValue = pathField == JobPathField.Source ? job.Source : job.Target;

        return _useFrench
            ? $"La tache {jobNumber} a ete mise a jour : {fieldName} = {pathValue}"
            : $"Job {jobNumber} was updated: {fieldName} = {pathValue}";
    }

    public string GetStorageDirectoryUpdatedMessage(string path)
    {
        return _useFrench
            ? $"Le dossier de stockage runtime a ete mis a jour : {path}"
            : $"Runtime storage directory was updated: {path}";
    }

    public string FormatBackupResult(BackupResult result)
    {
        if (result.Status == BackupExecutionStatus.Finished)
        {
            string readableBytes = FormatBytes(result.TransferredBytes);
            string elapsedTime = FormatDuration(result.ElapsedTime);
            return _useFrench
                ? $"Fichiers transferes : {result.TransferredFileCount}\nOctets transferes : {readableBytes}\nTemps ecoule : {elapsedTime}"
                : $"Transferred files: {result.TransferredFileCount}\nTransferred bytes: {readableBytes}\nElapsed time: {elapsedTime}";
        }

        return _useFrench
            ? $"Tache {result.JobNumber} en erreur : {result.BackupName} | {result.ErrorMessage}"
            : $"Job {result.JobNumber} failed: {result.BackupName} | {result.ErrorMessage}";
    }

    public string GetBackupSuccessMessage()
    {
        return _useFrench
            ? "Sauvegarde completee avec succes !"
            : "Backup completed successfully!";
    }

    public string GetBackupTypeDisplayName(BackupType backupType)
    {
        return backupType switch
        {
            BackupType.Full => _useFrench ? "Complete" : "Full",
            BackupType.Differential => _useFrench ? "Differentielle" : "Differential",
            _ => backupType.ToString()
        };
    }

    public string GetJobStartedTitle(SelectedBackupJob selectedBackupJob)
    {
        return _useFrench
            ? $"Demarrage de la tache {selectedBackupJob.JobNumber} : {selectedBackupJob.Job.Name}"
            : $"Starting job {selectedBackupJob.JobNumber}: {selectedBackupJob.Job.Name}";
    }

    public string GetJobCompletedTitle(SelectedBackupJob selectedBackupJob)
    {
        return _useFrench
            ? $"Tache {selectedBackupJob.JobNumber} terminee : {selectedBackupJob.Job.Name}"
            : $"Completed job {selectedBackupJob.JobNumber}: {selectedBackupJob.Job.Name}";
    }

    public string GetJobProgressTitle(SelectedBackupJob selectedBackupJob)
    {
        return _useFrench
            ? $"Progression de la tache {selectedBackupJob.JobNumber} : {selectedBackupJob.Job.Name}"
            : $"Job {selectedBackupJob.JobNumber} progress: {selectedBackupJob.Job.Name}";
    }

    public string GetEligibleFilesLine(long totalEligibleFileCount)
    {
        return _useFrench
            ? $"Fichiers eligibles : {totalEligibleFileCount}"
            : $"Eligible files: {totalEligibleFileCount}";
    }

    public string GetTotalBytesLine(long totalBytes)
    {
        return _useFrench
            ? $"Taille totale a transferer : {FormatBytes(totalBytes)}"
            : $"Total size to transfer: {FormatBytes(totalBytes)}";
    }

    public string GetTransferredFilesLine(long transferredFileCount, long totalEligibleFileCount)
    {
        return _useFrench
            ? $"Fichiers transferes : {transferredFileCount}/{totalEligibleFileCount}"
            : $"Transferred files: {transferredFileCount}/{totalEligibleFileCount}";
    }

    public string GetTransferredBytesLine(long transferredBytes, long totalEligibleBytes)
    {
        return _useFrench
            ? $"Taille transferee : {FormatBytes(transferredBytes)} / {FormatBytes(totalEligibleBytes)}"
            : $"Transferred size: {FormatBytes(transferredBytes)} / {FormatBytes(totalEligibleBytes)}";
    }

    public string GetProgressLine(double progress)
    {
        return _useFrench
            ? $"Progression : {progress:F2}%"
            : $"Progress: {progress:F2}%";
    }

    public string GetCurrentFileLine(string sourcePath)
    {
        return _useFrench
            ? $"Fichier courant : {sourcePath}"
            : $"Current file: {sourcePath}";
    }

    public string GetCurrentDestinationLine(string destinationPath)
    {
        return _useFrench
            ? $"Destination : {destinationPath}"
            : $"Destination: {destinationPath}";
    }

    public string GetCompletionStatusLine()
    {
        return _useFrench
            ? "Statut : termine"
            : "Status: completed";
    }

    public string GetSkippedFileMessage(SelectedBackupJob selectedBackupJob, string filePath, string reason)
    {
        return _useFrench
            ? $"Tache {selectedBackupJob.JobNumber} ({selectedBackupJob.Job.Name}) : fichier ignore : {filePath} | Raison : {reason}"
            : $"Job {selectedBackupJob.JobNumber} ({selectedBackupJob.Job.Name}) skipped file: {filePath} | Reason: {reason}";
    }

    public string GetEmptyFileSkipReason()
    {
        return _useFrench
            ? "fichier vide"
            : "empty file";
    }

    public string GetSuspiciousExtensionSkipReason(string extension)
    {
        return _useFrench
            ? $"extension suspecte '{extension}'"
            : $"suspicious extension '{extension}'";
    }

    public string GetElapsedTimeLine(TimeSpan elapsedTime)
    {
        return _useFrench
            ? $"Temps ecoule : {FormatDuration(elapsedTime)}"
            : $"Elapsed time: {FormatDuration(elapsedTime)}";
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
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return _useFrench ? "<non configure>" : "<not configured>";
    }

    private static bool IsConfigured(BackupJob job)
    {
        return !string.IsNullOrWhiteSpace(job.Source)
            && !string.IsNullOrWhiteSpace(job.Target);
    }
}
