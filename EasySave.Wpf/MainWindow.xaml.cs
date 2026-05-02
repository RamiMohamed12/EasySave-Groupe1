using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.IO;
using System.Windows.Media;

namespace EasySave.Wpf;

public partial class MainWindow : Window
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private BackupController _backupController;
    private readonly List<JobRow> _jobRows;
    private ApplicationTextService _textService;
    private bool _isBusy;
    private bool _isApplyingLanguage;
    private bool _isApplyingLogFormat;
    private bool _isApplyingTheme;
    private List<BackupTypeOption> _backupTypeOptions = new();
    private List<ThemeOption> _themeOptions = new();
    private List<string> _blockedProcessNames = new();
    private DashboardSection _activeSection = DashboardSection.Overview;

    public MainWindow()
    {
        App.ApplyConfiguredTheme();
        InitializeComponent();
        _jobRegistry = new BackupJobRegistry();
        _stateService = new StateService();

        _textService = ApplicationTextService.Create();
        _backupController = CreateBackupController();

        _jobRows = new List<JobRow>();
        ConfigureThemeSelector();
        ConfigureLanguageSelector();
        ConfigureLogFormatSelector();
        ApplyTexts();
        LoadEncryptionSettingsIntoForm();
        RefreshBlockedProcesses();
        LoadJobsIntoGrid();
        SetActiveSection(DashboardSection.Overview);
        RefreshStateAndLog();
    }

    private BackupController CreateBackupController()
    {
        var backupService = new BackupService(
            new LoggerService(),
            _stateService,
            new BackupHistoryService(),
            _textService,
            new BusinessSoftwareMonitor());
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
                ConfigurationStatus = GetConfigurationStatus(job.Source, job.Target)
            });
        }

        JobsDataGrid.ItemsSource = null;
        JobsDataGrid.ItemsSource = _jobRows;
        OverviewJobsDataGrid.ItemsSource = null;
        OverviewJobsDataGrid.ItemsSource = _jobRows;
        ExecutionJobsDataGrid.ItemsSource = null;
        ExecutionJobsDataGrid.ItemsSource = _jobRows;
        _stateService.SynchronizeConfiguredJobs(jobs);
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
        if (JobsDataGrid.SelectedItem is not JobRow selectedRow)
        {
            StatusTextBlock.Text = Text("Wpf.SelectJobFirstStatus");
            return;
        }

        if (TypeComboBox.SelectedValue is not BackupType selectedType)
        {
            StatusTextBlock.Text = Text("Wpf.SelectTypeFirstStatus");
            return;
        }

        selectedRow.Source = SourceTextBox.Text.Trim();
        selectedRow.Target = TargetTextBox.Text.Trim();
        selectedRow.Type = _textService.GetBackupTypeDisplayName(selectedType);
        selectedRow.ConfigurationStatus = GetConfigurationStatus(selectedRow.Source, selectedRow.Target);
        JobsDataGrid.Items.Refresh();
        OverviewJobsDataGrid.Items.Refresh();
        ExecutionJobsDataGrid.Items.Refresh();

        _jobRegistry.UpdateJob(selectedRow.JobNumber, new BackupJob
        {
            Name = selectedRow.Name,
            Source = selectedRow.Source,
            Target = selectedRow.Target,
            Type = selectedType
        });

        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
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
        JobsDataGrid.Items.Refresh();
        OverviewJobsDataGrid.Items.Refresh();
        ExecutionJobsDataGrid.Items.Refresh();
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
    }

    private string ReadFileSafely(string path)
    {
        if (!File.Exists(path))
        {
            return Format("Wpf.FileNotFound", path);
        }

        string content = File.ReadAllText(path);
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
        if (JobsDataGrid.SelectedItem is not JobRow selectedRow)
        {
            SelectedJobLabel.Text = Text("Wpf.NoSelectedJob");
            SourceTextBox.Text = string.Empty;
            TargetTextBox.Text = string.Empty;
            TypeComboBox.SelectedIndex = -1;
            UpdateDashboardMetrics();
            return;
        }

        BackupJob selectedJob = _jobRegistry.LoadJobs()[selectedRow.JobNumber - 1];
        SelectedJobLabel.Text = Format("Wpf.SelectedJobLabel", selectedRow.JobNumber, selectedRow.Name, selectedRow.Type);
        SourceTextBox.Text = selectedRow.Source;
        TargetTextBox.Text = selectedRow.Target;
        TypeComboBox.SelectedValue = selectedJob.Type;
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
        AddJobButton.IsEnabled = !busy;
        DeleteJobButton.IsEnabled = !busy;
        JobsDataGrid.IsEnabled = !busy;
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
        SidebarTitleTextBlock.Text = Text("Wpf.SidebarTitle");
        OverviewNavButton.Content = Text("Wpf.NavOverview");
        TasksNavButton.Content = Text("Wpf.NavTasks");
        ExecutionNavButton.Content = Text("Wpf.NavExecution");
        StateLogsNavButton.Content = Text("Wpf.NavStateLogs");
        SettingsNavButton.Content = Text("Wpf.NavSettings");
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
        EditSelectedJobGroupBox.Header = Text("Wpf.EditSelectedJobHeader");
        SourceLabel.Text = Text("Wpf.SourceColumnHeader");
        TargetLabel.Text = Text("Wpf.TargetColumnHeader");
        TypeLabel.Text = Text("Wpf.TypeColumnHeader");
        EncryptedExtensionsLabel.Text = Text("Wpf.EncryptedExtensionsLabel");
        CryptoSoftKeyLabel.Text = Text("Wpf.CryptoSoftKeyLabel");
        SaveEncryptionSettingsButton.Content = Text("Wpf.SaveEncryptionSettingsButton");
        SaveSelectedJobButton.Content = Text("Wpf.SaveSelectedJobButton");
        EditHintTextBlock.Text = Text("Wpf.EditHint");
        StateGroupBox.Header = Text("Wpf.StateHeader");
        LogGroupBox.Header = Text("Wpf.LogHeader");
        SettingsTitleTextBlock.Text = Text("Wpf.SettingsHeader");
        AppearanceSectionTitle.Text = Text("Wpf.AppearanceSectionTitle");
        ThemeLabel.Text = Text("Wpf.ThemeLabel");
        LanguageSectionTitle.Text = Text("Wpf.LanguageSectionTitle");
        LogFormatSectionTitle.Text = Text("Wpf.LogFormatSectionTitle");
        EncryptionSectionTitle.Text = Text("Wpf.EncryptionSectionTitle");
        BusinessSoftwareSectionTitle.Text = Text("Wpf.BusinessSoftwareSectionTitle");
        LanguageLabel.Text = Text("Wpf.LanguageLabel");
        LogFormatLabel.Text = Text("Wpf.LogFormatLabel");
        BlockedProcessesLabel.Text = Text("Wpf.BlockedProcessesLabel");
        ProcessNameLabel.Text = Text("Wpf.ProcessNameLabel");
        AddProcessButton.Content = Text("Wpf.AddBlockedProcessButton");
        RemoveProcessButton.Content = Text("Wpf.RemoveBlockedProcessButton");
        AddJobButton.Content = Text("Wpf.AddJobButton");
        DeleteJobButton.Content = Text("Wpf.DeleteJobButton");
        RefreshButton.Content = Text("Wpf.RefreshButton");
        RunSelectedButton.Content = Text("Wpf.RunSelectedButton");
        RunAllButton.Content = Text("Wpf.RunAllButton");
        SaveAllButton.Content = Text("Wpf.SaveAllButton");
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

    private void SaveEncryptionSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimeStoragePaths.SetEncryptedExtensions([EncryptedExtensionsTextBox.Text]);
        RuntimeStoragePaths.SetCryptoSoftKey(CryptoSoftKeyTextBox.Text);
        LoadEncryptionSettingsIntoForm();
        StatusTextBlock.Text = Text("Wpf.EncryptionSettingsSavedStatus");
        RefreshStateAndLog();
    }

    private void AddJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        int jobNumber = jobs.Count + 1;
        BackupType selectedType = TypeComboBox.SelectedValue is BackupType type ? type : BackupType.Full;

        _jobRegistry.CreateJob(jobNumber, new BackupJob
        {
            Name = $"Job{jobNumber}",
            Source = string.Empty,
            Target = string.Empty,
            Type = selectedType
        });

        LoadJobsIntoGrid();
        JobsDataGrid.SelectedIndex = _jobRows.Count - 1;
        StatusTextBlock.Text = Format("Wpf.JobAddedStatus", jobNumber);
        SetActiveSection(DashboardSection.Tasks);
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

    private static void ApplyNavigationButtonStyle(Button button, bool isActive)
    {
        if (isActive)
        {
            button.Background = new SolidColorBrush(Color.FromArgb(120, 103, 199, 218));
            button.Foreground = Brushes.White;
            button.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 154, 198, 218));
            return;
        }

        button.Background = Brushes.Transparent;
        button.Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        button.BorderBrush = Brushes.Transparent;
    }

    private void UpdateDashboardMetrics()
    {
        int totalJobs = _jobRows.Count;
        int configuredJobs = _jobRows.Count(IsConfigured);
        int selectedJobs = _jobRows.Count(row => row.IsSelected);

        KpiTotalJobsValueTextBlock.Text = totalJobs.ToString();
        KpiConfiguredJobsValueTextBlock.Text = configuredJobs.ToString();
        KpiSelectedJobsValueTextBlock.Text = selectedJobs.ToString();
        KpiStorageValueTextBlock.Text = RuntimeStoragePaths.BackupStateDirectory;
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

    private sealed record LanguageOption(string LanguageCode, string DisplayName);
    private sealed record LogFormatOption(string Value, string DisplayName);
    private sealed record BackupTypeOption(BackupType Value, string DisplayName);
    private sealed record ThemeOption(string Value, string DisplayName);

    private enum DashboardSection
    {
        Overview,
        Tasks,
        Execution,
        StateLogs,
        Settings
    }
}
