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

    public string BuildErrorMessage(ApplicationTextService textService, string details)
    {
        return IsFrench(textService)
            ? _frenchTexts.BuildErrorMessage(details)
            : _englishTexts.BuildErrorMessage(details);
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
