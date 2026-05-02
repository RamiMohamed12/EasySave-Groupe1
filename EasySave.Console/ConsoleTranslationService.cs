public class ConsoleTranslationService
{
    private readonly EnglishConsoleTexts _englishTexts = new();
    private readonly FrenchConsoleTexts _frenchTexts = new();

    public string GetMainMenuTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.MainMenuTitle : _englishTexts.MainMenuTitle;
    }

    public string GetViewJobsLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ViewJobsLabel : _englishTexts.ViewJobsLabel;
    }

    public string GetConfigureSourceLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ConfigureSourceLabel : _englishTexts.ConfigureSourceLabel;
    }

    public string GetConfigureTargetLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ConfigureTargetLabel : _englishTexts.ConfigureTargetLabel;
    }

    public string GetConfigureJobLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ConfigureJobLabel : _englishTexts.ConfigureJobLabel;
    }

    public string GetManageJobsLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ManageJobsLabel : _englishTexts.ManageJobsLabel;
    }

    public string GetAddJobLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.AddJobLabel : _englishTexts.AddJobLabel;
    }

    public string GetEditJobLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.EditJobLabel : _englishTexts.EditJobLabel;
    }

    public string GetDeleteJobLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.DeleteJobLabel : _englishTexts.DeleteJobLabel;
    }

    public string GetRunBackupsLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.RunBackupsLabel : _englishTexts.RunBackupsLabel;
    }

    public string GetViewStateLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ViewStateLabel : _englishTexts.ViewStateLabel;
    }

    public string GetViewLogsLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ViewLogsLabel : _englishTexts.ViewLogsLabel;
    }

    public string GetChangeLogFormatLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ChangeLogFormatLabel : _englishTexts.ChangeLogFormatLabel;
    }

    public string GetManageBusinessSoftwareLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ManageBusinessSoftwareLabel : _englishTexts.ManageBusinessSoftwareLabel;
    }

    public string GetChangeLanguageLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ChangeLanguageLabel : _englishTexts.ChangeLanguageLabel;
    }

    public string GetExitLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ExitLabel : _englishTexts.ExitLabel;
    }

    public string GetBackLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.BackLabel : _englishTexts.BackLabel;
    }

    public string GetSourceLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SourceLabel : _englishTexts.SourceLabel;
    }

    public string GetTargetLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.TargetLabel : _englishTexts.TargetLabel;
    }

    public string GetCurrentLanguageLine(ApplicationTextService textService)
    {
        string languageCode = textService.GetLanguageCode();

        return IsFrench(textService)
            ? _frenchTexts.GetCurrentLanguageLine(_frenchTexts.GetCurrentLanguageDisplayName(languageCode))
            : _englishTexts.GetCurrentLanguageLine(_englishTexts.GetCurrentLanguageDisplayName(languageCode));
    }

    public string GetCurrentLogFormatLine(ApplicationTextService textService)
    {
        string logFileFormat = RuntimeStoragePaths.GetLogFileFormat();

        return IsFrench(textService)
            ? _frenchTexts.GetCurrentLogFormatLine(logFileFormat)
            : _englishTexts.GetCurrentLogFormatLine(logFileFormat);
    }

    public string GetMenuOptionLabel(int optionNumber, string label)
    {
        return _englishTexts.GetMenuOptionLabel(optionNumber, label);
    }

    public string GetLanguageOptionLabel(int optionNumber, string label)
    {
        return _englishTexts.GetLanguageOptionLabel(optionNumber, label);
    }

    public string GetSourcePathPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SourcePathPrompt : _englishTexts.SourcePathPrompt;
    }

    public string GetTargetPathPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.TargetPathPrompt : _englishTexts.TargetPathPrompt;
    }

    public string GetPastePathLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.PastePathLabel : _englishTexts.PastePathLabel;
    }

    public string GetPasteSourcePathLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.PasteSourcePathLabel : _englishTexts.PasteSourcePathLabel;
    }

    public string GetPasteTargetPathLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.PasteTargetPathLabel : _englishTexts.PasteTargetPathLabel;
    }

    public string GetSearchDirectoryLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SearchDirectoryLabel : _englishTexts.SearchDirectoryLabel;
    }

    public string GetSkipLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SkipLabel : _englishTexts.SkipLabel;
    }

    public string GetPathInputModePrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.PathInputModePrompt : _englishTexts.PathInputModePrompt;
    }

    public string GetSearchRootPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SearchRootPrompt : _englishTexts.SearchRootPrompt;
    }

    public string GetSearchQueryPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SearchQueryPrompt : _englishTexts.SearchQueryPrompt;
    }

    public string GetNoSearchMatchesMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NoSearchMatchesMessage : _englishTexts.NoSearchMatchesMessage;
    }

    public string GetSearchResultSelectionPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SearchResultSelectionPrompt : _englishTexts.SearchResultSelectionPrompt;
    }

    public string GetInvalidSearchResultSelectionMessage(ApplicationTextService textService)
    {
        return IsFrench(textService)
            ? _frenchTexts.InvalidSearchResultSelectionMessage
            : _englishTexts.InvalidSearchResultSelectionMessage;
    }

    public string GetSearchUnsupportedMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SearchUnsupportedMessage : _englishTexts.SearchUnsupportedMessage;
    }

    public string GetInvalidSearchRootMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.InvalidSearchRootMessage : _englishTexts.InvalidSearchRootMessage;
    }

    public string GetDirectoryDoesNotExistMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.DirectoryDoesNotExistMessage : _englishTexts.DirectoryDoesNotExistMessage;
    }

    public string GetConfigurationCompletedMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ConfigurationCompletedMessage : _englishTexts.ConfigurationCompletedMessage;
    }

    public string GetNoConfigurationChangesMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NoConfigurationChangesMessage : _englishTexts.NoConfigurationChangesMessage;
    }

    public string GetSelectedJobLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SelectedJobLabel : _englishTexts.SelectedJobLabel;
    }

    public string GetSourcePathKeepExistingPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SourcePathKeepExistingPrompt : _englishTexts.SourcePathKeepExistingPrompt;
    }

    public string GetTargetPathKeepExistingPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.TargetPathKeepExistingPrompt : _englishTexts.TargetPathKeepExistingPrompt;
    }

    public string GetNoLogsFoundMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NoLogsFoundMessage : _englishTexts.NoLogsFoundMessage;
    }

    public string GetAvailableLogsLine(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.AvailableLogsLine : _englishTexts.AvailableLogsLine;
    }

    public string GetLogSelectionPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.LogSelectionPrompt : _englishTexts.LogSelectionPrompt;
    }

    public string GetInvalidLogSelectionMessage(ApplicationTextService textService)
    {
        return IsFrench(textService)
            ? _frenchTexts.InvalidLogSelectionMessage
            : _englishTexts.InvalidLogSelectionMessage;
    }

    public string GetBusinessSoftwareTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.BusinessSoftwareTitle : _englishTexts.BusinessSoftwareTitle;
    }

    public string GetAddProcessLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.AddProcessLabel : _englishTexts.AddProcessLabel;
    }

    public string GetRemoveProcessLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.RemoveProcessLabel : _englishTexts.RemoveProcessLabel;
    }

    public string GetNoBlockedProcessesMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NoBlockedProcessesMessage : _englishTexts.NoBlockedProcessesMessage;
    }

    public string GetProcessNamePrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ProcessNamePrompt : _englishTexts.ProcessNamePrompt;
    }

    public string GetProcessAlreadyConfiguredMessage(ApplicationTextService textService)
    {
        return IsFrench(textService)
            ? _frenchTexts.ProcessAlreadyConfiguredMessage
            : _englishTexts.ProcessAlreadyConfiguredMessage;
    }

    public string GetProcessAddedMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ProcessAddedMessage : _englishTexts.ProcessAddedMessage;
    }

    public string GetProcessRemovedMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ProcessRemovedMessage : _englishTexts.ProcessRemovedMessage;
    }

    public string GetFilePathLine(ApplicationTextService textService, string filePath)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetFilePathLine(filePath)
            : _englishTexts.GetFilePathLine(filePath);
    }

    public string GetFileNotFoundMessage(ApplicationTextService textService, string displayName)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetFileNotFoundMessage(displayName)
            : _englishTexts.GetFileNotFoundMessage(displayName);
    }

    public string GetFileEmptyMessage(ApplicationTextService textService, string displayName)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetFileEmptyMessage(displayName)
            : _englishTexts.GetFileEmptyMessage(displayName);
    }

    public string GetLogFormatUpdatedMessage(ApplicationTextService textService, string logFileFormat)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetLogFormatUpdatedMessage(logFileFormat)
            : _englishTexts.GetLogFormatUpdatedMessage(logFileFormat);
    }

    public string GetConfigurePathTitle(ApplicationTextService textService, JobPathField pathField)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetConfigurePathTitle(pathField)
            : _englishTexts.GetConfigurePathTitle(pathField);
    }

    public string GetSearchStoppedMessage(ApplicationTextService textService, int resultLimit)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetSearchStoppedMessage(resultLimit)
            : _englishTexts.GetSearchStoppedMessage(resultLimit);
    }

    public string GetSelectionInstructionsTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SelectionInstructionsTitle : _englishTexts.SelectionInstructionsTitle;
    }

    public string GetSingleSelectionExample(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SingleSelectionExample : _englishTexts.SingleSelectionExample;
    }

    public string GetRangeSelectionExample(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.RangeSelectionExample : _englishTexts.RangeSelectionExample;
    }

    public string GetMultipleSelectionExample(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.MultipleSelectionExample : _englishTexts.MultipleSelectionExample;
    }

    public string GetRunningJobsMessage(ApplicationTextService textService, int jobCount)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetRunningJobsMessage(jobCount)
            : _englishTexts.GetRunningJobsMessage(jobCount);
    }

    public string GetNoValidJobsSelectedMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NoValidJobsSelectedMessage : _englishTexts.NoValidJobsSelectedMessage;
    }

    public string GetAvailableJobsLine(ApplicationTextService textService, int jobCount)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetAvailableJobsLine(jobCount)
            : _englishTexts.GetAvailableJobsLine(jobCount);
    }

    public string GetJobNumberPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.JobNumberPrompt : _englishTexts.JobNumberPrompt;
    }

    public string GetNewJobNumberPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NewJobNumberPrompt : _englishTexts.NewJobNumberPrompt;
    }

    public string GetInvalidJobNumberSelectionMessage(ApplicationTextService textService, int jobCount)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetInvalidJobNumberSelectionMessage(jobCount)
            : _englishTexts.GetInvalidJobNumberSelectionMessage(jobCount);
    }

    public string GetInvalidMenuChoiceMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.InvalidMenuChoiceMessage : _englishTexts.InvalidMenuChoiceMessage;
    }

    public string GetInvalidLanguageSelectionMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.InvalidLanguageSelectionMessage : _englishTexts.InvalidLanguageSelectionMessage;
    }

    public string GetLanguageSelectionPrompt(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.LanguageSelectionPrompt : _englishTexts.LanguageSelectionPrompt;
    }

    public string GetPauseMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.PauseMessage : _englishTexts.PauseMessage;
    }

    public string GetNavigationHelp(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NavigationHelp : _englishTexts.NavigationHelp;
    }

    public string GetMultiSelectNavigationHelp(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.MultiSelectNavigationHelp : _englishTexts.MultiSelectNavigationHelp;
    }

    public string GetLeaveEmptyToGoBackMessage(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.LeaveEmptyToGoBackMessage : _englishTexts.LeaveEmptyToGoBackMessage;
    }

    public string BuildErrorMessage(ApplicationTextService textService, string details)
    {
        return IsFrench(textService)
            ? _frenchTexts.BuildErrorMessage(details)
            : _englishTexts.BuildErrorMessage(details);
    }

    public string GetConfiguredLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ConfiguredLabel : _englishTexts.ConfiguredLabel;
    }

    public string GetIncompleteLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.IncompleteLabel : _englishTexts.IncompleteLabel;
    }

    public string GetCurrentValueLine(ApplicationTextService textService, string value)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetCurrentValueLine(value)
            : _englishTexts.GetCurrentValueLine(value);
    }

    public string GetSelectedCountLine(ApplicationTextService textService, int count)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetSelectedCountLine(count)
            : _englishTexts.GetSelectedCountLine(count);
    }

    public string GetNotConfiguredLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.NotConfiguredLabel : _englishTexts.NotConfiguredLabel;
    }

    public string GetConfigurationSuccessMessage(
        ApplicationTextService textService,
        int jobNumber,
        BackupJob updatedJob,
        JobPathField pathField)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetConfigurationSuccessMessage(jobNumber, updatedJob, pathField)
            : _englishTexts.GetConfigurationSuccessMessage(jobNumber, updatedJob, pathField);
    }

    public string GetJobAddedMessage(ApplicationTextService textService, int jobNumber)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetJobAddedMessage(jobNumber)
            : _englishTexts.GetJobAddedMessage(jobNumber);
    }

    public string GetJobEditedMessage(ApplicationTextService textService, int jobNumber)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetJobEditedMessage(jobNumber)
            : _englishTexts.GetJobEditedMessage(jobNumber);
    }

    public string GetJobDeletedMessage(ApplicationTextService textService, int jobNumber)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetJobDeletedMessage(jobNumber)
            : _englishTexts.GetJobDeletedMessage(jobNumber);
    }

    public string GetJobAlreadyExistsMessage(ApplicationTextService textService, int jobNumber)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetJobAlreadyExistsMessage(jobNumber)
            : _englishTexts.GetJobAlreadyExistsMessage(jobNumber);
    }

    public string GetJobNumberDoesNotExistMessage(ApplicationTextService textService, int jobNumber)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetJobNumberDoesNotExistMessage(jobNumber)
            : _englishTexts.GetJobNumberDoesNotExistMessage(jobNumber);
    }

    public string GetAddJobTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.AddJobTitle : _englishTexts.AddJobTitle;
    }

    public string GetEditJobTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.EditJobTitle : _englishTexts.EditJobTitle;
    }

    public string GetDeleteJobTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.DeleteJobTitle : _englishTexts.DeleteJobTitle;
    }

    public string GetConfirmDeleteLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ConfirmDeleteLabel : _englishTexts.ConfirmDeleteLabel;
    }

    public string GetSelectBackupTypeTitle(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.SelectBackupTypeTitle : _englishTexts.SelectBackupTypeTitle;
    }

    public string GetChangeTypeLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.ChangeTypeLabel : _englishTexts.ChangeTypeLabel;
    }

    public string GetJobHeader(ApplicationTextService textService, BackupResult result)
    {
        return IsFrench(textService)
            ? _frenchTexts.GetJobHeader(result)
            : _englishTexts.GetJobHeader(result);
    }

    private static bool IsFrench(ApplicationTextService textService)
    {
        return string.Equals(
            textService.GetLanguageCode(),
            ApplicationTextService.FrenchLanguageCode,
            StringComparison.OrdinalIgnoreCase);
    }
}
