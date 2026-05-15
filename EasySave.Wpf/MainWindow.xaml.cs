using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using System.Text.Json.Serialization;

namespace EasySave.Wpf;

public partial class MainWindow : Window
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private BackupController _backupController;
    private readonly List<JobRow> _jobRows;
    private readonly IBackupExecutionController _executionController;
    private readonly IBackupExecutionCoordinator _executionCoordinator;
    private readonly DispatcherTimer _refreshTimer;
    private ApplicationTextService _textService;
    private bool _isBusy;
    private bool _isApplyingLanguage;
    private bool _isApplyingLogFormat;
    private bool _isApplyingTheme;
    private List<BackupTypeOption> _backupTypeOptions = new();
    private List<ThemeOption> _themeOptions = new();
    private List<string> _blockedProcessNames = new();
    private DashboardSection _activeSection = DashboardSection.Overview;
    private bool _isAddingNewJob;

    public MainWindow()
    {
        App.ApplyConfiguredTheme();
        InitializeComponent();
        _jobRegistry = new BackupJobRegistry();
        _stateService = new StateService();
        _executionController = new InMemoryBackupExecutionController();
        _executionCoordinator = new PriorityTransferCoordinator();

        _textService = ApplicationTextService.Create();
        _backupController = CreateBackupController();

        _jobRows = new List<JobRow>();
        ConfigureThemeSelector();
        ConfigureLanguageSelector();
        ConfigureLogFormatSelector();
        ApplyTexts();
        LoadRuntimeRulesIntoForm();
        LoadEncryptionSettingsIntoForm();
        RefreshBlockedProcesses();
        LoadJobsIntoGrid();
        SetActiveSection(DashboardSection.Overview);
        RefreshStateAndLog();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += (_, _) => RefreshStateAndLog();
        _refreshTimer.Start();
    }

    private BackupController CreateBackupController()
    {
        var backupService = new BackupService(
            new LoggerService(),
            _stateService,
            new BackupHistoryService(),
            _textService,
            new CryptoSoftService(),
            new BusinessSoftwareMonitor(),
            _executionController,
            _executionCoordinator);
        return new BackupController(backupService);
    }

    private void LoadJobsIntoGrid()
    {
        _jobRows.Clear();
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();

        for (int index = 0; index < jobs.Count; index++)
        {
            BackupJob job = jobs[index];
            _jobRows.Add(new JobRow
            {
                IsSelected = false,
                JobNumber = index + 1,
                Name = job.Name,
                Type = _textService.GetBackupTypeDisplayName(job.Type),
                Source = job.Source,
                Target = job.Target,
                ConfigurationStatus = GetConfigurationStatus(job.Source, job.Target),
                RuntimeStatus = TranslateRuntimeStatus(BackupExecutionStatus.Inactive, BackupPauseReason.None),
                ProgressPercentage = 0,
                CurrentFile = string.Empty,
                TransferMode = UiText("Idle", "En attente")
            });
        }

        JobsDataGrid.ItemsSource = null;
        JobsDataGrid.ItemsSource = _jobRows;
        OverviewJobsDataGrid.ItemsSource = null;
        OverviewJobsDataGrid.ItemsSource = _jobRows;
        ExecutionJobsDataGrid.ItemsSource = null;
        ExecutionJobsDataGrid.ItemsSource = _jobRows;
        _stateService.SynchronizeConfiguredJobs(jobs);
        ApplyRuntimeStateToRows();
        UpdateDashboardMetrics();
    }

    private async void RunSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        List<JobRow> selectedRows = _jobRows.Where(row => row.IsSelected).ToList();
        if (selectedRows.Count == 0)
        {
            StatusTextBlock.Text = Text("Wpf.NoCheckedJobStatus");
            return;
        }

        try
        {
            SetBusy(true, Text("Wpf.RunningSelectedStatus"));
            SaveAllRowsToRegistry();

            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            List<SelectedBackupJob> selectedJobs = selectedRows
                .Select(row => new SelectedBackupJob
                {
                    JobNumber = row.JobNumber,
                    Job = jobs[row.JobNumber - 1]
                })
                .ToList();

            IReadOnlyList<BackupResult> results = await Task.Run(() => _backupController.StartBackups(selectedJobs));
            int successCount = results.Count(result => result.Status == BackupExecutionStatus.Finished);
            int errorCount = results.Count - successCount;
            StatusTextBlock.Text = Format("Wpf.ExecutionCompleteStatus", successCount, errorCount);
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = Format("Wpf.ErrorStatus", exception.Message);
        }
        finally
        {
            SetBusy(false, StatusTextBlock.Text);
            RefreshStateAndLog();
        }
    }

    private async void RunAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        try
        {
            SetBusy(true, Text("Wpf.RunningAllStatus"));
            SaveAllRowsToRegistry();

            IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
            var all = jobs.Select((job, index) => new SelectedBackupJob
            {
                JobNumber = index + 1,
                Job = job
            }).ToList();

            IReadOnlyList<BackupResult> results = await Task.Run(() => _backupController.StartBackups(all));
            int successCount = results.Count(result => result.Status == BackupExecutionStatus.Finished);
            int errorCount = results.Count - successCount;
            StatusTextBlock.Text = Format("Wpf.ExecutionCompleteStatus", successCount, errorCount);
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = Format("Wpf.ErrorStatus", exception.Message);
        }
        finally
        {
            SetBusy(false, StatusTextBlock.Text);
            RefreshStateAndLog();
        }
    }

    private void SaveSelectedJobButton_Click(object sender, RoutedEventArgs e)
    {
        string jobName = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(jobName))
        {
            StatusTextBlock.Text = UiText("Please enter a job name.", "Veuillez saisir un nom de job.");
            return;
        }

        string jobSource = SourceTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(jobSource))
        {
            StatusTextBlock.Text = UiText("Please enter a source path.", "Veuillez saisir un chemin source.");
            return;
        }

        string jobTarget = TargetTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(jobTarget))
        {
            StatusTextBlock.Text = UiText("Please enter a target path.", "Veuillez saisir un chemin cible.");
            return;
        }

        if (TypeComboBox.SelectedValue is not BackupType selectedType)
        {
            StatusTextBlock.Text = Text("Wpf.SelectTypeFirstStatus");
            return;
        }

        if (!Directory.Exists(jobSource))
        {
            StatusTextBlock.Text = UiText($"Source path does not exist: {jobSource}", $"Le chemin source n'existe pas : {jobSource}");
            return;
        }

        if (!Directory.Exists(jobTarget))
        {
            StatusTextBlock.Text = UiText($"Target path does not exist: {jobTarget}", $"Le chemin cible n'existe pas : {jobTarget}");
            return;
        }

        if (string.Equals(Path.GetFullPath(jobSource), Path.GetFullPath(jobTarget), StringComparison.OrdinalIgnoreCase))
        {
            StatusTextBlock.Text = UiText("Source and target paths must be different.", "La source et la cible ne peuvent pas être identiques.");
            return;
        }

        if (_isAddingNewJob)
        {
            _isAddingNewJob = false;
            int jobNumber = _jobRegistry.LoadJobs().Count + 1;
            _jobRegistry.CreateJob(jobNumber, new BackupJob
            {
                Name = jobName,
                Source = jobSource,
                Target = jobTarget,
                Type = selectedType
            });
            _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
            LoadJobsIntoGrid();
            JobsDataGrid.SelectedIndex = _jobRows.Count - 1;
            EditJobOverlay.Visibility = Visibility.Collapsed;
            StatusTextBlock.Text = Format("Wpf.JobAddedStatus", jobNumber);
            SetActiveSection(DashboardSection.Tasks);
            RefreshStateAndLog();
            return;
        }

        if (JobsDataGrid.SelectedItem is not JobRow selectedRow)
        {
            StatusTextBlock.Text = Text("Wpf.SelectJobFirstStatus");
            return;
        }

        selectedRow.Name = jobName;
        selectedRow.Source = jobSource;
        selectedRow.Target = jobTarget;
        selectedRow.Type = _textService.GetBackupTypeDisplayName(selectedType);
        selectedRow.ConfigurationStatus = GetConfigurationStatus(selectedRow.Source, selectedRow.Target);
        SafeRefresh(JobsDataGrid);
        SafeRefresh(OverviewJobsDataGrid);
        SafeRefresh(ExecutionJobsDataGrid);

        _jobRegistry.UpdateJob(selectedRow.JobNumber, new BackupJob
        {
            Name = selectedRow.Name,
            Source = selectedRow.Source,
            Target = selectedRow.Target,
            Type = selectedType
        });

        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
        EditJobOverlay.Visibility = Visibility.Collapsed;
        StatusTextBlock.Text = Format("Wpf.JobUpdatedStatus", selectedRow.JobNumber);
        UpdateDashboardMetrics();
        RefreshStateAndLog();
    }

    private void SaveAllButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAllRowsToRegistry();
        StatusTextBlock.Text = Text("Wpf.TableSavedStatus");
        RefreshStateAndLog();
    }

    private void SaveAllRowsToRegistry()
    {
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        foreach (JobRow row in _jobRows.OrderBy(row => row.JobNumber))
        {
            row.Source = row.Source?.Trim() ?? string.Empty;
            row.Target = row.Target?.Trim() ?? string.Empty;
            row.ConfigurationStatus = GetConfigurationStatus(row.Source, row.Target);
            BackupType currentType = jobs[row.JobNumber - 1].Type;
            _jobRegistry.UpdateJob(row.JobNumber, new BackupJob
            {
                Name = row.Name,
                Source = row.Source,
                Target = row.Target,
                Type = currentType
            });
        }

        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
        SafeRefresh(JobsDataGrid);
        SafeRefresh(OverviewJobsDataGrid);
        SafeRefresh(ExecutionJobsDataGrid);
        UpdateDashboardMetrics();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        LoadJobsIntoGrid();
        RefreshStateAndLog();
        StatusTextBlock.Text = Text("Wpf.RefreshedStatus");
    }

    private void RefreshStateAndLog()
    {
        StateTextBox.Text = ReadFileSafely(RuntimeStoragePaths.StateFilePath);
        string todayLogPath = RuntimeStoragePaths.GetDailyLogFilePath(DateTime.Now);
        LogTextBox.Text = ReadFileSafely(todayLogPath);
        ApplyRuntimeStateToRows();
    }

    private string ReadFileSafely(string path)
    {
        if (!File.Exists(path))
        {
            return Format("Wpf.FileNotFound", path);
        }

        string content;
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
            content = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(content))
        {
            return Text("Wpf.EmptyFile");
        }

        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return TryFormatJson(content);
        }

        return content;
    }

    private static string TryFormatJson(string content)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return content;
        }
    }

    private void JobsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateDashboardMetrics();
    }

    private void SetBusy(bool busy, string message)
    {
        _isBusy = busy;
        RefreshButton.IsEnabled = !busy;
        RunSelectedButton.IsEnabled = !busy;
        RunAllButton.IsEnabled = !busy;
        SaveAllButton.IsEnabled = !busy;
        SaveEncryptionSettingsButton.IsEnabled = !busy;
        SaveRuntimeRulesButton.IsEnabled = !busy;
        AddJobButton.IsEnabled = !busy;
        DeleteJobButton.IsEnabled = !busy;
        JobsDataGrid.IsEnabled = !busy;
        PauseSelectedButton.IsEnabled = true;
        ResumeSelectedButton.IsEnabled = true;
        StopSelectedButton.IsEnabled = true;
        ExecutionJobsDataGrid.IsEnabled = true;
        StatusTextBlock.Text = message;
    }

    private void ConfigureLanguageSelector()
    {
        _isApplyingLanguage = true;
        LanguageComboBox.ItemsSource = new[]
        {
            new LanguageOption(ApplicationTextService.EnglishLanguageCode, "English"),
            new LanguageOption(ApplicationTextService.FrenchLanguageCode, "Français")
        };
        LanguageComboBox.DisplayMemberPath = nameof(LanguageOption.DisplayName);
        LanguageComboBox.SelectedValuePath = nameof(LanguageOption.LanguageCode);
        LanguageComboBox.SelectedValue = _textService.GetLanguageCode();
        _isApplyingLanguage = false;
    }

    private void ConfigureLogFormatSelector()
    {
        _isApplyingLogFormat = true;
        LogFormatComboBox.ItemsSource = new[]
        {
            new LogFormatOption(RuntimeStoragePaths.JsonLogFileFormat, "JSON"),
            new LogFormatOption(RuntimeStoragePaths.XmlLogFileFormat, "XML")
        };
        LogFormatComboBox.DisplayMemberPath = nameof(LogFormatOption.DisplayName);
        LogFormatComboBox.SelectedValuePath = nameof(LogFormatOption.Value);
        LogFormatComboBox.SelectedValue = RuntimeStoragePaths.GetLogFileFormat();
        _isApplyingLogFormat = false;
    }

    private void ConfigureThemeSelector()
    {
        _isApplyingTheme = true;
        _themeOptions =
        [
            new ThemeOption(RuntimeStoragePaths.SystemThemeMode, Text("Wpf.ThemeSystem")),
            new ThemeOption(RuntimeStoragePaths.LightThemeMode, Text("Wpf.ThemeLight")),
            new ThemeOption(RuntimeStoragePaths.DarkThemeMode, Text("Wpf.ThemeDark"))
        ];
        ThemeComboBox.ItemsSource = _themeOptions;
        ThemeComboBox.DisplayMemberPath = nameof(ThemeOption.DisplayName);
        ThemeComboBox.SelectedValuePath = nameof(ThemeOption.Value);
        ThemeComboBox.SelectedValue = RuntimeStoragePaths.GetThemeMode();
        _isApplyingTheme = false;
    }

    private void ApplyTexts()
    {
        Title = Text("Wpf.WindowTitle");
        HeadingTextBlock.Text = Text("Wpf.Heading");
        HeroDescriptionTextBlock.Text = Text("Wpf.HeroDescription");
        SetButtonContent(OverviewNavButton, "\uE80F", Text("Wpf.NavOverview"));
        SetButtonContent(TasksNavButton, "\uE8FD", Text("Wpf.NavTasks"));
        SetButtonContent(ExecutionNavButton, "\uE768", Text("Wpf.NavExecution"));
        SetButtonContent(StateLogsNavButton, "\uE9D2", Text("Wpf.NavStateLogs"));
        SetButtonContent(SettingsNavButton, "\uE713", Text("Wpf.NavSettings"));
        PageSubtitleTextBlock.Text = Text("Wpf.SubtitleOverview");
        ConfiguredJobsGroupBox.Header = Text("Wpf.ConfiguredJobsHeader");
        OverviewJobsGroupBox.Header = Text("Wpf.OverviewJobsHeader");
        ExecutionJobsGroupBox.Header = Text("Wpf.ExecutionJobsHeader");
        RunColumn.Header = Text("Wpf.RunColumnHeader");
        NumberColumn.Header = Text("Wpf.NumberColumnHeader");
        NameColumn.Header = Text("Wpf.NameColumnHeader");
        TypeColumn.Header = Text("Wpf.TypeColumnHeader");
        SourceColumn.Header = Text("Wpf.SourceColumnHeader");
        TargetColumn.Header = Text("Wpf.TargetColumnHeader");
        NameLabel.Text = UiText("Name", "Nom");
        SourceLabel.Text = Text("Wpf.SourceColumnHeader");
        TargetLabel.Text = Text("Wpf.TargetColumnHeader");
        TypeLabel.Text = Text("Wpf.TypeColumnHeader");
        EncryptedExtensionsLabel.Text = Text("Wpf.EncryptedExtensionsLabel");
        CryptoSoftKeyLabel.Text = Text("Wpf.CryptoSoftKeyLabel");
        SetButtonContent(EditJobButton, "", UiText("Edit selected", "Modifier selection"));
        SetButtonContent(CancelEditButton, "", UiText("Cancel", "Annuler"));
        StateGroupBox.Header = Text("Wpf.StateHeader");
        LogGroupBox.Header = Text("Wpf.LogHeader");
        SettingsTitleTextBlock.Text = Text("Wpf.SettingsHeader");
        AppearanceSectionTitle.Text = Text("Wpf.AppearanceSectionTitle");
        ThemeLabel.Text = Text("Wpf.ThemeLabel");
        LanguageSectionTitle.Text = Text("Wpf.LanguageSectionTitle");
        LogFormatSectionTitle.Text = Text("Wpf.LogFormatSectionTitle");
        EncryptionSectionTitle.Text = Text("Wpf.EncryptionSectionTitle");
        RuntimeRulesSectionTitle.Text = UiText("Runtime scheduling", "Ordonnancement runtime");
        PriorityExtensionsLabel.Text = UiText("Priority extensions (; or , separated)", "Extensions prioritaires (separees par ; ou ,)");
        LargeFileThresholdLabel.Text = UiText("Large file threshold (KB)", "Seuil gros fichiers (Ko)");
        MaxConcurrencyLabel.Text = UiText("Max concurrent jobs", "Nombre max de jobs concurrents");
        BusinessSoftwareSectionTitle.Text = Text("Wpf.BusinessSoftwareSectionTitle");
        LanguageLabel.Text = Text("Wpf.LanguageLabel");
        LogFormatLabel.Text = Text("Wpf.LogFormatLabel");
        BlockedProcessesLabel.Text = Text("Wpf.BlockedProcessesLabel");
        ProcessNameLabel.Text = Text("Wpf.ProcessNameLabel");
        SetButtonContent(AddProcessButton, "\uE710", Text("Wpf.AddBlockedProcessButton"));
        SetButtonContent(RemoveProcessButton, "\uE74D", Text("Wpf.RemoveBlockedProcessButton"));
        SetButtonContent(AddJobButton, "\uE710", Text("Wpf.AddJobButton"));
        SetButtonContent(DeleteJobButton, "\uE74D", Text("Wpf.DeleteJobButton"));
        SetButtonContent(RefreshButton, "\uE72C", Text("Wpf.RefreshButton"));
        SetButtonContent(RunSelectedButton, "\uE768", Text("Wpf.RunSelectedButton"));
        SetButtonContent(RunAllButton, "\uE102", Text("Wpf.RunAllButton"));
        SetButtonContent(PauseSelectedButton, "\uE769", UiText("Pause selected", "Pause selection"));
        SetButtonContent(ResumeSelectedButton, "\uE768", UiText("Resume selected", "Reprendre selection"));
        SetButtonContent(StopSelectedButton, "\uE71A", UiText("Stop selected", "Arreter selection"));
        SetButtonContent(SaveAllButton, "\uE74E", Text("Wpf.SaveAllButton"));
        SetButtonContent(SaveSelectedJobButton, "\uE74E", Text("Wpf.SaveSelectedJobButton"));
        SetButtonContent(SaveEncryptionSettingsButton, "\uE74E", Text("Wpf.SaveEncryptionSettingsButton"));
        SetButtonContent(SaveRuntimeRulesButton, "\uE74E", UiText("Save runtime rules", "Enregistrer regles runtime"));
        KpiTotalJobsLabelTextBlock.Text = Text("Wpf.KpiTotalJobs");
        KpiConfiguredJobsLabelTextBlock.Text = Text("Wpf.KpiConfiguredJobs");
        KpiSelectedJobsLabelTextBlock.Text = Text("Wpf.KpiSelectedJobs");
        KpiStorageLabelTextBlock.Text = Text("Wpf.KpiStorage");
        ConfigureTypeSelector();
        ConfigureThemeSelector();

        if (string.IsNullOrWhiteSpace(StatusTextBlock.Text))
        {
            StatusTextBlock.Text = Text("Wpf.ReadyStatus");
        }

        UpdateSelectedJobLabel();
        UpdateDashboardMetrics();
        UpdateNavigationTexts();
    }

    private void ConfigureTypeSelector()
    {
        _backupTypeOptions =
        [
            new BackupTypeOption(BackupType.Full, _textService.GetBackupTypeDisplayName(BackupType.Full)),
            new BackupTypeOption(BackupType.Differential, _textService.GetBackupTypeDisplayName(BackupType.Differential))
        ];
        TypeComboBox.ItemsSource = _backupTypeOptions;
        TypeComboBox.DisplayMemberPath = nameof(BackupTypeOption.DisplayName);
        TypeComboBox.SelectedValuePath = nameof(BackupTypeOption.Value);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLanguage || LanguageComboBox.SelectedValue is not string languageCode)
        {
            return;
        }

        RuntimeStoragePaths.SetLanguageCode(languageCode);
        _textService = ApplicationTextService.Create(languageCode);
        _backupController = CreateBackupController();
        ConfigureLogFormatSelector();
        LoadJobsIntoGrid();
        RefreshBlockedProcesses();
        ApplyTexts();
        LoadEncryptionSettingsIntoForm();
        StatusTextBlock.Text = _textService.GetLanguageUpdatedMessage();
        RefreshStateAndLog();
    }

    private void LogFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLogFormat || LogFormatComboBox.SelectedValue is not string logFormat)
        {
            return;
        }

        RuntimeStoragePaths.SetLogFileFormat(logFormat);
        ConfigureLogFormatSelector();
        StatusTextBlock.Text = Format("Wpf.LogFormatUpdatedStatus", _textService.GetLogFileFormatDisplayName(logFormat));
        RefreshStateAndLog();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingTheme || ThemeComboBox.SelectedValue is not string themeMode)
        {
            return;
        }

        RuntimeStoragePaths.SetThemeMode(themeMode);
        App.ApplyTheme(themeMode);
        ConfigureThemeSelector();
        StatusTextBlock.Text = Format("Wpf.ThemeUpdatedStatus", GetThemeDisplayName(themeMode));
    }

    private void AddProcessButton_Click(object sender, RoutedEventArgs e)
    {
        string processNameInput = ProcessNameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(processNameInput))
        {
            StatusTextBlock.Text = Text("Wpf.ProcessNameRequiredStatus");
            return;
        }

        string normalizedProcessName = NormalizeProcessName(processNameInput);
        if (_blockedProcessNames.Any(name => string.Equals(name, normalizedProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusTextBlock.Text = Text("Wpf.ProcessAlreadyConfiguredStatus");
            return;
        }

        RuntimeStoragePaths.SetBlockedProcessNames(_blockedProcessNames.Append(normalizedProcessName));
        ProcessNameTextBox.Text = string.Empty;
        RefreshBlockedProcesses();
        StatusTextBlock.Text = Format("Wpf.ProcessAddedStatus", $"{normalizedProcessName}.exe");
    }

    private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (BlockedProcessesListBox.SelectedItem is not string selectedProcessLabel)
        {
            StatusTextBlock.Text = Text("Wpf.SelectProcessFirstStatus");
            return;
        }

        string normalizedProcessName = NormalizeProcessName(selectedProcessLabel);
        RuntimeStoragePaths.SetBlockedProcessNames(
            _blockedProcessNames.Where(process => !string.Equals(process, normalizedProcessName, StringComparison.OrdinalIgnoreCase)));
        RefreshBlockedProcesses();
        StatusTextBlock.Text = Format("Wpf.ProcessRemovedStatus", $"{normalizedProcessName}.exe");
    }

    private void UpdateSelectedJobLabel()
    {
        if (JobsDataGrid.SelectedItem is JobRow selectedRow)
        {
            SelectedJobLabel.Text = Format("Wpf.SelectedJobLabel", selectedRow.JobNumber, selectedRow.Name, selectedRow.Type);
            return;
        }

        SelectedJobLabel.Text = Text("Wpf.NoSelectedJob");
    }

    private string Text(string key) => _textService.GetText(key);

    private string Format(string key, params object[] args) => _textService.FormatText(key, args);

    private void LoadEncryptionSettingsIntoForm()
    {
        EncryptedExtensionsTextBox.Text = string.Join("; ", RuntimeStoragePaths.GetEncryptedExtensions());
        CryptoSoftKeyTextBox.Text = RuntimeStoragePaths.GetCryptoSoftKey();
    }

    private void LoadRuntimeRulesIntoForm()
    {
        PriorityExtensionsTextBox.Text = string.Join("; ", RuntimeStoragePaths.GetPriorityExtensions());
        LargeFileThresholdTextBox.Text = RuntimeStoragePaths.GetLargeFileThresholdKb().ToString();
        MaxConcurrencyTextBox.Text = RuntimeStoragePaths.GetMaxConcurrentJobs().ToString();
    }

    private void SaveEncryptionSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimeStoragePaths.SetEncryptedExtensions([EncryptedExtensionsTextBox.Text]);
        RuntimeStoragePaths.SetCryptoSoftKey(CryptoSoftKeyTextBox.Text);
        LoadEncryptionSettingsIntoForm();
        StatusTextBlock.Text = Text("Wpf.EncryptionSettingsSavedStatus");
        RefreshStateAndLog();
    }

    private void SaveRuntimeRulesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(LargeFileThresholdTextBox.Text.Trim(), out int thresholdKb) || thresholdKb < 0)
        {
            StatusTextBlock.Text = UiText("Large file threshold must be a number >= 0.", "Le seuil gros fichiers doit etre un nombre >= 0.");
            return;
        }

        if (!int.TryParse(MaxConcurrencyTextBox.Text.Trim(), out int maxConcurrentJobs) || maxConcurrentJobs <= 0)
        {
            StatusTextBlock.Text = UiText("Max concurrent jobs must be a number > 0.", "Le nombre max de jobs concurrents doit etre > 0.");
            return;
        }

        RuntimeStoragePaths.SetPriorityExtensions([PriorityExtensionsTextBox.Text]);
        RuntimeStoragePaths.SetLargeFileThresholdKb(thresholdKb);
        RuntimeStoragePaths.SetMaxConcurrentJobs(maxConcurrentJobs);
        LoadRuntimeRulesIntoForm();
        StatusTextBlock.Text = UiText("Runtime scheduling rules saved.", "Regles runtime enregistrees.");
    }

    private void AddJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        _isAddingNewJob = true;
        SelectedJobLabel.Text = UiText("New job", "Nouvelle tâche");
        NameTextBox.Text = string.Empty;
        SourceTextBox.Text = string.Empty;
        TargetTextBox.Text = string.Empty;
        TypeComboBox.SelectedIndex = _backupTypeOptions.Count > 0 ? 0 : -1;
        EditJobOverlay.Visibility = Visibility.Visible;
        NameTextBox.Focus();
    }

    private void EditJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (JobsDataGrid.SelectedItem is not JobRow row)
        {
            StatusTextBlock.Text = Text("Wpf.SelectJobFirstStatus");
            return;
        }
        OpenEditPopup(row);
    }

    private void EditJobInlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not JobRow row)
            return;
        JobsDataGrid.SelectedItem = row;
        OpenEditPopup(row);
    }

    private void OpenEditPopup(JobRow row)
    {
        BackupJob job = _jobRegistry.LoadJobs()[row.JobNumber - 1];
        SelectedJobLabel.Text = Format("Wpf.SelectedJobLabel", row.JobNumber, row.Name, row.Type);
        NameTextBox.Text = row.Name;
        SourceTextBox.Text = row.Source;
        TargetTextBox.Text = row.Target;
        TypeComboBox.SelectedValue = job.Type;
        EditJobOverlay.Visibility = Visibility.Visible;
        NameTextBox.Focus();
    }

    private void CloseEditPopup_Click(object sender, RoutedEventArgs e)
    {
        _isAddingNewJob = false;
        EditJobOverlay.Visibility = Visibility.Collapsed;
    }

    private void DeleteJobInlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;
        if (((Button)sender).Tag is not JobRow row)
            return;

        _jobRegistry.DeleteJob(row.JobNumber);
        LoadJobsIntoGrid();
        if (_jobRows.Count > 0)
            JobsDataGrid.SelectedIndex = Math.Min(row.JobNumber - 1, _jobRows.Count - 1);
        StatusTextBlock.Text = Format("Wpf.JobDeletedStatus", row.JobNumber);
        RefreshStateAndLog();
    }

    private void DeleteJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        if (JobsDataGrid.SelectedItem is not JobRow selectedRow)
        {
            StatusTextBlock.Text = Text("Wpf.SelectJobFirstStatus");
            return;
        }

        _jobRegistry.DeleteJob(selectedRow.JobNumber);
        LoadJobsIntoGrid();
        if (_jobRows.Count > 0)
        {
            JobsDataGrid.SelectedIndex = Math.Min(selectedRow.JobNumber - 1, _jobRows.Count - 1);
        }
        StatusTextBlock.Text = Format("Wpf.JobDeletedStatus", selectedRow.JobNumber);
        RefreshStateAndLog();
    }

    private void PauseSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (JobRow row in GetCheckedRows())
        {
            _backupController.PauseJob(row.JobNumber);
        }

        StatusTextBlock.Text = UiText("Pause requested for selected jobs.", "Pause demandee pour les jobs selectionnes.");
    }

    private void ResumeSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (JobRow row in GetCheckedRows())
        {
            _backupController.ResumeJob(row.JobNumber);
        }

        StatusTextBlock.Text = UiText("Resume requested for selected jobs.", "Reprise demandee pour les jobs selectionnes.");
    }

    private void StopSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (JobRow row in GetCheckedRows())
        {
            _backupController.StopJob(row.JobNumber);
        }

        StatusTextBlock.Text = UiText("Stop requested for selected jobs.", "Arret demande pour les jobs selectionnes.");
    }

    private void OverviewNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Overview);
    }

    private void TasksNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Tasks);
    }

    private void ExecutionNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Execution);
    }

    private void StateLogsNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.StateLogs);
    }

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Settings);
    }

    private void SetActiveSection(DashboardSection section)
    {
        _activeSection = section;
        OverviewPanel.Visibility = section == DashboardSection.Overview ? Visibility.Visible : Visibility.Collapsed;
        TasksPanel.Visibility = section == DashboardSection.Tasks ? Visibility.Visible : Visibility.Collapsed;
        ExecutionPanel.Visibility = section == DashboardSection.Execution ? Visibility.Visible : Visibility.Collapsed;
        StateLogsPanel.Visibility = section == DashboardSection.StateLogs ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = section == DashboardSection.Settings ? Visibility.Visible : Visibility.Collapsed;
        UpdateNavigationTexts();
    }

    private void UpdateNavigationTexts()
    {
        PageSubtitleTextBlock.Text = _activeSection switch
        {
            DashboardSection.Overview => Text("Wpf.SubtitleOverview"),
            DashboardSection.Tasks => Text("Wpf.SubtitleTasks"),
            DashboardSection.Execution => Text("Wpf.SubtitleExecution"),
            DashboardSection.StateLogs => Text("Wpf.SubtitleStateLogs"),
            DashboardSection.Settings => Text("Wpf.SubtitleSettings"),
            _ => Text("Wpf.SubtitleOverview")
        };

        ApplyNavigationButtonStyle(OverviewNavButton, _activeSection == DashboardSection.Overview);
        ApplyNavigationButtonStyle(TasksNavButton, _activeSection == DashboardSection.Tasks);
        ApplyNavigationButtonStyle(ExecutionNavButton, _activeSection == DashboardSection.Execution);
        ApplyNavigationButtonStyle(StateLogsNavButton, _activeSection == DashboardSection.StateLogs);
        ApplyNavigationButtonStyle(SettingsNavButton, _activeSection == DashboardSection.Settings);
    }

    private static void SetButtonContent(Button button, string iconGlyph, string label)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var icon = new TextBlock
        {
            Text = iconGlyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 15,
            Margin = new Thickness(0, 0, 9, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        panel.Children.Add(icon);
        panel.Children.Add(text);
        button.Content = panel;
    }

    private void ApplyNavigationButtonStyle(Button button, bool isActive)
    {
        if (isActive)
        {
            button.SetResourceReference(Control.BackgroundProperty, "AccentSoftBrush");
            button.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
            button.SetResourceReference(Control.BorderBrushProperty, "AccentBrush");
            button.BorderThickness = new Thickness(4, 0, 0, 0);
            return;
        }

        button.Background = Brushes.Transparent;
        button.SetResourceReference(Control.ForegroundProperty, "SidebarTextBrush");
        button.BorderBrush = Brushes.Transparent;
        button.BorderThickness = new Thickness(4, 0, 0, 0);
    }

    private void UpdateDashboardMetrics()
    {
        int totalJobs = _jobRows.Count;
        int configuredJobs = _jobRows.Count(IsConfigured);
        int selectedJobs = _jobRows.Count(row => row.IsSelected);

        KpiTotalJobsValueTextBlock.Text = totalJobs.ToString();
        KpiConfiguredJobsValueTextBlock.Text = configuredJobs.ToString();
        KpiSelectedJobsValueTextBlock.Text = selectedJobs.ToString();
        KpiStorageValueTextBlock.Text = Text("Wpf.LocalStorageSummary");
        HeroStatusTextBlock.Text = Format("Wpf.HeroReadySummary", configuredJobs, totalJobs);
    }

    private static bool IsConfigured(JobRow row)
    {
        return row.IsConfigured;
    }

    private string GetConfigurationStatus(string source, string target)
    {
        return !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target)
            ? Text("Wpf.ConfiguredStatus")
            : Text("Wpf.IncompleteStatus");
    }

    private string GetThemeDisplayName(string themeMode)
    {
        return themeMode switch
        {
            RuntimeStoragePaths.LightThemeMode => Text("Wpf.ThemeLight"),
            RuntimeStoragePaths.DarkThemeMode => Text("Wpf.ThemeDark"),
            _ => Text("Wpf.ThemeSystem")
        };
    }

    private void RefreshBlockedProcesses()
    {
        _blockedProcessNames = RuntimeStoragePaths.GetBlockedProcessNames().ToList();
        BlockedProcessesListBox.ItemsSource = null;
        BlockedProcessesListBox.ItemsSource = _blockedProcessNames
            .Select(processName => $"{processName}.exe")
            .ToList();
    }

    private static string NormalizeProcessName(string processName)
    {
        string normalized = processName.Trim().ToLowerInvariant();
        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }

    private static void SafeRefresh(DataGrid grid)
    {
        // Calling Refresh() during an AddNew/EditItem transaction throws InvalidOperationException.
        // Skip and let the next timer tick retry instead of committing user's in-progress edits.
        var view = grid.Items as System.ComponentModel.IEditableCollectionView;
        if (view != null && (view.IsEditingItem || view.IsAddingNew))
            return;
        grid.Items.Refresh();
    }

    private void ApplyRuntimeStateToRows()
    {
        IReadOnlyDictionary<string, BackupState> statesByJobName = LoadStatesByJobName();

        foreach (JobRow row in _jobRows)
        {
            if (!statesByJobName.TryGetValue(row.Name, out BackupState? state))
            {
                row.RuntimeStatus = TranslateRuntimeStatus(BackupExecutionStatus.Inactive, BackupPauseReason.None);
                row.ProgressPercentage = 0;
                row.CurrentFile = string.Empty;
                row.TransferMode = UiText("Idle", "En attente");
                continue;
            }

            row.RuntimeStatus = TranslateRuntimeStatus(state.Status, state.PauseReason);
            row.ProgressPercentage = Math.Round(state.Progress, 1);
            row.CurrentFile = string.IsNullOrWhiteSpace(state.CurrentSourcePath)
                ? string.Empty
                : Path.GetFileName(state.CurrentSourcePath);
            row.TransferMode = BuildTransferMode(state);
        }

        SafeRefresh(JobsDataGrid);
        SafeRefresh(OverviewJobsDataGrid);
        SafeRefresh(ExecutionJobsDataGrid);
        UpdateBusinessSoftwareBanner(statesByJobName.Values);
    }

    private void UpdateBusinessSoftwareBanner(IEnumerable<BackupState> states)
    {
        BackupState? blocked = _isBusy
            ? states.FirstOrDefault(s => s.Status == BackupExecutionStatus.PausedByBusinessSoftware)
            : null;
        if (blocked != null)
        {
            string detail = string.IsNullOrWhiteSpace(blocked.PauseReasonDetails)
                ? UiText("a business software", "un logiciel métier")
                : blocked.PauseReasonDetails;
            BusinessSoftwareBannerText.Text = UiText(
                $"Execution paused — {detail} is running. It will resume automatically once the software is closed.",
                $"Exécution suspendue — {detail} est en cours d'exécution. Elle reprendra automatiquement à la fermeture du logiciel.");
            BusinessSoftwareBanner.Visibility = Visibility.Visible;
        }
        else
        {
            BusinessSoftwareBanner.Visibility = Visibility.Collapsed;
        }
    }

    private IReadOnlyDictionary<string, BackupState> LoadStatesByJobName()
    {
        return _stateService.ReadAllStates()
            .ToDictionary(state => state.BackupName, StringComparer.OrdinalIgnoreCase);
    }

    private string TranslateRuntimeStatus(BackupExecutionStatus status, BackupPauseReason pauseReason)
    {
        return status switch
        {
            BackupExecutionStatus.Active => UiText("Active", "Actif"),
            BackupExecutionStatus.Paused when pauseReason == BackupPauseReason.UserRequested => UiText("Paused", "En pause"),
            BackupExecutionStatus.PausedByBusinessSoftware => UiText("Paused by business software", "Pause logiciel metier"),
            BackupExecutionStatus.Stopping => UiText("Stopping", "Arret en cours"),
            BackupExecutionStatus.Stopped => UiText("Stopped", "Arrete"),
            BackupExecutionStatus.Finished => UiText("Finished", "Termine"),
            BackupExecutionStatus.Error => UiText("Error", "Erreur"),
            _ => UiText("Inactive", "Inactif")
        };
    }

    private string BuildTransferMode(BackupState state)
    {
        string priorityLabel = state.CurrentFilePriority == FileTransferPriority.Priority
            ? UiText("Priority", "Prioritaire")
            : UiText("Normal", "Normal");
        string sizeLabel = state.IsLargeFileTransfer
            ? UiText("Large file", "Gros fichier")
            : UiText("Standard", "Standard");

        if (state.IsPriorityWorkPending)
        {
            return $"{priorityLabel} | {sizeLabel} | {UiText("priority queue", "file prioritaire active")}";
        }

        return $"{priorityLabel} | {sizeLabel}";
    }

    private IReadOnlyList<JobRow> GetCheckedRows()
    {
        return _jobRows.Where(row => row.IsSelected).ToList();
    }

    private string UiText(string english, string french)
    {
        return _textService.GetLanguageCode() == ApplicationTextService.FrenchLanguageCode
            ? french
            : english;
    }

    private sealed record LanguageOption(string LanguageCode, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
    private sealed record LogFormatOption(string Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
    private sealed record BackupTypeOption(BackupType Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
    private sealed record ThemeOption(string Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private enum DashboardSection
    {
        Overview,
        Tasks,
        Execution,
        StateLogs,
        Settings
    }
}
