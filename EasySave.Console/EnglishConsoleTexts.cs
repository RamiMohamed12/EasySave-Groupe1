public class EnglishConsoleTexts
{
    public string MainMenuTitle => "Main menu";
    public string ViewJobsLabel => "View jobs";
    public string ConfigureSourceLabel => "Configure source";
    public string ConfigureTargetLabel => "Configure target";
    public string ConfigureJobLabel => "Configure job";
    public string RunBackupsLabel => "Run backups";
    public string ViewStateLabel => "View state";
    public string ViewLogsLabel => "View logs";
    public string ChangeLanguageLabel => "Change language";
    public string ExitLabel => "Exit";
    public string BackLabel => "Back";
    public string SourceLabel => "  Source: ";
    public string TargetLabel => "  Target: ";
    public string SelectionInstructionsTitle => "Enter job selection:";
    public string SingleSelectionExample => "  - Single job: 1";
    public string RangeSelectionExample => "  - Range: 1-3";
    public string MultipleSelectionExample => "  - Multiple: 1;3;5";
    public string NoValidJobsSelectedMessage => "No valid jobs selected!";
    public string InvalidMenuChoiceMessage => "Invalid menu choice.";
    public string InvalidLanguageSelectionMessage => "Invalid language selection.";
    public string LanguageSelectionPrompt => "Choose a language: ";
    public string PauseMessage => "Press any key to continue...";
    public string NavigationHelp => "Use Up/Down to move, Enter to select, Esc to go back.";
    public string MultiSelectNavigationHelp => "Use Up/Down to move, Space to toggle, Enter to confirm, Esc to go back.";
    public string LeaveEmptyToGoBackMessage => "Leave the field empty to go back.";
    public string SourcePathPrompt => "Enter source path: ";
    public string TargetPathPrompt => "Enter target path: ";
    public string JobNumberPrompt => "Enter job number: ";
    public string NotConfiguredLabel => "<not configured>";
    public string ConfiguredLabel => "Configured";
    public string IncompleteLabel => "Incomplete";
    public string PastePathLabel => "Paste path";
    public string PasteSourcePathLabel => "Paste source path";
    public string PasteTargetPathLabel => "Paste target path";
    public string SearchDirectoryLabel => "Search directory";
    public string SkipLabel => "Skip";
    public string PathInputModePrompt => "Choose how to set this path: ";
    public string SearchRootPrompt => "Enter search root (example C:\\ or D:\\Data): ";
    public string SearchQueryPrompt => "Enter directory name to search: ";
    public string NoSearchMatchesMessage => "No matching directories found.";
    public string SearchResultSelectionPrompt => "Choose a result number: ";
    public string InvalidSearchResultSelectionMessage => "Invalid result selection.";
    public string SearchUnsupportedMessage => "Directory search is only supported on Windows. Paste a path instead.";
    public string InvalidSearchRootMessage => "Search root does not exist.";
    public string DirectoryDoesNotExistMessage => "Directory does not exist.";
    public string ConfigurationCompletedMessage => "Configuration was successful.";
    public string NoConfigurationChangesMessage => "No configuration changes were made.";
    public string SelectedJobLabel => "Selected job:";
    public string SourcePathKeepExistingPrompt => "Source path (leave empty to keep unchanged): ";
    public string TargetPathKeepExistingPrompt => "Target path (leave empty to keep unchanged): ";
    public string NoLogsFoundMessage => "No log files were found.";
    public string AvailableLogsLine => "Available logs:";
    public string LogSelectionPrompt => "Choose a log number: ";
    public string InvalidLogSelectionMessage => "Invalid log selection.";

    public string GetFilePathLine(string filePath)
    {
        return $"File: {filePath}";
    }

    public string GetFileNotFoundMessage(string displayName)
    {
        return $"{displayName} does not exist yet.";
    }

    public string GetFileEmptyMessage(string displayName)
    {
        return $"{displayName} is empty.";
    }

    public string GetCurrentLanguageLine(string currentLanguageDisplayName)
    {
        return $"Current language: {currentLanguageDisplayName}";
    }

    public string GetCurrentValueLine(string value)
    {
        return $"Current value: {value}";
    }

    public string GetSelectedCountLine(int count)
    {
        return $"Selected jobs: {count}";
    }

    public string GetCurrentLanguageDisplayName(string languageCode)
    {
        return languageCode == ApplicationTextService.FrenchLanguageCode
            ? "French"
            : "English";
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
        return $"Running {jobCount} backup job(s)...";
    }

    public string GetAvailableJobsLine(int jobCount)
    {
        return $"Available jobs: 1-{jobCount}";
    }

    public string GetInvalidJobNumberSelectionMessage(int jobCount)
    {
        return $"Invalid job number. Use a value between 1 and {jobCount}.";
    }

    public string GetConfigurationSuccessMessage(int jobNumber, BackupJob updatedJob, JobPathField pathField)
    {
        string fieldName = pathField == JobPathField.Source ? "source" : "target";
        string pathValue = pathField == JobPathField.Source ? updatedJob.Source : updatedJob.Target;
        return $"Job {jobNumber} was updated: {fieldName} = {FormatPath(pathValue)}";
    }

    public string GetConfigurePathTitle(JobPathField pathField)
    {
        return pathField == JobPathField.Source
            ? "Configure source"
            : "Configure target";
    }

    public string GetSearchStoppedMessage(int resultLimit)
    {
        return $"Search stopped after {resultLimit} results. Refine the query if needed.";
    }

    public string GetJobHeader(BackupResult result)
    {
        return $"Job {result.JobNumber}: {result.BackupName}";
    }

    public string BuildErrorMessage(string details)
    {
        return $"Error: {details}";
    }

    private string FormatPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? NotConfiguredLabel
            : $"<{path}>";
    }
}
