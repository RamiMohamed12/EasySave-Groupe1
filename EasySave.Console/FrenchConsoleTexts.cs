public class FrenchConsoleTexts
{
    public string MainMenuTitle => "Menu principal";
    public string ViewJobsLabel => "Voir les taches";
    public string ConfigureSourceLabel => "Configurer la source";
    public string ConfigureTargetLabel => "Configurer la cible";
    public string ConfigureJobLabel => "Configurer une tache";
    public string RunBackupsLabel => "Lancer les sauvegardes";
    public string ViewStateLabel => "Voir l'etat";
    public string ViewLogsLabel => "Voir les logs";
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
    public string PastePathLabel => "Coller un chemin";
    public string PasteSourcePathLabel => "Coller le chemin source";
    public string PasteTargetPathLabel => "Coller le chemin cible";
    public string SearchDirectoryLabel => "Rechercher un dossier";
    public string SkipLabel => "Ignorer";
    public string PathInputModePrompt => "Choisissez comment definir ce chemin : ";
    public string SearchRootPrompt => "Entrez la racine de recherche (exemple C:\\ ou D:\\Data) : ";
    public string SearchQueryPrompt => "Entrez le nom du dossier a rechercher : ";
    public string NoSearchMatchesMessage => "Aucun dossier correspondant trouve.";
    public string SearchResultSelectionPrompt => "Choisissez un numero de resultat : ";
    public string InvalidSearchResultSelectionMessage => "Selection de resultat invalide.";
    public string SearchUnsupportedMessage => "La recherche de dossiers est uniquement disponible sur Windows. Collez un chemin a la place.";
    public string InvalidSearchRootMessage => "La racine de recherche n'existe pas.";
    public string DirectoryDoesNotExistMessage => "Le dossier n'existe pas.";
    public string ConfigurationCompletedMessage => "La configuration a reussi.";
    public string NoConfigurationChangesMessage => "Aucune modification de configuration n'a ete effectuee.";
    public string SelectedJobLabel => "Tache selectionnee :";
    public string SourcePathKeepExistingPrompt => "Chemin source (laisser vide pour conserver la valeur actuelle) : ";
    public string TargetPathKeepExistingPrompt => "Chemin cible (laisser vide pour conserver la valeur actuelle) : ";
    public string NoLogsFoundMessage => "Aucun fichier de log trouve.";
    public string AvailableLogsLine => "Logs disponibles :";
    public string LogSelectionPrompt => "Choisissez un numero de log : ";
    public string InvalidLogSelectionMessage => "Selection de log invalide.";

    public string GetFilePathLine(string filePath)
    {
        return $"Fichier : {filePath}";
    }

    public string GetFileNotFoundMessage(string displayName)
    {
        return $"{displayName} n'existe pas encore.";
    }

    public string GetFileEmptyMessage(string displayName)
    {
        return $"{displayName} est vide.";
    }

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

    public string GetConfigurePathTitle(JobPathField pathField)
    {
        return pathField == JobPathField.Source
            ? "Configurer la source"
            : "Configurer la cible";
    }

    public string GetSearchStoppedMessage(int resultLimit)
    {
        return $"La recherche s'est arretee apres {resultLimit} resultats. Affinez la recherche si necessaire.";
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
