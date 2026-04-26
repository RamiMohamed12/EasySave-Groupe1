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

    public string GetRunBackupsLabel(ApplicationTextService textService)
    {
        return IsFrench(textService) ? _frenchTexts.RunBackupsLabel : _englishTexts.RunBackupsLabel;
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
