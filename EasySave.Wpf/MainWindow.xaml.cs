using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.IO;

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
    private List<BackupTypeOption> _backupTypeOptions = new();

    public MainWindow()
    {
        InitializeComponent();
        _jobRegistry = new BackupJobRegistry();
        _stateService = new StateService();

        _textService = ApplicationTextService.Create();
        _backupController = CreateBackupController();

        _jobRows = new List<JobRow>();
        ConfigureLanguageSelector();
        ApplyTexts();
        LoadJobsIntoGrid();
        RefreshStateAndLog();
    }

    private BackupController CreateBackupController()
    {
        var backupService = new BackupService(
            new LoggerService(),
            _stateService,
            new BackupHistoryService(),
            _textService);
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
                Target = job.Target
            });
        }

        JobsDataGrid.ItemsSource = null;
        JobsDataGrid.ItemsSource = _jobRows;
        _stateService.SynchronizeConfiguredJobs(jobs);
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
        JobsDataGrid.Items.Refresh();

        _jobRegistry.UpdateJob(selectedRow.JobNumber, new BackupJob
        {
            Name = selectedRow.Name,
            Source = selectedRow.Source,
            Target = selectedRow.Target,
            Type = selectedType
        });

        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
        StatusTextBlock.Text = Format("Wpf.JobUpdatedStatus", selectedRow.JobNumber);
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
            BackupType currentType = jobs[row.JobNumber - 1].Type;
            _jobRegistry.UpdateJob(row.JobNumber, new BackupJob
            {
                Name = row.Name,
                Source = row.Source?.Trim() ?? string.Empty,
                Target = row.Target?.Trim() ?? string.Empty,
                Type = currentType
            });
        }

        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
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
            return;
        }

        BackupJob selectedJob = _jobRegistry.LoadJobs()[selectedRow.JobNumber - 1];
        SelectedJobLabel.Text = Format("Wpf.SelectedJobLabel", selectedRow.JobNumber, selectedRow.Name, selectedRow.Type);
        SourceTextBox.Text = selectedRow.Source;
        TargetTextBox.Text = selectedRow.Target;
        TypeComboBox.SelectedValue = selectedJob.Type;
    }

    private void SetBusy(bool busy, string message)
    {
        _isBusy = busy;
        RefreshButton.IsEnabled = !busy;
        RunSelectedButton.IsEnabled = !busy;
        RunAllButton.IsEnabled = !busy;
        SaveAllButton.IsEnabled = !busy;
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

    private void ApplyTexts()
    {
        Title = Text("Wpf.WindowTitle");
        HeadingTextBlock.Text = Text("Wpf.Heading");
        ConfiguredJobsGroupBox.Header = Text("Wpf.ConfiguredJobsHeader");
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
        SaveSelectedJobButton.Content = Text("Wpf.SaveSelectedJobButton");
        EditHintTextBlock.Text = Text("Wpf.EditHint");
        StateGroupBox.Header = Text("Wpf.StateHeader");
        LogGroupBox.Header = Text("Wpf.LogHeader");
        LanguageLabel.Text = Text("Wpf.LanguageLabel");
        AddJobButton.Content = Text("Wpf.AddJobButton");
        DeleteJobButton.Content = Text("Wpf.DeleteJobButton");
        RefreshButton.Content = Text("Wpf.RefreshButton");
        RunSelectedButton.Content = Text("Wpf.RunSelectedButton");
        RunAllButton.Content = Text("Wpf.RunAllButton");
        SaveAllButton.Content = Text("Wpf.SaveAllButton");
        ConfigureTypeSelector();

        if (string.IsNullOrWhiteSpace(StatusTextBlock.Text))
        {
            StatusTextBlock.Text = Text("Wpf.ReadyStatus");
        }

        UpdateSelectedJobLabel();
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
        LoadJobsIntoGrid();
        ApplyTexts();
        StatusTextBlock.Text = _textService.GetLanguageUpdatedMessage();
        RefreshStateAndLog();
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
        JobsDataGrid.SelectedIndex = Math.Min(selectedRow.JobNumber - 1, Math.Max(0, _jobRows.Count - 1));
        StatusTextBlock.Text = Format("Wpf.JobDeletedStatus", selectedRow.JobNumber);
        RefreshStateAndLog();
    }

    private sealed record LanguageOption(string LanguageCode, string DisplayName);
    private sealed record BackupTypeOption(BackupType Value, string DisplayName);
}
