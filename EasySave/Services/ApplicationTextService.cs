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
            ? "Utilisation : EasySave <selection-des-taches>"
            : "Usage: EasySave <job-selection>";
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
}
