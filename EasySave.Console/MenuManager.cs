public class MenuManager
{
    private const int MenuInnerWidth = 42;

    private readonly Func<string?, ConsoleMenuRuntime> _runtimeFactory;
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;

    private ConsoleMenuRuntime _runtime;

    public MenuManager(
        Func<string?, ConsoleMenuRuntime> runtimeFactory,
        BackupJobRegistry jobRegistry,
        StateService stateService)
    {
        _runtimeFactory = runtimeFactory;
        _jobRegistry = jobRegistry;
        _stateService = stateService;
        _runtime = _runtimeFactory(null);
    }

    public void Start()
    {
        while (true)
        {
            Console.Clear();
            DisplayMainMenu();

            Console.Write("\n> ");
            string? choice = Console.ReadLine();

            switch (choice?.Trim().ToLowerInvariant())
            {
                case "1":
                    ViewJobs();
                    break;
                case "2":
                    ConfigureJobSource();
                    break;
                case "3":
                    ConfigureJobTarget();
                    break;
                case "4":
                    RunBackups();
                    break;
                case "5":
                    ChangeLanguage();
                    break;
                case "6":
                    return;
                default:
                    WriteError(GetInvalidMenuChoiceMessage());
                    Pause();
                    break;
            }
        }
    }

    private ApplicationTextService TextService => _runtime.TextService;

    private ArgumentParser ArgumentParser => _runtime.ArgumentParser;

    private BackupController BackupController => _runtime.BackupController;

    private void DisplayMainMenu()
    {
        WriteMenuBorder();
        WriteMenuLine(GetMainMenuTitle());
        WriteMenuBorder();
        WriteMenuLine(GetMenuOptionLabel(1, IsFrench() ? "Voir les taches" : "View jobs"));
        WriteMenuLine(GetMenuOptionLabel(2, IsFrench() ? "Configurer la source" : "Configure source"));
        WriteMenuLine(GetMenuOptionLabel(3, IsFrench() ? "Configurer la cible" : "Configure target"));
        WriteMenuLine(GetMenuOptionLabel(4, IsFrench() ? "Lancer les sauvegardes" : "Run backups"));
        WriteMenuLine(GetMenuOptionLabel(5, IsFrench() ? "Changer la langue" : "Change language"));
        WriteMenuLine(GetMenuOptionLabel(6, IsFrench() ? "Quitter" : "Exit"));
        WriteMenuBorder();
        Console.WriteLine(GetCurrentLanguageLine());
    }

    private void ViewJobs()
    {
        Console.Clear();
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        RenderJobs(jobs);
        Pause();
    }

    private void ConfigureJobSource()
    {
        Console.Clear();
        WriteSectionHeader(
            IsFrench() ? "Configurer la source" : "Configure source");

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        Console.Write(GetSourcePathPrompt());
        string? sourcePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            WriteError(TextService.GetPathValueRequiredMessage());
            Pause();
            return;
        }

        try
        {
            BackupJob updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Source, sourcePath);
            RenderConfigurationSuccess(jobNumber, updatedJob, JobPathField.Source);
        }
        catch (Exception ex)
        {
            WriteError(BuildErrorMessage(ex.Message));
        }

        Pause();
    }

    private void ConfigureJobTarget()
    {
        Console.Clear();
        WriteSectionHeader(
            IsFrench() ? "Configurer la cible" : "Configure target");

        int jobNumber = GetJobNumber();
        if (jobNumber == -1)
        {
            return;
        }

        Console.Write(GetTargetPathPrompt());
        string? targetPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            WriteError(TextService.GetPathValueRequiredMessage());
            Pause();
            return;
        }

        try
        {
            BackupJob updatedJob = _jobRegistry.UpdateJobPath(jobNumber, JobPathField.Target, targetPath);
            RenderConfigurationSuccess(jobNumber, updatedJob, JobPathField.Target);
        }
        catch (Exception ex)
        {
            WriteError(BuildErrorMessage(ex.Message));
        }

        Pause();
    }

    private void RunBackups()
    {
        Console.Clear();
        WriteSectionHeader(IsFrench() ? "Lancer les sauvegardes" : "Run backups");
        Console.WriteLine(GetSelectionInstructionsTitle());
        Console.WriteLine(GetSingleSelectionExample());
        Console.WriteLine(GetRangeSelectionExample());
        Console.WriteLine(GetMultipleSelectionExample());
        Console.WriteLine();

        Console.Write("> ");
        string? selection = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(selection))
        {
            WriteError(TextService.GetSelectionRequiredMessage());
            Pause();
            return;
        }

        try
        {
            var selectedJobNumbers = ArgumentParser.ParseJobSelection(selection);
            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            _stateService.SynchronizeConfiguredJobs(jobs);

            var selectedJobs = new List<SelectedBackupJob>();
            foreach (int jobNumber in selectedJobNumbers)
            {
                if (jobNumber >= 1 && jobNumber <= jobs.Count)
                {
                    selectedJobs.Add(new SelectedBackupJob
                    {
                        JobNumber = jobNumber,
                        Job = jobs[jobNumber - 1]
                    });
                }
            }

            if (selectedJobs.Count == 0)
            {
                WriteError(GetNoValidJobsSelectedMessage());
                Pause();
                return;
            }

            Console.WriteLine();
            Console.WriteLine(GetRunningJobsMessage(selectedJobs.Count));
            Console.WriteLine();

            IReadOnlyList<BackupResult> results = BackupController.StartBackups(selectedJobs);

            for (int index = 0; index < results.Count; index++)
            {
                BackupResult result = results[index];
                bool showHeader = results.Count > 1;

                if (result.Status == BackupExecutionStatus.Finished)
                {
                    WriteSuccess(BuildBackupSuccessMessage(result, showHeader));
                }
                else
                {
                    WriteError(BuildBackupErrorMessage(result, showHeader));
                }

                if (index < results.Count - 1)
                {
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            WriteError(BuildErrorMessage(ex.Message));
        }

        Console.WriteLine();
        Pause();
    }

    private void ChangeLanguage()
    {
        Console.Clear();
        WriteSectionHeader(IsFrench() ? "Changer la langue" : "Change language");
        Console.WriteLine(GetCurrentLanguageLine());
        Console.WriteLine();
        Console.WriteLine(GetLanguageOptionLabel(1, "English"));
        Console.WriteLine(GetLanguageOptionLabel(2, "Francais"));
        Console.WriteLine(GetLanguageOptionLabel(3, IsFrench() ? "Retour" : "Back"));
        Console.WriteLine();
        Console.Write(GetLanguageSelectionPrompt());

        string? choice = Console.ReadLine();
        string normalizedChoice = choice?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalizedChoice is "3" or "back" or "retour")
        {
            return;
        }

        string? languageCode = normalizedChoice switch
        {
            "1" or "en" or "english" or "anglais" => ApplicationTextService.EnglishLanguageCode,
            "2" or "fr" or "french" or "francais" => ApplicationTextService.FrenchLanguageCode,
            _ => null
        };

        if (languageCode == null)
        {
            WriteError(GetInvalidLanguageSelectionMessage());
            Pause();
            return;
        }

        RuntimeStoragePaths.SetLanguageCode(languageCode);
        _runtime = _runtimeFactory(languageCode);

        WriteSuccess(TextService.GetLanguageUpdatedMessage());
        Pause();
    }

    private int GetJobNumber()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();

        Console.WriteLine(GetAvailableJobsLine(jobs.Count));
        Console.Write(GetJobNumberPrompt());

        string? input = Console.ReadLine();

        if (int.TryParse(input, out int jobNumber) && jobNumber >= 1 && jobNumber <= jobs.Count)
        {
            return jobNumber;
        }

        WriteError(GetInvalidJobNumberSelectionMessage(jobs.Count));
        Pause();

        return -1;
    }

    private void RenderConfigurationSuccess(int jobNumber, BackupJob updatedJob, JobPathField pathField)
    {
        string fieldName = GetConfigurationFieldName(pathField);
        string pathValue = pathField == JobPathField.Source ? updatedJob.Source : updatedJob.Target;

        WriteSuccess(GetConfigurationSuccessMessage(jobNumber, fieldName, pathValue));
        Console.WriteLine();
        Console.WriteLine(TextService.GetConfiguredJobsHeader());
        RenderJob(jobNumber, updatedJob);
    }

    private void RenderJobs(IReadOnlyList<BackupJob> jobs)
    {
        Console.WriteLine(TextService.GetConfiguredJobsHeader());
        Console.WriteLine();

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            RenderJob(index + 1, job);
        }
    }

    private void RenderJob(int jobNumber, BackupJob job)
    {
        Console.WriteLine(TextService.GetJobSummaryLine(jobNumber, job));
        Console.WriteLine($"{GetSourceLabel()}{FormatPath(job.Source)}");
        Console.WriteLine($"{GetTargetLabel()}{FormatPath(job.Target)}");
        Console.WriteLine(TextService.GetJobTypeLine(job.Type));
        Console.WriteLine(TextService.GetJobConfigurationStatusLine(job));
        Console.WriteLine();
    }

    private string BuildBackupSuccessMessage(BackupResult result, bool showHeader)
    {
        string details = TextService.FormatBackupResult(result);
        string successMessage = TextService.GetBackupSuccessMessage();

        if (!showHeader)
        {
            return $"{details}\n{successMessage}";
        }

        return $"{GetJobHeader(result)}\n{details}\n{successMessage}";
    }

    private string BuildBackupErrorMessage(BackupResult result, bool showHeader)
    {
        string details = TextService.FormatBackupResult(result);
        return showHeader
            ? $"{GetJobHeader(result)}\n{details}"
            : details;
    }

    private string GetJobHeader(BackupResult result)
    {
        return IsFrench()
            ? $"Tache {result.JobNumber} : {result.BackupName}"
            : $"Job {result.JobNumber}: {result.BackupName}";
    }

    private string GetConfigurationFieldName(JobPathField pathField)
    {
        if (pathField == JobPathField.Source)
        {
            return IsFrench() ? "source" : "source";
        }

        return IsFrench() ? "cible" : "target";
    }

    private string GetConfigurationSuccessMessage(int jobNumber, string fieldName, string pathValue)
    {
        return IsFrench()
            ? $"La tache {jobNumber} a ete mise a jour : {fieldName} = {FormatPath(pathValue)}"
            : $"Job {jobNumber} was updated: {fieldName} = {FormatPath(pathValue)}";
    }

    private string GetSourceLabel()
    {
        return IsFrench() ? "  Source : " : "  Source: ";
    }

    private string GetTargetLabel()
    {
        return IsFrench() ? "  Cible : " : "  Target: ";
    }

    private string GetMainMenuTitle()
    {
        return IsFrench() ? "Menu principal" : "Main menu";
    }

    private string GetCurrentLanguageLine()
    {
        return IsFrench()
            ? $"Langue actuelle : {GetCurrentLanguageDisplayName()}"
            : $"Current language: {GetCurrentLanguageDisplayName()}";
    }

    private string GetCurrentLanguageDisplayName()
    {
        string languageCode = TextService.GetLanguageCode();

        return languageCode == ApplicationTextService.FrenchLanguageCode
            ? (IsFrench() ? "francais" : "French")
            : (IsFrench() ? "anglais" : "English");
    }

    private string GetMenuOptionLabel(int optionNumber, string label)
    {
        return $"{optionNumber}. {label}";
    }

    private string GetLanguageOptionLabel(int optionNumber, string label)
    {
        return $"{optionNumber}. {label}";
    }

    private string GetSourcePathPrompt()
    {
        return IsFrench() ? "Entrez le chemin source : " : "Enter source path: ";
    }

    private string GetTargetPathPrompt()
    {
        return IsFrench() ? "Entrez le chemin cible : " : "Enter target path: ";
    }

    private string GetSelectionInstructionsTitle()
    {
        return IsFrench() ? "Entrez la selection des taches :" : "Enter job selection:";
    }

    private string GetSingleSelectionExample()
    {
        return IsFrench() ? "  - Tache unique : 1" : "  - Single job: 1";
    }

    private string GetRangeSelectionExample()
    {
        return IsFrench() ? "  - Plage : 1-3" : "  - Range: 1-3";
    }

    private string GetMultipleSelectionExample()
    {
        return IsFrench() ? "  - Multiple : 1;3;5" : "  - Multiple: 1;3;5";
    }

    private string GetRunningJobsMessage(int jobCount)
    {
        return IsFrench()
            ? $"Execution de {jobCount} tache(s) de sauvegarde..."
            : $"Running {jobCount} backup job(s)...";
    }

    private string GetNoValidJobsSelectedMessage()
    {
        return IsFrench() ? "Aucune tache valide selectionnee !" : "No valid jobs selected!";
    }

    private string GetAvailableJobsLine(int jobCount)
    {
        return IsFrench()
            ? $"Taches disponibles : 1-{jobCount}"
            : $"Available jobs: 1-{jobCount}";
    }

    private string GetJobNumberPrompt()
    {
        return IsFrench() ? "Entrez le numero de tache : " : "Enter job number: ";
    }

    private string GetInvalidJobNumberSelectionMessage(int jobCount)
    {
        return IsFrench()
            ? $"Numero de tache invalide. Utilisez une valeur entre 1 et {jobCount}."
            : $"Invalid job number. Use a value between 1 and {jobCount}.";
    }

    private string GetInvalidMenuChoiceMessage()
    {
        return IsFrench() ? "Choix invalide dans le menu." : "Invalid menu choice.";
    }

    private string GetInvalidLanguageSelectionMessage()
    {
        return IsFrench() ? "Choix de langue invalide." : "Invalid language selection.";
    }

    private string GetLanguageSelectionPrompt()
    {
        return IsFrench() ? "Choisissez une langue : " : "Choose a language: ";
    }

    private string GetPauseMessage()
    {
        return IsFrench()
            ? "Appuyez sur une touche pour continuer..."
            : "Press any key to continue...";
    }

    private string BuildErrorMessage(string details)
    {
        return IsFrench() ? $"Erreur : {details}" : $"Error: {details}";
    }

    private bool IsFrench()
    {
        return string.Equals(
            TextService.GetLanguageCode(),
            ApplicationTextService.FrenchLanguageCode,
            StringComparison.OrdinalIgnoreCase);
    }

    private string FormatPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? GetNotConfiguredLabel()
            : $"<{path}>";
    }

    private string GetNotConfiguredLabel()
    {
        return IsFrench() ? "<non configure>" : "<not configured>";
    }

    private void Pause()
    {
        Console.WriteLine(GetPauseMessage());
        Console.ReadKey();
    }

    private static void WriteSuccess(string message)
    {
        WriteColored(message, ConsoleColor.Green);
    }

    private static void WriteError(string message)
    {
        WriteColored(message, ConsoleColor.Red);
    }

    private static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void WriteSectionHeader(string title)
    {
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine();
    }

    private void WriteMenuBorder()
    {
        Console.WriteLine($"+{new string('-', MenuInnerWidth + 2)}+");
    }

    private void WriteMenuLine(string content)
    {
        string paddedContent = content.Length > MenuInnerWidth
            ? content[..MenuInnerWidth]
            : content.PadRight(MenuInnerWidth);

        Console.WriteLine($"| {paddedContent} |");
    }
}
