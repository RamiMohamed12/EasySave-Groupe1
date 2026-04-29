using System.Windows;
using System.Text.Json;
using System.IO;

namespace EasySave.Wpf;

public partial class MainWindow : Window
{
    private readonly BackupJobRegistry _jobRegistry;
    private readonly StateService _stateService;
    private readonly BackupController _backupController;
    private readonly List<JobRow> _jobRows;
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
        _jobRegistry = new BackupJobRegistry();
        _stateService = new StateService();

        var textService = ApplicationTextService.Create();
        var backupService = new BackupService(
            new LoggerService(),
            _stateService,
            new BackupHistoryService(),
            textService);
        _backupController = new BackupController(backupService);

        _jobRows = new List<JobRow>();
        LoadJobsIntoGrid();
        RefreshStateAndLog();
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
                Type = job.Type.ToString(),
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
            StatusTextBlock.Text = "Aucun job coché.";
            return;
        }

        try
        {
            SetBusy(true, "Execution des jobs selectionnes...");
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
            StatusTextBlock.Text = $"Execution terminee - Succès: {successCount}, Erreurs: {errorCount}";
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Erreur: {exception.Message}";
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
            SetBusy(true, "Execution de tous les jobs...");
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
            StatusTextBlock.Text = $"Execution terminee - Succès: {successCount}, Erreurs: {errorCount}";
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Erreur: {exception.Message}";
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
            StatusTextBlock.Text = "Selectionne un job d'abord.";
            return;
        }

        selectedRow.Source = SourceTextBox.Text.Trim();
        selectedRow.Target = TargetTextBox.Text.Trim();
        JobsDataGrid.Items.Refresh();

        _jobRegistry.UpdateJobPath(selectedRow.JobNumber, JobPathField.Source, selectedRow.Source);
        _jobRegistry.UpdateJobPath(selectedRow.JobNumber, JobPathField.Target, selectedRow.Target);
        _stateService.SynchronizeConfiguredJobs(_jobRegistry.LoadJobs());
        StatusTextBlock.Text = $"Job {selectedRow.Name} mis a jour.";
        RefreshStateAndLog();
    }

    private void SaveAllButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAllRowsToRegistry();
        StatusTextBlock.Text = "Tableau enregistre dans jobs.json.";
        RefreshStateAndLog();
    }

    private void SaveAllRowsToRegistry()
    {
        foreach (JobRow row in _jobRows)
        {
            _jobRegistry.UpdateJobPath(row.JobNumber, JobPathField.Source, row.Source?.Trim() ?? string.Empty);
            _jobRegistry.UpdateJobPath(row.JobNumber, JobPathField.Target, row.Target?.Trim() ?? string.Empty);
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
        StatusTextBlock.Text = "Actualise.";
    }

    private void RefreshStateAndLog()
    {
        StateTextBox.Text = ReadFileSafely(RuntimeStoragePaths.StateFilePath);
        string todayLogPath = RuntimeStoragePaths.GetDailyLogFilePath(DateTime.Now);
        LogTextBox.Text = ReadFileSafely(todayLogPath);
    }

    private static string ReadFileSafely(string path)
    {
        if (!File.Exists(path))
        {
            return $"Fichier introuvable: {path}";
        }

        string content = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(content))
        {
            return "(fichier vide)";
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
            SelectedJobLabel.Text = "Aucun job sélectionné";
            SourceTextBox.Text = string.Empty;
            TargetTextBox.Text = string.Empty;
            return;
        }

        SelectedJobLabel.Text = $"Job {selectedRow.JobNumber} - {selectedRow.Name} ({selectedRow.Type})";
        SourceTextBox.Text = selectedRow.Source;
        TargetTextBox.Text = selectedRow.Target;
    }

    private void SetBusy(bool busy, string message)
    {
        _isBusy = busy;
        RefreshButton.IsEnabled = !busy;
        RunSelectedButton.IsEnabled = !busy;
        RunAllButton.IsEnabled = !busy;
        SaveAllButton.IsEnabled = !busy;
        JobsDataGrid.IsEnabled = !busy;
        StatusTextBlock.Text = message;
    }
}