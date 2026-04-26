public class FrenchConsoleTexts
{
    public string MainMenuTitle => "Menu principal";
    public string ViewJobsLabel => "Voir les taches";
    public string ConfigureSourceLabel => "Configurer la source";
    public string ConfigureTargetLabel => "Configurer la cible";
    public string RunBackupsLabel => "Lancer les sauvegardes";
    public string ChangeLanguageLabel => "Changer la langue";
    public string ExitLabel => "Quitter";
    public string BackLabel => "Retour";
    public string SourceLabel => "  Source : ";
    public string TargetLabel => "  Cible : ";
    public string SelectionInstructionsTitle => "Entrez la selection des taches :";
    public string SingleSelectionExample => "  - Tache unique : 1";
    public string RangeSelectionExample => "  - Plage : 1-3";
    public string MultipleSelectionExample => "  - Multiple : 1;3;5";
    public string NoValidJobsSelectedMessage => "Aucune tache valide selectionnee !";
    public string InvalidMenuChoiceMessage => "Choix invalide dans le menu.";
    public string InvalidLanguageSelectionMessage => "Choix de langue invalide.";
    public string LanguageSelectionPrompt => "Choisissez une langue : ";
    public string PauseMessage => "Appuyez sur une touche pour continuer...";
    public string SourcePathPrompt => "Entrez le chemin source : ";
    public string TargetPathPrompt => "Entrez le chemin cible : ";
    public string JobNumberPrompt => "Entrez le numero de tache : ";
    public string NotConfiguredLabel => "<non configure>";

    public string GetCurrentLanguageLine(string currentLanguageDisplayName)
    {
        return $"Langue actuelle : {currentLanguageDisplayName}";
    }

    public string GetCurrentLanguageDisplayName(string languageCode)
    {
        return languageCode == ApplicationTextService.FrenchLanguageCode
            ? "francais"
            : "anglais";
    }

    public string GetMenuOptionLabel(int optionNumber, string label)
    {
        return $"{optionNumber}. {label}";
    }

    public string GetLanguageOptionLabel(int optionNumber, string label)
    {
        return $"{optionNumber}. {label}";
    }

    public string GetRunningJobsMessage(int jobCount)
    {
        return $"Execution de {jobCount} tache(s) de sauvegarde...";
    }

    public string GetAvailableJobsLine(int jobCount)
    {
        return $"Taches disponibles : 1-{jobCount}";
    }

    public string GetInvalidJobNumberSelectionMessage(int jobCount)
    {
        return $"Numero de tache invalide. Utilisez une valeur entre 1 et {jobCount}.";
    }

    public string GetConfigurationSuccessMessage(int jobNumber, BackupJob updatedJob, JobPathField pathField)
    {
        string fieldName = pathField == JobPathField.Source ? "source" : "cible";
        string pathValue = pathField == JobPathField.Source ? updatedJob.Source : updatedJob.Target;
        return $"La tache {jobNumber} a ete mise a jour : {fieldName} = {FormatPath(pathValue)}";
    }

    public string GetJobHeader(BackupResult result)
    {
        return $"Tache {result.JobNumber} : {result.BackupName}";
    }

    public string BuildErrorMessage(string details)
    {
        return $"Erreur : {details}";
    }

    private string FormatPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? NotConfiguredLabel
            : $"<{path}>";
    }
}
