public class EnglishConsoleTexts
{
    public string MainMenuTitle => "Main menu";
    public string ViewJobsLabel => "View jobs";
    public string ConfigureSourceLabel => "Configure source";
    public string ConfigureTargetLabel => "Configure target";
    public string RunBackupsLabel => "Run backups";
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
    public string SourcePathPrompt => "Enter source path: ";
    public string TargetPathPrompt => "Enter target path: ";
    public string JobNumberPrompt => "Enter job number: ";
    public string NotConfiguredLabel => "<not configured>";

    public string GetCurrentLanguageLine(string currentLanguageDisplayName)
    {
        return $"Current language: {currentLanguageDisplayName}";
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
