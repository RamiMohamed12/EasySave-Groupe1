public abstract class ConsoleTexts
{
    private readonly ApplicationTextService _textService;

    protected ConsoleTexts(string languageCode)
    {
        _textService = ApplicationTextService.Create(languageCode);
    }

    public string MainMenuTitle => Text("Console.MainMenuTitle");
    public string ViewJobsLabel => Text("Console.ViewJobsLabel");
    public string ConfigureSourceLabel => Text("Console.ConfigureSourceLabel");
    public string ConfigureTargetLabel => Text("Console.ConfigureTargetLabel");
    public string ConfigureJobLabel => Text("Console.ConfigureJobLabel");
    public string ManageJobsLabel => Text("Console.ManageJobsLabel");
    public string AddJobLabel => Text("Console.AddJobLabel");
    public string EditJobLabel => Text("Console.EditJobLabel");
    public string DeleteJobLabel => Text("Console.DeleteJobLabel");
    public string RunBackupsLabel => Text("Console.RunBackupsLabel");
    public string ViewStateLabel => Text("Console.ViewStateLabel");
    public string ViewLogsLabel => Text("Console.ViewLogsLabel");
    public string ChangeLogFormatLabel => Text("Console.ChangeLogFormatLabel");
    public string ChangeLanguageLabel => Text("Console.ChangeLanguageLabel");
    public string ExitLabel => Text("Console.ExitLabel");
    public string BackLabel => Text("Console.BackLabel");
    public string SourceLabel => Text("Console.SourceLabel");
    public string TargetLabel => Text("Console.TargetLabel");
    public string SelectionInstructionsTitle => Text("Console.SelectionInstructionsTitle");
    public string SingleSelectionExample => Text("Console.SingleSelectionExample");
    public string RangeSelectionExample => Text("Console.RangeSelectionExample");
    public string MultipleSelectionExample => Text("Console.MultipleSelectionExample");
    public string NoValidJobsSelectedMessage => Text("Console.NoValidJobsSelectedMessage");
    public string InvalidMenuChoiceMessage => Text("Console.InvalidMenuChoiceMessage");
    public string InvalidLanguageSelectionMessage => Text("Console.InvalidLanguageSelectionMessage");
    public string LanguageSelectionPrompt => Text("Console.LanguageSelectionPrompt");
    public string PauseMessage => Text("Console.PauseMessage");
    public string NavigationHelp => Text("Console.NavigationHelp");
    public string MultiSelectNavigationHelp => Text("Console.MultiSelectNavigationHelp");
    public string LeaveEmptyToGoBackMessage => Text("Console.LeaveEmptyToGoBackMessage");
    public string SourcePathPrompt => Text("Console.SourcePathPrompt");
    public string TargetPathPrompt => Text("Console.TargetPathPrompt");
    public string JobNumberPrompt => Text("Console.JobNumberPrompt");
    public string NewJobNumberPrompt => Text("Console.NewJobNumberPrompt");
    public string NotConfiguredLabel => Text("NotConfiguredLabel");
    public string ConfiguredLabel => Text("ConfiguredLabel");
    public string IncompleteLabel => Text("IncompleteLabel");
    public string PastePathLabel => Text("Console.PastePathLabel");
    public string PasteSourcePathLabel => Text("Console.PasteSourcePathLabel");
    public string PasteTargetPathLabel => Text("Console.PasteTargetPathLabel");
    public string SearchDirectoryLabel => Text("Console.SearchDirectoryLabel");
    public string SkipLabel => Text("Console.SkipLabel");
    public string PathInputModePrompt => Text("Console.PathInputModePrompt");
    public string SearchRootPrompt => Text("Console.SearchRootPrompt");
    public string SearchQueryPrompt => Text("Console.SearchQueryPrompt");
    public string NoSearchMatchesMessage => Text("Console.NoSearchMatchesMessage");
    public string SearchResultSelectionPrompt => Text("Console.SearchResultSelectionPrompt");
    public string InvalidSearchResultSelectionMessage => Text("Console.InvalidSearchResultSelectionMessage");
    public string SearchUnsupportedMessage => Text("Console.SearchUnsupportedMessage");
    public string InvalidSearchRootMessage => Text("Console.InvalidSearchRootMessage");
    public string DirectoryDoesNotExistMessage => Text("Console.DirectoryDoesNotExistMessage");
    public string ConfigurationCompletedMessage => Text("Console.ConfigurationCompletedMessage");
    public string AddJobTitle => Text("Console.AddJobTitle");
    public string EditJobTitle => Text("Console.EditJobTitle");
    public string DeleteJobTitle => Text("Console.DeleteJobTitle");
    public string ConfirmDeleteLabel => Text("Console.ConfirmDeleteLabel");
    public string SelectBackupTypeTitle => Text("Console.SelectBackupTypeTitle");
    public string ChangeTypeLabel => Text("Console.ChangeTypeLabel");
    public string NoConfigurationChangesMessage => Text("Console.NoConfigurationChangesMessage");
    public string SelectedJobLabel => Text("Console.SelectedJobLabel");
    public string SourcePathKeepExistingPrompt => Text("Console.SourcePathKeepExistingPrompt");
    public string TargetPathKeepExistingPrompt => Text("Console.TargetPathKeepExistingPrompt");
    public string NoLogsFoundMessage => Text("Console.NoLogsFoundMessage");
    public string AvailableLogsLine => Text("Console.AvailableLogsLine");
    public string LogSelectionPrompt => Text("Console.LogSelectionPrompt");
    public string InvalidLogSelectionMessage => Text("Console.InvalidLogSelectionMessage");

    public string GetFilePathLine(string filePath) => Format("Console.FilePathLine", filePath);

    public string GetFileNotFoundMessage(string displayName) => Format("Console.FileNotFoundMessage", displayName);

    public string GetFileEmptyMessage(string displayName) => Format("Console.FileEmptyMessage", displayName);

    public string GetCurrentLanguageLine(string currentLanguageDisplayName)
    {
        return Format("Console.CurrentLanguageLine", currentLanguageDisplayName);
    }

    public string GetCurrentLogFormatLine(string logFileFormat)
    {
        return Format("Console.CurrentLogFormatLine", GetLogFileFormatDisplayName(logFileFormat));
    }

    public string GetCurrentValueLine(string value) => Format("Console.CurrentValueLine", value);

    public string GetSelectedCountLine(int count) => Format("Console.SelectedCountLine", count);

    public string GetCurrentLanguageDisplayName(string languageCode) => _textService.GetLanguageDisplayName(languageCode);

    public string GetLogFileFormatDisplayName(string logFileFormat) => _textService.GetLogFileFormatDisplayName(logFileFormat);

    public string GetMenuOptionLabel(int optionNumber, string label) => Format("Console.MenuOptionLabel", optionNumber, label);

    public string GetLanguageOptionLabel(int optionNumber, string label) => Format("Console.LanguageOptionLabel", optionNumber, label);

    public string GetRunningJobsMessage(int jobCount) => Format("Console.RunningJobsMessage", jobCount);

    public string GetAvailableJobsLine(int jobCount) => Format("Console.AvailableJobsLine", jobCount);

    public string GetInvalidJobNumberSelectionMessage(int jobCount)
    {
        return Format("Console.InvalidJobNumberSelectionMessage", jobCount);
    }

    public string GetJobAddedMessage(int jobNumber) => Format("Console.JobAddedMessage", jobNumber);

    public string GetJobEditedMessage(int jobNumber) => Format("Console.JobEditedMessage", jobNumber);

    public string GetJobDeletedMessage(int jobNumber) => Format("Console.JobDeletedMessage", jobNumber);

    public string GetJobAlreadyExistsMessage(int jobNumber) => Format("Console.JobAlreadyExistsMessage", jobNumber);

    public string GetJobNumberDoesNotExistMessage(int jobNumber) => Format("Console.JobNumberDoesNotExistMessage", jobNumber);

    public string GetConfigurationSuccessMessage(int jobNumber, BackupJob updatedJob, JobPathField pathField)
    {
        string fieldName = _textService.GetPathFieldDisplayName(pathField);
        string pathValue = pathField == JobPathField.Source ? updatedJob.Source : updatedJob.Target;
        return Format("Console.ConfigurationSuccessMessage", jobNumber, fieldName, FormatPath(pathValue));
    }

    public string GetConfigurePathTitle(JobPathField pathField)
    {
        return pathField == JobPathField.Source ? Text("Console.ConfigureSourceTitle") : Text("Console.ConfigureTargetTitle");
    }

    public string GetSearchStoppedMessage(int resultLimit) => Format("Console.SearchStoppedMessage", resultLimit);

    public string GetJobHeader(BackupResult result) => Format("Console.JobHeader", result.JobNumber, result.BackupName);

    public string BuildErrorMessage(string details) => Format("Console.ErrorMessage", details);

    public string GetLogFormatUpdatedMessage(string logFileFormat)
    {
        return Format("Console.LogFormatUpdatedMessage", GetLogFileFormatDisplayName(logFileFormat));
    }

    private string Text(string key) => _textService.GetText(key);

    private string Format(string key, params object[] args) => _textService.FormatText(key, args);

    private string FormatPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? NotConfiguredLabel : $"<{path}>";
    }
}
