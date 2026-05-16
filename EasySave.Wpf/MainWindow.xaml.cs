using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Text.Json;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Windows.Data;
using Microsoft.Win32;

namespace EasySave.Wpf;

public partial class MainWindow : Window
{
    private static readonly AtomicRuntimeFileStore RuntimeFileStore = new();
    private readonly BackupJobRegistry _jobRegistry;
    private readonly ScheduleRegistry _scheduleRegistry;
    private readonly IWindowsTaskSchedulerAdapter _taskSchedulerAdapter;
    private readonly StateService _stateService;
    private BackupController _backupController;
    private readonly List<JobRow> _jobRows;
    private readonly List<ScheduleRow> _scheduleRows;
    private readonly List<ScheduleJobChoice> _scheduleJobChoices;
    private readonly IBackupExecutionController _executionController;
    private readonly IBackupExecutionCoordinator _executionCoordinator;
    private readonly DispatcherTimer _refreshTimer;
    private ApplicationTextService _textService;
    private bool _isBusy;
    private int _lastToggledIndex = -1;
    private bool _isApplyingLanguage;
    private bool _isApplyingLogFormat;
    private bool _isApplyingLogStorageMode;
    private bool _isApplyingTheme;
    private List<BackupTypeOption> _backupTypeOptions = new();
    private List<ThemeOption> _themeOptions = new();
    private List<string> _blockedProcessNames = new();
    private List<string> _priorityExtensions = new();
    private List<ThresholdUnitOption> _thresholdUnitOptions = new();
    private DashboardSection _activeSection = DashboardSection.Overview;
    private bool _isAddingNewJob;
    private readonly DispatcherTimer _toastDismissTimer;
    private bool _toastInitialized;
    private DateTime _selectedLogDate = DateTime.Today;
    private bool _isApplyingLogHistorySelection;
    private IReadOnlyList<LogHistoryEntry> _logHistoryEntries = Array.Empty<LogHistoryEntry>();
    private string _logHistorySignature = string.Empty;
    private string _scheduleFileSignature = string.Empty;
    private const int LogHistoryPageSize = 12;
    private int _logHistoryPageIndex;
    private bool _isFullScreen;
    private string? _editingScheduleId;
    private IReadOnlyList<JobRow> _pendingDeleteRows = Array.Empty<JobRow>();
    private WindowState _preFullScreenWindowState = WindowState.Normal;
    private bool _preFullScreenTopmost;
    private double _preFullScreenLeft, _preFullScreenTop, _preFullScreenWidth, _preFullScreenHeight;

    public MainWindow()
    {
        App.ApplyConfiguredTheme();
        InitializeComponent();
        ApplyCroppedLogoImages();
        _jobRegistry = new BackupJobRegistry();
        _scheduleRegistry = new ScheduleRegistry();
        _taskSchedulerAdapter = new WindowsTaskSchedulerAdapter();
        _stateService = new StateService();
        _executionController = new InMemoryBackupExecutionController();
        _executionCoordinator = new PriorityTransferCoordinator();

        _textService = ApplicationTextService.Create();
        _backupController = CreateBackupController();

        _jobRows = new List<JobRow>();
        _scheduleRows = new List<ScheduleRow>();
        _scheduleJobChoices = new List<ScheduleJobChoice>();
        ConfigureThemeSelector();
        ConfigureLanguageSelector();
        ConfigureLogFormatSelector();
        ConfigureLogStorageModeSelector();
        ConfigureThresholdUnitSelector();
        ConfigureLogHistoryControls();
        ApplyTexts();
        UpdateMaximizeRestoreGlyph();
        this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        LoadRuntimeRulesIntoForm();
        LoadEncryptionSettingsIntoForm();
        LoadCentralLogSettingsIntoForm();
        RefreshBlockedProcesses();
        LoadJobsIntoGrid();
        LoadSchedulesIntoGrid();
        SetActiveSection(DashboardSection.Overview);
        RefreshStateAndLog();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += (_, _) => RefreshStateAndLog();
        _refreshTimer.Start();

        // Toast notification : surveille toute modification de StatusTextBlock.Text
        // pour faire apparaitre un popup transitoire (auto-dismiss apres 4 secondes)
        // au lieu d'un badge permanent dans l'entete.
        _toastDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(4200)
        };
        _toastDismissTimer.Tick += (_, _) => HideStatusToast();
        var textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        textDescriptor?.AddValueChanged(StatusTextBlock, (_, _) => OnStatusTextChanged());
        _toastInitialized = true;
    }

    private void OnStatusTextChanged()
    {
        if (!_toastInitialized)
        {
            return;
        }
        string text = StatusTextBlock.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            HideStatusToast();
            return;
        }
        ApplyToastSeverity(text);
        ShowStatusToast();
    }

    private void ApplyToastSeverity(string text)
    {
        string lower = text.ToLowerInvariant();
        string accentResource;
        string icon;

        // Check for real errors: word "erreur/error" must NOT be followed by ": 0"
        bool hasRealError = (lower.Contains("error") || lower.Contains("erreur"))
                            && !System.Text.RegularExpressions.Regex.IsMatch(lower,
                                @"erreurs?\s*:\s*0\b|errors?\s*:\s*0\b");

        if (hasRealError
            || lower.Contains("does not exist") || lower.Contains("n'existe pas")
            || lower.Contains("must be different") || lower.Contains("identiques"))
        {
            accentResource = "DangerBrush";
            icon = "\uEA39"; // ErrorBadge
        }
        else if (lower.Contains("please") || lower.Contains("veuillez")
                 || lower.Contains("select") || lower.Contains("selection"))
        {
            accentResource = "WarningBrush";
            icon = "\uE7BA"; // Warning
        }
        else if (lower.Contains("complete") || lower.Contains("termine")
                 || lower.Contains("succes") || lower.Contains("success")
                 || lower.Contains("added") || lower.Contains("ajoute")
                 || lower.Contains("updated") || lower.Contains("mis a jour")
                 || lower.Contains("saved") || lower.Contains("enregistre"))
        {
            accentResource = "SuccessBrush";
            icon = "\uE930"; // Completed
        }
        else
        {
            accentResource = "AccentBrush";
            icon = "\uE946"; // Info
        }

        if (TryFindResource(accentResource) is Brush brush)
        {
            StatusToastAccentBar.Background = brush;
            StatusToastIcon.Foreground = brush;
        }
        StatusToastIcon.Text = icon;
    }

    private void ShowStatusToast()
    {
        StatusToastCard.Visibility = Visibility.Visible;
        StatusToastCard.BeginAnimation(UIElement.OpacityProperty, null);
        var fadeIn = new DoubleAnimation
        {
            From = StatusToastCard.Opacity,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        StatusToastCard.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        _toastDismissTimer.Stop();
        _toastDismissTimer.Start();
    }

    private void HideStatusToast()
    {
        _toastDismissTimer.Stop();
        if (StatusToastCard.Visibility != Visibility.Visible)
        {
            return;
        }
        var fadeOut = new DoubleAnimation
        {
            From = StatusToastCard.Opacity,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            if (StatusToastCard.Opacity <= 0.01)
            {
                StatusToastCard.Visibility = Visibility.Collapsed;
            }
        };
        StatusToastCard.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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

    private void ApplyCroppedLogoImages()
    {
        Icon = CreateCroppedLogoSource(4);
        TitleBarLogoImage.Source = CreateCroppedLogoSource(16);
    }

    private static BitmapSource CreateCroppedLogoSource(int padding)
    {
        var source = new BitmapImage();
        source.BeginInit();
        source.UriSource = new Uri("pack://application:,,,/Assets/EasySaveLogo.png", UriKind.Absolute);
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.EndInit();

        BitmapSource readableSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        Int32Rect cropArea = FindVisibleLogoBounds(readableSource, padding);
        var croppedSource = new CroppedBitmap(readableSource, cropArea);
        if (croppedSource.CanFreeze)
        {
            croppedSource.Freeze();
        }

        return croppedSource;
    }

    private static Int32Rect FindVisibleLogoBounds(BitmapSource source, int padding)
    {
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                int offset = rowOffset + x * 4;
                byte blue = pixels[offset];
                byte green = pixels[offset + 1];
                byte red = pixels[offset + 2];
                byte alpha = pixels[offset + 3];

                if (alpha <= 10 || (red >= 245 && green >= 245 && blue >= 245))
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new Int32Rect(0, 0, width, height);
        }

        int left = Math.Max(0, minX - padding);
        int top = Math.Max(0, minY - padding);
        int right = Math.Min(width - 1, maxX + padding);
        int bottom = Math.Min(height - 1, maxY + padding);

        return new Int32Rect(left, top, right - left + 1, bottom - top + 1);
    }

    private void LoadJobsIntoGrid()
    {
        _jobRows.Clear();
        _lastToggledIndex = -1;
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

    private void LoadSchedulesIntoGrid()
    {
        _scheduleRows.Clear();
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();
        Dictionary<string, BackupJob> jobsById = jobs
            .Where(job => !string.IsNullOrWhiteSpace(job.Id))
            .ToDictionary(job => job.Id, StringComparer.OrdinalIgnoreCase);

        foreach (BackupSchedule schedule in _scheduleRegistry.LoadSchedules())
        {
            _scheduleRows.Add(new ScheduleRow
            {
                Id = schedule.Id,
                Name = schedule.Name,
                EnabledStatus = schedule.IsEnabled ? UiText("Yes", "Oui") : UiText("No", "Non"),
                JobsSummary = BuildScheduleJobsSummary(schedule, jobsById),
                TimeSummary = $"{schedule.LocalRunTime} - {BuildWeekdaysSummary(schedule.Weekdays)}",
                LastRunSummary = BuildLastRunSummary(schedule)
            });
        }

        SchedulesDataGrid.ItemsSource = null;
        SchedulesDataGrid.ItemsSource = _scheduleRows;
        _scheduleFileSignature = BuildScheduleFileSignature();
    }

    private void RefreshSchedulesIfChanged()
    {
        string currentSignature = BuildScheduleFileSignature();
        if (!string.Equals(_scheduleFileSignature, currentSignature, StringComparison.Ordinal))
        {
            LoadSchedulesIntoGrid();
        }
    }

    private static string BuildScheduleFileSignature()
    {
        string filePath = RuntimeStoragePaths.SchedulesFilePath;
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        var fileInfo = new FileInfo(filePath);
        return $"{fileInfo.Length}:{fileInfo.LastWriteTimeUtc.Ticks}";
    }

    private string BuildScheduleJobsSummary(BackupSchedule schedule, IReadOnlyDictionary<string, BackupJob> jobsById)
    {
        List<string> jobNames = schedule.TargetJobIds
            .Select(jobId => jobsById.TryGetValue(jobId, out BackupJob? job) ? job.Name : UiText("Missing job", "Job manquant"))
            .ToList();

        return jobNames.Count == 0 ? UiText("No jobs", "Aucun job") : string.Join(", ", jobNames);
    }

    private string BuildWeekdaysSummary(IEnumerable<DayOfWeek> weekdays)
    {
        DayOfWeek[] orderedWeekdays = weekdays.OrderBy(WeekdaySortOrder).ToArray();
        if (orderedWeekdays.Length == 7)
        {
            return UiText("Daily", "Tous les jours");
        }

        return string.Join(", ", orderedWeekdays.Select(GetShortWeekdayName));
    }

    private string BuildLastRunSummary(BackupSchedule schedule)
    {
        if (!schedule.LastRunCompletedAtUtc.HasValue)
        {
            return UiText("Never", "Jamais");
        }

        DateTime completedLocal = schedule.LastRunCompletedAtUtc.Value.ToLocalTime();
        string status = string.IsNullOrWhiteSpace(schedule.LastRunStatus)
            ? UiText("Unknown", "Inconnu")
            : schedule.LastRunStatus;
        return $"{completedLocal:g} - {status} - {schedule.LastRunMessage}";
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
            LoadSchedulesIntoGrid();
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
        LoadSchedulesIntoGrid();
        EditJobOverlay.Visibility = Visibility.Collapsed;
        StatusTextBlock.Text = Format("Wpf.JobUpdatedStatus", selectedRow.JobNumber);
        UpdateDashboardMetrics();
        RefreshStateAndLog();
    }

    private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseForJobFolder(SourceTextBox, UiText("Select source folder", "Selectionner le dossier source"));
    }

    private void BrowseTargetButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseForJobFolder(TargetTextBox, UiText("Select target folder", "Selectionner le dossier cible"));
    }

    private void BrowseForJobFolder(TextBox targetTextBox, string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        string currentPath = targetTextBox.Text.Trim();
        if (Directory.Exists(currentPath))
        {
            dialog.InitialDirectory = currentPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            targetTextBox.Text = dialog.FolderName;
            targetTextBox.CaretIndex = targetTextBox.Text.Length;
            targetTextBox.Focus();
        }
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
        LoadSchedulesIntoGrid();
        RefreshStateAndLog();
        StatusTextBlock.Text = Text("Wpf.RefreshedStatus");
    }

    private void LogDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLogHistorySelection || LogDatePicker.SelectedDate is not DateTime selectedDate)
        {
            return;
        }

        SetSelectedLogDate(selectedDate);
    }

    private void PreviousLogDateButton_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedLogDate(_selectedLogDate.AddDays(-1));
    }

    private void TodayLogDateButton_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedLogDate(DateTime.Today);
    }

    private void NextLogDateButton_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedLogDate(_selectedLogDate.AddDays(1));
    }

    private void LogHistoryPrevPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_logHistoryPageIndex > 0)
        {
            _logHistoryPageIndex--;
            ApplyLogHistoryPage();
        }
    }

    private void LogHistoryNextPageButton_Click(object sender, RoutedEventArgs e)
    {
        int totalPages = GetLogHistoryPageCount();
        if (_logHistoryPageIndex < totalPages - 1)
        {
            _logHistoryPageIndex++;
            ApplyLogHistoryPage();
        }
    }

    private int GetLogHistoryPageCount()
    {
        if (_logHistoryEntries.Count == 0)
        {
            return 1;
        }
        return (int)Math.Ceiling(_logHistoryEntries.Count / (double)LogHistoryPageSize);
    }

    private void ApplyLogHistoryPage()
    {
        int totalPages = GetLogHistoryPageCount();
        if (_logHistoryPageIndex >= totalPages)
        {
            _logHistoryPageIndex = Math.Max(0, totalPages - 1);
        }

        IEnumerable<LogHistoryEntry> pageItems = _logHistoryEntries
            .Skip(_logHistoryPageIndex * LogHistoryPageSize)
            .Take(LogHistoryPageSize);
        var pageList = pageItems.ToList();

        _isApplyingLogHistorySelection = true;
        LogHistoryListBox.ItemsSource = null;
        LogHistoryListBox.ItemsSource = pageList;
        LogHistoryEntry? selectedEntry = pageList.FirstOrDefault(entry => entry.Date == _selectedLogDate.Date);
        LogHistoryListBox.SelectedItem = selectedEntry;
        _isApplyingLogHistorySelection = false;

        if (LogHistoryPageInfoTextBlock is not null)
        {
            string pageLabel = UiText("Page", "Page");
            LogHistoryPageInfoTextBlock.Text = $"{pageLabel} {_logHistoryPageIndex + 1} / {totalPages}";
        }
        if (LogHistoryPrevPageButton is not null)
        {
            LogHistoryPrevPageButton.IsEnabled = _logHistoryPageIndex > 0;
        }
        if (LogHistoryNextPageButton is not null)
        {
            LogHistoryNextPageButton.IsEnabled = _logHistoryPageIndex < totalPages - 1;
        }
    }

    private void LogHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLogHistorySelection || LogHistoryListBox.SelectedItem is not LogHistoryEntry selectedEntry)
        {
            return;
        }

        SetSelectedLogDate(selectedEntry.Date);
    }

    private void SetSelectedLogDate(DateTime selectedDate)
    {
        _selectedLogDate = selectedDate.Date;
        RefreshStateAndLog();
    }

    private void RefreshStateAndLog()
    {
        StateTextBox.Text = ReadFileSafely(RuntimeStoragePaths.StateFilePath);
        RefreshLogHistory();
        LogTextBox.Text = ReadSelectedLogSafely();
        RefreshSchedulesIfChanged();
        ApplyRuntimeStateToRows();
    }

    private string ReadSelectedLogSafely()
    {
        if (RuntimeStoragePaths.GetLogStorageMode() == RuntimeStoragePaths.CentralizedLogStorageMode)
        {
            try
            {
                string centralContent = new CentralLogClient()
                    .GetDailyLogAsync(_selectedLogDate)
                    .GetAwaiter()
                    .GetResult();
                return string.IsNullOrWhiteSpace(centralContent)
                    ? Text("Wpf.EmptyFile")
                    : centralContent;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        IReadOnlyList<string> logFilePaths = GetLocalLogFilePathsForDate(_selectedLogDate);
        if (logFilePaths.Count == 0)
        {
            return Format("Wpf.FileNotFound", RuntimeStoragePaths.GetDailyLogFilePath(_selectedLogDate));
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            logFilePaths.Select(path => $"===== {Path.GetFileName(path)} ====={Environment.NewLine}{ReadFileSafely(path)}"));
    }

    private void RefreshLogHistory()
    {
        IReadOnlyList<LogHistoryEntry> loadedEntries = LoadLogHistoryEntries();
        string loadedSignature = BuildLogHistorySignature(loadedEntries);
        IReadOnlyList<LogHistoryEntry> entries = _logHistoryEntries;

        if (!string.Equals(_logHistorySignature, loadedSignature, StringComparison.Ordinal))
        {
            _logHistoryEntries = loadedEntries;
            _logHistorySignature = loadedSignature;
            entries = loadedEntries;
            _logHistoryPageIndex = 0;
        }

        LogHistoryEntry? selectedEntry = entries.FirstOrDefault(entry => entry.Date == _selectedLogDate.Date);

        // Snap page to selected entry
        if (selectedEntry is not null)
        {
            int idx = entries.ToList().IndexOf(selectedEntry);
            if (idx >= 0)
            {
                _logHistoryPageIndex = idx / LogHistoryPageSize;
            }
        }

        ApplyLogHistoryPage();

        _isApplyingLogHistorySelection = true;
        LogDatePicker.SelectedDate = _selectedLogDate;
        _isApplyingLogHistorySelection = false;

        LogHistorySummaryTextBlock.Text = BuildLogHistorySummary(entries, selectedEntry);
    }

    private IReadOnlyList<LogHistoryEntry> LoadLogHistoryEntries()
    {
        if (RuntimeStoragePaths.GetLogStorageMode() == RuntimeStoragePaths.CentralizedLogStorageMode)
        {
            return
            [
                new LogHistoryEntry(
                    _selectedLogDate,
                    $"{_selectedLogDate:yyyy-MM-dd}",
                    UiText("Centralized log", "Log centralise"),
                    Array.Empty<string>())
            ];
        }

        if (!Directory.Exists(RuntimeStoragePaths.LogsDirectoryPath))
        {
            return Array.Empty<LogHistoryEntry>();
        }

        return RuntimeStoragePaths.GetSupportedLogFilePatterns()
            .SelectMany(pattern => Directory.EnumerateFiles(RuntimeStoragePaths.LogsDirectoryPath, pattern))
            .Select(TryCreateLocalLogHistoryItem)
            .Where(entry => entry is not null)
            .Cast<LocalLogFileEntry>()
            .GroupBy(entry => entry.Date)
            .OrderByDescending(group => group.Key)
            .Select(group =>
            {
                List<string> filePaths = group
                    .OrderBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => entry.FilePath)
                    .ToList();
                string formats = string.Join(", ", group
                    .Select(entry => Path.GetExtension(entry.FileName).TrimStart('.').ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(format => format, StringComparer.OrdinalIgnoreCase));
                return new LogHistoryEntry(
                    group.Key,
                    $"{group.Key:yyyy-MM-dd}",
                    formats,
                    filePaths);
            })
            .ToList();
    }

    private static LocalLogFileEntry? TryCreateLocalLogHistoryItem(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        if (fileName.Length < "yyyy-MM-dd".Length)
        {
            return null;
        }

        string datePart = fileName.Substring(0, "yyyy-MM-dd".Length);
        if (!DateTime.TryParseExact(
                datePart,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
        {
            return null;
        }

        return new LocalLogFileEntry(date.Date, filePath, fileName);
    }

    private static IReadOnlyList<string> GetLocalLogFilePathsForDate(DateTime date)
    {
        if (!Directory.Exists(RuntimeStoragePaths.LogsDirectoryPath))
        {
            return Array.Empty<string>();
        }

        string datePrefix = $"{date:yyyy-MM-dd}";
        return RuntimeStoragePaths.GetSupportedLogFilePatterns()
            .SelectMany(pattern => Directory.EnumerateFiles(RuntimeStoragePaths.LogsDirectoryPath, pattern))
            .Where(path => Path.GetFileName(path).StartsWith(datePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string BuildLogHistorySummary(IReadOnlyList<LogHistoryEntry> entries, LogHistoryEntry? selectedEntry)
    {
        string mode = _textService.GetLogStorageModeDisplayName(RuntimeStoragePaths.GetLogStorageMode());
        if (selectedEntry is null)
        {
            return UiText(
                $"{entries.Count} log day(s) found - selected date: {_selectedLogDate:yyyy-MM-dd} - {mode}",
                $"{entries.Count} jour(s) de logs trouve(s) - date selectionnee : {_selectedLogDate:yyyy-MM-dd} - {mode}");
        }

        return UiText(
            $"{entries.Count} log day(s) found - selected: {selectedEntry.DisplayName} ({selectedEntry.Detail}) - {mode}",
            $"{entries.Count} jour(s) de logs trouve(s) - selection : {selectedEntry.DisplayName} ({selectedEntry.Detail}) - {mode}");
    }

    private static string BuildLogHistorySignature(IEnumerable<LogHistoryEntry> entries)
    {
        return string.Join(
            "|",
            entries.Select(entry => $"{entry.Date:yyyy-MM-dd}:{entry.Detail}:{string.Join(",", entry.FilePaths)}"));
    }

    private string ReadFileSafely(string path)
    {
        if (!File.Exists(path))
        {
            return Format("Wpf.FileNotFound", path);
        }

        string? content = RuntimeFileStore.ReadAllText(path);
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

    private void JobRow_ToggleSelect(object sender, MouseButtonEventArgs e)
    {
        var dep = (DependencyObject)e.OriginalSource;
        while (dep != null && dep is not DataGridRow)
            dep = VisualTreeHelper.GetParent(dep);

        if (dep is not DataGridRow gridRow || gridRow.Item is not JobRow jobRow)
            return;

        int clickedIndex = _jobRows.IndexOf(jobRow);

        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 && _lastToggledIndex >= 0 && clickedIndex >= 0)
        {
            int from = Math.Min(_lastToggledIndex, clickedIndex);
            int to = Math.Max(_lastToggledIndex, clickedIndex);
            for (int i = from; i <= to; i++)
                _jobRows[i].IsSelected = true;
        }
        else
        {
            jobRow.IsSelected = !jobRow.IsSelected;
            _lastToggledIndex = clickedIndex;
        }

        SafeRefresh(JobsDataGrid);
        SafeRefresh(OverviewJobsDataGrid);
        SafeRefresh(ExecutionJobsDataGrid);
        UpdateDashboardMetrics();
    }

    private void SelectAllJobsButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (JobRow row in _jobRows)
            row.IsSelected = true;
        SafeRefresh(JobsDataGrid);
        SafeRefresh(OverviewJobsDataGrid);
        SafeRefresh(ExecutionJobsDataGrid);
        UpdateDashboardMetrics();
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (JobRow row in _jobRows)
            row.IsSelected = false;
        _lastToggledIndex = -1;
        SafeRefresh(JobsDataGrid);
        SafeRefresh(OverviewJobsDataGrid);
        SafeRefresh(ExecutionJobsDataGrid);
        UpdateDashboardMetrics();
    }

    private void JobsDataGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            bool allSelected = _jobRows.Count > 0 && _jobRows.All(row => row.IsSelected);
            foreach (JobRow row in _jobRows)
                row.IsSelected = !allSelected;
            SafeRefresh(JobsDataGrid);
            SafeRefresh(OverviewJobsDataGrid);
            SafeRefresh(ExecutionJobsDataGrid);
            UpdateDashboardMetrics();
            e.Handled = true;
        }
    }

    private void SetBusy(bool busy, string message)
    {
        _isBusy = busy;
        RefreshButton.IsEnabled = !busy;
        RunSelectedButton.IsEnabled = !busy;
        RunAllButton.IsEnabled = !busy;
        SaveAllButton.IsEnabled = !busy;
        SaveCentralLogSettingsButton.IsEnabled = !busy;
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

    private void ConfigureLogStorageModeSelector()
    {
        _isApplyingLogStorageMode = true;
        LogStorageModeComboBox.ItemsSource = new[]
        {
            new LogStorageModeOption(RuntimeStoragePaths.LocalLogStorageMode, _textService.GetLogStorageModeDisplayName(RuntimeStoragePaths.LocalLogStorageMode)),
            new LogStorageModeOption(RuntimeStoragePaths.CentralizedLogStorageMode, _textService.GetLogStorageModeDisplayName(RuntimeStoragePaths.CentralizedLogStorageMode)),
            new LogStorageModeOption(RuntimeStoragePaths.BothLogStorageMode, _textService.GetLogStorageModeDisplayName(RuntimeStoragePaths.BothLogStorageMode))
        };
        LogStorageModeComboBox.DisplayMemberPath = nameof(LogStorageModeOption.DisplayName);
        LogStorageModeComboBox.SelectedValuePath = nameof(LogStorageModeOption.Value);
        LogStorageModeComboBox.SelectedValue = RuntimeStoragePaths.GetLogStorageMode();
        _isApplyingLogStorageMode = false;
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

    private void ConfigureThresholdUnitSelector()
    {
        _thresholdUnitOptions =
        [
            new ThresholdUnitOption("KB", 1),
            new ThresholdUnitOption("MB", 1024),
            new ThresholdUnitOption("GB", 1024 * 1024)
        ];
        LargeFileThresholdUnitComboBox.ItemsSource = _thresholdUnitOptions;
        LargeFileThresholdUnitComboBox.DisplayMemberPath = nameof(ThresholdUnitOption.DisplayName);
        LargeFileThresholdUnitComboBox.SelectedValuePath = nameof(ThresholdUnitOption.ValueInKb);
    }

    private void ConfigureLogHistoryControls()
    {
        _isApplyingLogHistorySelection = true;
        LogDatePicker.SelectedDate = _selectedLogDate;
        _isApplyingLogHistorySelection = false;
    }

    private void ApplyTexts()
    {
        Title = Text("Wpf.WindowTitle");
        HeadingTextBlock.Text = Text("Wpf.Heading");
        HeroDescriptionTextBlock.Text = Text("Wpf.HeroDescription");
        SetButtonContent(OverviewNavButton, "\uE80F", Text("Wpf.NavOverview"));
        SetButtonContent(TasksNavButton, "\uE8FD", Text("Wpf.NavTasks"));
        SetButtonContent(SchedulesNavButton, "\uE823", UiText("Schedules", "Planifications"));
        SetButtonContent(ExecutionNavButton, "\uE768", Text("Wpf.NavExecution"));
        SetButtonContent(StateLogsNavButton, "\uE9D2", Text("Wpf.NavStateLogs"));
        SetButtonContent(SettingsNavButton, "\uE713", Text("Wpf.NavSettings"));
        PageSubtitleTextBlock.Text = Text("Wpf.SubtitleOverview");
        ConfiguredJobsGroupBox.Header = Text("Wpf.ConfiguredJobsHeader");
        SchedulesGroupBox.Header = UiText("Schedules", "Planifications");
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
        BrowseSourceButton.ToolTip = UiText("Choose the source folder", "Choisir le dossier source");
        BrowseTargetButton.ToolTip = UiText("Choose the target folder", "Choisir le dossier cible");
        EncryptedExtensionsLabel.Text = Text("Wpf.EncryptedExtensionsLabel");
        CryptoSoftKeyLabel.Text = Text("Wpf.CryptoSoftKeyLabel");
        SetButtonContent(EditJobButton, "", UiText("Edit selected", "Modifier selection"));
        SetButtonContent(CancelEditButton, "", UiText("Cancel", "Annuler"));
        StateGroupBoxHeader.Text = Text("Wpf.StateHeader");
        LogGroupBoxHeader.Text = Text("Wpf.LogHeader");
        StateLogsPageTitle.Text = UiText("States & logs", "\u00C9tats & logs");
        StateLogsPageSubtitle.Text = UiText(
            "Inspect the live state file and browse past activity logs.",
            "Consultez l'\u00E9tat en direct et parcourez l'historique des logs.");
        SettingsTitleTextBlock.Text = Text("Wpf.SettingsHeader");
        AppearanceSectionTitle.Text = Text("Wpf.AppearanceSectionTitle");
        ThemeLabel.Text = Text("Wpf.ThemeLabel");
        LanguageSectionTitle.Text = Text("Wpf.LanguageSectionTitle");
        LogFormatSectionTitle.Text = Text("Wpf.LogFormatSectionTitle");
        EncryptionSectionTitle.Text = Text("Wpf.EncryptionSectionTitle");
        RuntimeRulesSectionTitle.Text = UiText("Runtime scheduling", "Ordonnancement runtime");
        PriorityExtensionsLabel.Text = UiText("Priority extensions", "Extensions prioritaires");
        PriorityExtensionInputLabel.Text = UiText("Add an extension", "Ajouter une extension");
        PriorityPreviewLabel.Text = UiText("Effective priority order", "Ordre effectif des priorites");
        LargeFileThresholdLabel.Text = UiText("Large file threshold", "Seuil gros fichiers");
        MaxConcurrencyLabel.Text = UiText("Max concurrent jobs", "Nombre max de jobs concurrents");
        BusinessSoftwareSectionTitle.Text = Text("Wpf.BusinessSoftwareSectionTitle");
        LanguageLabel.Text = Text("Wpf.LanguageLabel");
        LogFormatLabel.Text = Text("Wpf.LogFormatLabel");
        LogStorageModeLabel.Text = Text("Wpf.LogStorageModeLabel");
        CentralLogServerUrlLabel.Text = Text("Wpf.CentralLogServerUrlLabel");
        CentralLogUserNameLabel.Text = Text("Wpf.CentralLogUserNameLabel");
        CentralLogApiKeyLabel.Text = Text("Wpf.CentralLogApiKeyLabel");
        BlockedProcessesLabel.Text = Text("Wpf.BlockedProcessesLabel");
        ProcessNameLabel.Text = Text("Wpf.ProcessNameLabel");
        SetButtonContent(AddProcessButton, "\uE710", Text("Wpf.AddBlockedProcessButton"));
        SetButtonContent(RemoveProcessButton, "\uE74D", Text("Wpf.RemoveBlockedProcessButton"));
        SetButtonContent(AddPriorityExtensionButton, "\uE710", UiText("Add extension", "Ajouter extension"));
        SetButtonContent(RemovePriorityExtensionButton, "\uE74D", UiText("Remove selected", "Supprimer selection"));
        SetButtonContent(AddJobButton, "\uE710", Text("Wpf.AddJobButton"));
        SetButtonContent(DeleteJobButton, "\uE74D", Text("Wpf.DeleteJobButton"));
        SetButtonContent(CreateScheduleButton, "\uE823", UiText("New schedule", "Nouvelle planif."));
        SetButtonContent(EditScheduleButton, "\uE70F", UiText("Edit schedule", "Modifier planif."));
        SetButtonContent(ToggleScheduleButton, "\uE7E8", UiText("Enable/disable", "Activer/desactiver"));
        SetButtonContent(DeleteScheduleButton, "\uE74D", UiText("Delete schedule", "Supprimer planif."));
        SetButtonContent(SelectAllJobsButton, "\uE8B3", UiText("Select all", "Tout cocher"));
        SetButtonContent(ClearSelectionButton, "\uE8E6", UiText("Clear selection", "Tout d\u00E9cocher"));
        SetButtonContent(RefreshButton, "\uE72C", Text("Wpf.RefreshButton"));
        LogDateLabel.Text = UiText("Log date", "Date du log");
        LogHistoryListLabel.Text = UiText("History", "Historique");
        SetIconOnlyContent(PreviousLogDateButton, "\uE76B", UiText("Previous day", "Jour pr\u00E9c\u00E9dent"));
        SetButtonContent(TodayLogDateButton, "\uE787", UiText("Today", "Aujourd'hui"));
        SetIconOnlyContent(NextLogDateButton, "\uE76C", UiText("Next day", "Jour suivant"));
        SetIconOnlyContent(LogHistoryPrevPageButton, "\uE76B", UiText("Previous page", "Page pr\u00E9c\u00E9dente"));
        SetIconOnlyContent(LogHistoryNextPageButton, "\uE76C", UiText("Next page", "Page suivante"));
SetButtonContent(RunSelectedButton, "\uE768", Text("Wpf.RunSelectedButton"));
        SetButtonContent(RunAllButton, "\uE102", Text("Wpf.RunAllButton"));
        SetButtonContent(PauseSelectedButton, "\uE769", UiText("Pause selected", "Pause selection"));
        SetButtonContent(ResumeSelectedButton, "\uE768", UiText("Resume selected", "Reprendre selection"));
        SetButtonContent(StopSelectedButton, "\uE71A", UiText("Stop selected", "Arreter selection"));
        SetButtonContent(SaveAllButton, "\uE74E", Text("Wpf.SaveAllButton"));
        SetButtonContent(SaveSelectedJobButton, "\uE74E", Text("Wpf.SaveSelectedJobButton"));
        SetButtonContent(SaveCentralLogSettingsButton, "\uE74E", Text("Wpf.SaveCentralLogSettingsButton"));
        SetButtonContent(SaveEncryptionSettingsButton, "\uE74E", Text("Wpf.SaveEncryptionSettingsButton"));
        SetButtonContent(SaveRuntimeRulesButton, "\uE74E", UiText("Save runtime rules", "Enregistrer regles runtime"));
        ScheduleNameLabel.Text = UiText("Schedule name", "Nom de la planification");
        ScheduleTimeLabel.Text = UiText("Run time (HH:mm)", "Heure (HH:mm)");
        ScheduleWeekdaysLabel.Text = UiText("Weekdays", "Jours");
        ScheduleJobsLabel.Text = UiText("Jobs", "Jobs");
        ScheduleEnabledCheckBox.Content = UiText("Enabled", "Activee");
        ScheduleMondayCheckBox.Content = UiText("Monday", "Lundi");
        ScheduleTuesdayCheckBox.Content = UiText("Tuesday", "Mardi");
        ScheduleWednesdayCheckBox.Content = UiText("Wednesday", "Mercredi");
        ScheduleThursdayCheckBox.Content = UiText("Thursday", "Jeudi");
        ScheduleFridayCheckBox.Content = UiText("Friday", "Vendredi");
        ScheduleSaturdayCheckBox.Content = UiText("Saturday", "Samedi");
        ScheduleSundayCheckBox.Content = UiText("Sunday", "Dimanche");
        SetButtonContent(CancelScheduleButton, "\uE711", UiText("Cancel", "Annuler"));
        SetButtonContent(SaveScheduleButton, "\uE74E", UiText("Save schedule", "Enregistrer planif."));
        DeleteJobModalTitle.Text = UiText("Confirm deletion", "Confirmer la suppression");
        SetButtonContent(CancelDeleteJobButton, "\uE711", UiText("Cancel", "Annuler"));
        SetButtonContent(ConfirmDeleteJobButton, "\uE74D", Text("Wpf.DeleteJobButton"));
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
        ConfigureLogStorageModeSelector();
        LoadJobsIntoGrid();
        LoadSchedulesIntoGrid();
        RefreshBlockedProcesses();
        ApplyTexts();
        LoadEncryptionSettingsIntoForm();
        LoadCentralLogSettingsIntoForm();
        LoadRuntimeRulesIntoForm();
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

    private void LogStorageModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLogStorageMode || LogStorageModeComboBox.SelectedValue is not string logStorageMode)
        {
            return;
        }

        RuntimeStoragePaths.SetLogStorageMode(logStorageMode);
        ConfigureLogStorageModeSelector();
        StatusTextBlock.Text = Format("Wpf.LogStorageUpdatedStatus", _textService.GetLogStorageModeDisplayName(logStorageMode));
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

    private void LoadCentralLogSettingsIntoForm()
    {
        CentralLogServerUrlTextBox.Text = RuntimeStoragePaths.GetCentralLogServerUrl();
        CentralLogUserNameTextBox.Text = RuntimeStoragePaths.GetCentralLogUserName();
        CentralLogApiKeyTextBox.Text = RuntimeStoragePaths.GetCentralLogApiKey();
    }

    private void LoadRuntimeRulesIntoForm()
    {
        _priorityExtensions = RuntimeStoragePaths.GetPriorityExtensions().ToList();
        PriorityExtensionsListBox.ItemsSource = null;
        PriorityExtensionsListBox.ItemsSource = _priorityExtensions;
        PriorityPreviewTextBlock.Text = BuildPriorityPreview();
        int thresholdKb = RuntimeStoragePaths.GetLargeFileThresholdKb();
        ThresholdUnitOption selectedUnit = SelectBestThresholdUnit(thresholdKb);
        LargeFileThresholdUnitComboBox.SelectedValue = selectedUnit.ValueInKb;
        LargeFileThresholdTextBox.Text = FormatThresholdValue(thresholdKb, selectedUnit.ValueInKb);
        UpdateLargeFileThresholdSummary();
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

    private void SaveCentralLogSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimeStoragePaths.SetCentralLogServerUrl(CentralLogServerUrlTextBox.Text);
        RuntimeStoragePaths.SetCentralLogUserName(CentralLogUserNameTextBox.Text);
        RuntimeStoragePaths.SetCentralLogApiKey(CentralLogApiKeyTextBox.Text);
        LoadCentralLogSettingsIntoForm();
        StatusTextBlock.Text = Text("Wpf.CentralLogSettingsSavedStatus");
        RefreshStateAndLog();
    }

    private void SaveRuntimeRulesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseThresholdKb(out int thresholdKb))
        {
            StatusTextBlock.Text = UiText("Large file threshold must be a number >= 0.", "Le seuil gros fichiers doit etre un nombre >= 0.");
            return;
        }

        if (!int.TryParse(MaxConcurrencyTextBox.Text.Trim(), out int maxConcurrentJobs) || maxConcurrentJobs <= 0)
        {
            StatusTextBlock.Text = UiText("Max concurrent jobs must be a number > 0.", "Le nombre max de jobs concurrents doit etre > 0.");
            return;
        }

        RuntimeStoragePaths.SetPriorityExtensions(_priorityExtensions);
        RuntimeStoragePaths.SetLargeFileThresholdKb(thresholdKb);
        RuntimeStoragePaths.SetMaxConcurrentJobs(maxConcurrentJobs);
        LoadRuntimeRulesIntoForm();
        StatusTextBlock.Text = UiText("Runtime scheduling rules saved. Priority order preserved.", "Regles runtime enregistrees. L'ordre des priorites est conserve.");
    }

    private void AddPriorityExtensionButton_Click(object sender, RoutedEventArgs e)
    {
        string? extension = NormalizePriorityExtension(PriorityExtensionTextBox.Text);
        if (string.IsNullOrWhiteSpace(extension))
        {
            StatusTextBlock.Text = UiText("Enter a valid extension like .txt or pdf.", "Saisissez une extension valide comme .txt ou pdf.");
            return;
        }

        if (_priorityExtensions.Any(existing => string.Equals(existing, extension, StringComparison.OrdinalIgnoreCase)))
        {
            StatusTextBlock.Text = UiText("This extension is already in the priority list.", "Cette extension est deja dans la liste prioritaire.");
            return;
        }

        _priorityExtensions.Add(extension);
        RefreshPriorityExtensionsList();
        PriorityExtensionTextBox.Text = string.Empty;
        PriorityExtensionTextBox.Focus();
        StatusTextBlock.Text = UiText($"Added {extension} to the priority list.", $"{extension} a ete ajoutee a la liste prioritaire.");
    }

    private void PriorityExtensionTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        AddPriorityExtensionButton_Click(sender, e);
        e.Handled = true;
    }

    private void RemovePriorityExtensionButton_Click(object sender, RoutedEventArgs e)
    {
        if (PriorityExtensionsListBox.SelectedItem is not string selectedExtension)
        {
            StatusTextBlock.Text = UiText("Select a priority extension first.", "Selectionnez d'abord une extension prioritaire.");
            return;
        }

        _priorityExtensions = _priorityExtensions
            .Where(extension => !string.Equals(extension, selectedExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();
        RefreshPriorityExtensionsList();
        StatusTextBlock.Text = UiText($"Removed {selectedExtension} from the priority list.", $"{selectedExtension} a ete retiree de la liste prioritaire.");
    }

    private void LargeFileThresholdUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateLargeFileThresholdSummary();
    }

    private void LargeFileThresholdTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLargeFileThresholdSummary();
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

    private void CreateScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSchedulePopup(null);
    }

    private void EditScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (SchedulesDataGrid.SelectedItem is not ScheduleRow row)
        {
            StatusTextBlock.Text = UiText("Select a schedule first.", "Selectionnez d'abord une planification.");
            return;
        }

        OpenSchedulePopup(row.Id);
    }

    private void ToggleScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (SchedulesDataGrid.SelectedItem is not ScheduleRow row)
        {
            StatusTextBlock.Text = UiText("Select a schedule first.", "Selectionnez d'abord une planification.");
            return;
        }

        try
        {
            BackupSchedule schedule = _scheduleRegistry.GetSchedule(row.Id);
            schedule.IsEnabled = !schedule.IsEnabled;
            CreateSchedulerService().SaveSchedule(schedule, ResolveConsoleRunnerPath());
            LoadSchedulesIntoGrid();
            StatusTextBlock.Text = schedule.IsEnabled
                ? UiText("Schedule enabled.", "Planification activee.")
                : UiText("Schedule disabled.", "Planification desactivee.");
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = Format("Wpf.ErrorStatus", exception.Message);
        }
    }

    private void DeleteScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (SchedulesDataGrid.SelectedItem is not ScheduleRow row)
        {
            StatusTextBlock.Text = UiText("Select a schedule first.", "Selectionnez d'abord une planification.");
            return;
        }

        try
        {
            CreateSchedulerService().DeleteSchedule(row.Id);
            LoadSchedulesIntoGrid();
            StatusTextBlock.Text = UiText("Schedule deleted.", "Planification supprimee.");
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = Format("Wpf.ErrorStatus", exception.Message);
        }
    }

    private void OpenSchedulePopup(string? scheduleId)
    {
        _editingScheduleId = scheduleId;
        BackupSchedule? schedule = string.IsNullOrWhiteSpace(scheduleId)
            ? null
            : _scheduleRegistry.GetSchedule(scheduleId);

        ScheduleModalTitle.Text = schedule is null
            ? UiText("New schedule", "Nouvelle planification")
            : UiText("Edit schedule", "Modifier la planification");
        ScheduleNameTextBox.Text = schedule?.Name ?? string.Empty;
        ScheduleTimeTextBox.Text = schedule?.LocalRunTime ?? "04:00";
        ScheduleEnabledCheckBox.IsChecked = schedule?.IsEnabled ?? true;
        SetWeekdayCheckboxes(schedule?.Weekdays ?? GetWeekdays().ToList());
        LoadScheduleJobChoices(schedule?.TargetJobIds ?? new List<string>());
        ScheduleOverlay.Visibility = Visibility.Visible;
        ScheduleNameTextBox.Focus();
    }

    private void CloseSchedulePopup_Click(object sender, RoutedEventArgs e)
    {
        _editingScheduleId = null;
        ScheduleOverlay.Visibility = Visibility.Collapsed;
    }

    private void SaveScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BackupSchedule schedule = string.IsNullOrWhiteSpace(_editingScheduleId)
                ? new BackupSchedule()
                : _scheduleRegistry.GetSchedule(_editingScheduleId);

            schedule.Name = ScheduleNameTextBox.Text.Trim();
            schedule.LocalRunTime = ScheduleTimeTextBox.Text.Trim();
            schedule.IsEnabled = ScheduleEnabledCheckBox.IsChecked == true;
            schedule.TargetJobIds = _scheduleJobChoices
                .Where(choice => choice.IsSelected)
                .Select(choice => choice.JobId)
                .ToList();
            schedule.Weekdays = GetSelectedWeekdays().ToList();

            CreateSchedulerService().SaveSchedule(schedule, ResolveConsoleRunnerPath());
            ScheduleOverlay.Visibility = Visibility.Collapsed;
            _editingScheduleId = null;
            LoadSchedulesIntoGrid();
            StatusTextBlock.Text = UiText("Schedule saved.", "Planification enregistree.");
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = Format("Wpf.ErrorStatus", exception.Message);
        }
    }

    private void LoadScheduleJobChoices(IEnumerable<string> selectedJobIds)
    {
        var selectedJobIdSet = selectedJobIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _scheduleJobChoices.Clear();
        IReadOnlyList<BackupJob> jobs = _jobRegistry.LoadJobs();

        for (int index = 0; index < jobs.Count; index++)
        {
            BackupJob job = jobs[index];
            _scheduleJobChoices.Add(new ScheduleJobChoice
            {
                IsSelected = selectedJobIdSet.Contains(job.Id),
                JobId = job.Id,
                JobNumber = index + 1,
                DisplayName = $"{index + 1}. {job.Name}"
            });
        }

        ScheduleJobsListBox.ItemsSource = null;
        ScheduleJobsListBox.ItemsSource = _scheduleJobChoices;
    }

    private void SetWeekdayCheckboxes(IEnumerable<DayOfWeek> weekdays)
    {
        var weekdaySet = weekdays.ToHashSet();
        ScheduleMondayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Monday);
        ScheduleTuesdayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Tuesday);
        ScheduleWednesdayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Wednesday);
        ScheduleThursdayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Thursday);
        ScheduleFridayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Friday);
        ScheduleSaturdayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Saturday);
        ScheduleSundayCheckBox.IsChecked = weekdaySet.Contains(DayOfWeek.Sunday);
    }

    private IEnumerable<DayOfWeek> GetSelectedWeekdays()
    {
        if (ScheduleMondayCheckBox.IsChecked == true) yield return DayOfWeek.Monday;
        if (ScheduleTuesdayCheckBox.IsChecked == true) yield return DayOfWeek.Tuesday;
        if (ScheduleWednesdayCheckBox.IsChecked == true) yield return DayOfWeek.Wednesday;
        if (ScheduleThursdayCheckBox.IsChecked == true) yield return DayOfWeek.Thursday;
        if (ScheduleFridayCheckBox.IsChecked == true) yield return DayOfWeek.Friday;
        if (ScheduleSaturdayCheckBox.IsChecked == true) yield return DayOfWeek.Saturday;
        if (ScheduleSundayCheckBox.IsChecked == true) yield return DayOfWeek.Sunday;
    }

    private static IEnumerable<DayOfWeek> GetWeekdays()
    {
        yield return DayOfWeek.Monday;
        yield return DayOfWeek.Tuesday;
        yield return DayOfWeek.Wednesday;
        yield return DayOfWeek.Thursday;
        yield return DayOfWeek.Friday;
    }

    private SchedulerService CreateSchedulerService()
    {
        return new SchedulerService(
            _scheduleRegistry,
            _jobRegistry,
            _stateService,
            new LoggerService(),
            _taskSchedulerAdapter,
            _backupController);
    }

    private static string ResolveConsoleRunnerPath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string localConsolePath = Path.Combine(baseDirectory, "EasySave.exe");
        if (File.Exists(localConsolePath))
        {
            return localConsolePath;
        }

        DirectoryInfo? directory = new DirectoryInfo(baseDirectory);
        while (directory is not null)
        {
            string debugPath = Path.Combine(directory.FullName, "EasySave.Console", "bin", "Debug", "net10.0", "EasySave.exe");
            if (File.Exists(debugPath))
            {
                return debugPath;
            }

            string releasePath = Path.Combine(directory.FullName, "EasySave.Console", "bin", "Release", "net10.0", "EasySave.exe");
            if (File.Exists(releasePath))
            {
                return releasePath;
            }

            directory = directory.Parent;
        }

        return localConsolePath;
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

        OpenDeleteJobPopup([row]);
    }

    private void DeleteJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        List<JobRow> toDelete = GetCheckedRows().ToList();
        if (toDelete.Count == 0)
        {
            StatusTextBlock.Text = Text("Wpf.SelectJobFirstStatus");
            return;
        }

        OpenDeleteJobPopup(toDelete);
        return;
    }

    private void OpenDeleteJobPopup(IReadOnlyList<JobRow> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        _pendingDeleteRows = rows.ToList();
        DeleteJobModalMessage.Text = rows.Count == 1
            ? UiText(
                $"Are you sure you want to delete {rows[0].Name}?",
                $"Etes-vous sur de vouloir supprimer {rows[0].Name} ?")
            : UiText(
                $"Are you sure you want to delete these backup jobs?{Environment.NewLine}{string.Join(Environment.NewLine, rows.Select(row => $"- {row.Name}"))}",
                $"Etes-vous sur de vouloir supprimer ces sauvegardes ?{Environment.NewLine}{string.Join(Environment.NewLine, rows.Select(row => $"- {row.Name}"))}");
        DeleteJobOverlay.Visibility = Visibility.Visible;
        ConfirmDeleteJobButton.Focus();
    }

    private void CloseDeleteJobPopup_Click(object sender, RoutedEventArgs e)
    {
        _pendingDeleteRows = Array.Empty<JobRow>();
        DeleteJobOverlay.Visibility = Visibility.Collapsed;
    }

    private void ConfirmDeleteJobButton_Click(object sender, RoutedEventArgs e)
    {
        List<JobRow> rowsToDelete = _pendingDeleteRows.ToList();
        if (rowsToDelete.Count == 0)
        {
            DeleteJobOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        foreach (JobRow row in rowsToDelete.OrderByDescending(r => r.JobNumber))
        {
            _jobRegistry.DeleteJob(row.JobNumber);
        }

        int count = rowsToDelete.Count;
        int firstDeletedJobNumber = rowsToDelete[0].JobNumber;
        _pendingDeleteRows = Array.Empty<JobRow>();
        DeleteJobOverlay.Visibility = Visibility.Collapsed;
        LoadJobsIntoGrid();
        LoadSchedulesIntoGrid();
        if (_jobRows.Count > 0)
        {
            JobsDataGrid.SelectedIndex = Math.Min(Math.Max(0, firstDeletedJobNumber - 1), _jobRows.Count - 1);
        }

        StatusTextBlock.Text = count == 1
            ? Format("Wpf.JobDeletedStatus", firstDeletedJobNumber)
            : UiText($"{count} jobs deleted.", $"{count} tache(s) supprimee(s).");
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ResizeMode == ResizeMode.CanResize)
        {
            ToggleWindowState();
            return;
        }

        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        DragMove();
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreWindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isFullScreen)
            ToggleFullScreen();
        else
            ToggleWindowState();
    }

    private void FullscreenWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void ToggleFullScreen()
    {
        if (!_isFullScreen)
        {
            _preFullScreenWindowState = WindowState;
            _preFullScreenTopmost = Topmost;

            // Normalize to Normal FIRST so Left/Top/Width/Height reflect the true
            // restore bounds, not the maximized dimensions.
            if (WindowState != WindowState.Normal)
                WindowState = WindowState.Normal;

            // Save restore bounds AFTER normalization.
            _preFullScreenLeft   = Left;
            _preFullScreenTop    = Top;
            _preFullScreenWidth  = Width;
            _preFullScreenHeight = Height;

            _isFullScreen = true;
            Topmost = true;
            if (TitleBarBorder is not null)
                TitleBarBorder.Visibility = Visibility.Collapsed;
            if (TitleBarRow is not null)
                TitleBarRow.Height = new GridLength(0);

            // Cover the full monitor via explicit positioning — avoids WM_GETMINMAXINFO
            // fighting with the taskbar that WindowState.Maximized causes.
            var hwnd = new WindowInteropHelper(this).Handle;
            var monitor = MonitorFromWindow(hwnd, 0x00000002);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                Left   = info.rcMonitor.Left   / dpi.DpiScaleX;
                Top    = info.rcMonitor.Top    / dpi.DpiScaleY;
                Width  = (info.rcMonitor.Right  - info.rcMonitor.Left) / dpi.DpiScaleX;
                Height = (info.rcMonitor.Bottom - info.rcMonitor.Top)  / dpi.DpiScaleY;
            }
        }
        else
        {
            _isFullScreen = false;
            Topmost = _preFullScreenTopmost;
            if (TitleBarBorder is not null)
                TitleBarBorder.Visibility = Visibility.Visible;
            if (TitleBarRow is not null)
                TitleBarRow.Height = new GridLength(30);

            if (_preFullScreenWindowState == WindowState.Maximized)
            {
                // Restore the correct Normal bounds before maximizing so WPF
                // uses them as restore position when the user clicks restore later.
                Left   = _preFullScreenLeft;
                Top    = _preFullScreenTop;
                Width  = _preFullScreenWidth;
                Height = _preFullScreenHeight;
                WindowState = WindowState.Maximized;
            }
            else
            {
                Left   = _preFullScreenLeft;
                Top    = _preFullScreenTop;
                Width  = _preFullScreenWidth;
                Height = _preFullScreenHeight;
            }
            UpdateMaximizeRestoreGlyph();
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F11)
        {
            ToggleFullScreen();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape && _isFullScreen)
        {
            ToggleFullScreen();
            e.Handled = true;
        }
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        if (_isFullScreen)
        {
            ToggleFullScreen();
            return;
        }
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        UpdateMaximizeRestoreGlyph();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(MaximizeWndProc);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private IntPtr MaximizeWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0024) // WM_GETMINMAXINFO
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var monitor = MonitorFromWindow(hwnd, 0x00000002); // MONITOR_DEFAULTTONEAREST
            if (monitor != IntPtr.Zero)
            {
                var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                GetMonitorInfo(monitor, ref info);
                RECT targetArea = _isFullScreen ? info.rcMonitor : info.rcWork;
                mmi.ptMaxPosition.X = targetArea.Left - info.rcMonitor.Left;
                mmi.ptMaxPosition.Y = targetArea.Top - info.rcMonitor.Top;
                mmi.ptMaxSize.X = targetArea.Right - targetArea.Left;
                mmi.ptMaxSize.Y = targetArea.Bottom - targetArea.Top;
            }

            // Enforce a minimum trackable window size so the custom chrome cannot be shrunk
            // below a usable layout. Values are in device pixels: convert from WPF DIPs via DPI.
            var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow ?? new Window());
            const double minDipWidth = 1080;
            const double minDipHeight = 700;
            mmi.ptMinTrackSize.X = (int)Math.Ceiling(minDipWidth * dpi.DpiScaleX);
            mmi.ptMinTrackSize.Y = (int)Math.Ceiling(minDipHeight * dpi.DpiScaleY);

            Marshal.StructureToPtr(mmi, lParam, true);
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateMaximizeRestoreGlyph();
    }

    private void UpdateMaximizeRestoreGlyph()
    {
        if (MaximizeRestoreWindowGlyph is null)
        {
            return;
        }

        MaximizeRestoreWindowGlyph.Text = (WindowState == WindowState.Maximized || _isFullScreen)
            ? "\uE923"
            : "\uE922";
    }

    private void OverviewNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Overview);
    }

    private void TasksNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Tasks);
    }

    private void SchedulesNavButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSection(DashboardSection.Schedules);
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
        SchedulesPanel.Visibility = section == DashboardSection.Schedules ? Visibility.Visible : Visibility.Collapsed;
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
            DashboardSection.Schedules => UiText("Create and manage Windows Task Scheduler backed backup schedules.", "Creez et gerez les planifications de sauvegarde via le Planificateur Windows."),
            DashboardSection.Execution => Text("Wpf.SubtitleExecution"),
            DashboardSection.StateLogs => Text("Wpf.SubtitleStateLogs"),
            DashboardSection.Settings => Text("Wpf.SubtitleSettings"),
            _ => Text("Wpf.SubtitleOverview")
        };

        ApplyNavigationButtonStyle(OverviewNavButton, _activeSection == DashboardSection.Overview);
        ApplyNavigationButtonStyle(TasksNavButton, _activeSection == DashboardSection.Tasks);
        ApplyNavigationButtonStyle(SchedulesNavButton, _activeSection == DashboardSection.Schedules);
        ApplyNavigationButtonStyle(ExecutionNavButton, _activeSection == DashboardSection.Execution);
        ApplyNavigationButtonStyle(StateLogsNavButton, _activeSection == DashboardSection.StateLogs);
        ApplyNavigationButtonStyle(SettingsNavButton, _activeSection == DashboardSection.Settings);
    }

    private static void SetIconOnlyContent(Button button, string iconGlyph, string toolTip)
    {
        var icon = new TextBlock
        {
            Text = iconGlyph,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            UseLayoutRounding = true
        };
        BindTextBlockToButtonForeground(icon);
        TextOptions.SetTextRenderingMode(icon, TextRenderingMode.ClearType);
        button.Content = icon;
        if (!string.IsNullOrEmpty(toolTip))
        {
            button.ToolTip = toolTip;
        }
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
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 17,
            Width = 24,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 11, 0),
            VerticalAlignment = VerticalAlignment.Center,
            UseLayoutRounding = true
        };
        BindTextBlockToButtonForeground(icon);
        TextOptions.SetTextRenderingMode(icon, TextRenderingMode.ClearType);

        var text = new TextBlock
        {
            Text = label,
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
            FontSize = 13.5,
            FontWeight = FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        BindTextBlockToButtonForeground(text);

        panel.Children.Add(icon);
        panel.Children.Add(text);
        button.Content = panel;
    }

    private static void BindTextBlockToButtonForeground(TextBlock textBlock)
    {
        textBlock.SetBinding(
            TextBlock.ForegroundProperty,
            new Binding(nameof(Control.Foreground))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });
    }

    private void ApplyNavigationButtonStyle(Button button, bool isActive)
    {
        if (isActive)
        {
            button.Background = Brushes.Transparent;
            button.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
            button.SetResourceReference(Control.BorderBrushProperty, "AccentBrush");
            button.BorderThickness = new Thickness(3, 0, 0, 0);
            return;
        }

        button.Background = Brushes.Transparent;
        button.SetResourceReference(Control.ForegroundProperty, "SidebarTextBrush");
        button.BorderBrush = Brushes.Transparent;
        button.BorderThickness = new Thickness(3, 0, 0, 0);
    }

    private void UpdateDashboardMetrics()
    {
        int totalJobs = _jobRows.Count;
        int configuredJobs = _jobRows.Count(IsConfigured);
        int selectedJobs = _jobRows.Count(row => row.IsSelected);

        KpiTotalJobsValueTextBlock.Text = totalJobs.ToString();
        KpiConfiguredJobsValueTextBlock.Text = configuredJobs.ToString();
        KpiSelectedJobsValueTextBlock.Text = selectedJobs.ToString();
        KpiStorageValueTextBlock.Text = RuntimeStoragePaths.GetLogStorageMode() == RuntimeStoragePaths.LocalLogStorageMode
            ? Text("Wpf.LocalStorageSummary")
            : _textService.GetLogStorageModeDisplayName(RuntimeStoragePaths.GetLogStorageMode());
        HeroStatusTextBlock.Text = Format("Wpf.HeroReadySummary", configuredJobs, totalJobs);

        if (selectedJobs == 0)
        {
            SelectionBadge.Visibility = Visibility.Collapsed;
        }
        else
        {
            SelectionBadge.Visibility = Visibility.Visible;
            SelectionBadgeText.Text = UiText($"{selectedJobs} selected", $"{selectedJobs} cochés");
        }
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

    private string BuildPriorityPreview()
    {
        if (_priorityExtensions.Count == 0)
        {
            return UiText(
                "No priority extensions configured.\nExample: .txt; .pdf; .docx",
                "Aucune extension prioritaire configuree.\nExemple : .txt; .pdf; .docx");
        }

        return string.Join(
            Environment.NewLine,
            _priorityExtensions.Select((extension, index) => $"{index + 1}. {extension}"));
    }

    private void RefreshPriorityExtensionsList()
    {
        PriorityExtensionsListBox.ItemsSource = null;
        PriorityExtensionsListBox.ItemsSource = _priorityExtensions;
        PriorityPreviewTextBlock.Text = BuildPriorityPreview();
    }

    private string? NormalizePriorityExtension(string? rawExtension)
    {
        if (string.IsNullOrWhiteSpace(rawExtension))
        {
            return null;
        }

        string normalized = rawExtension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.')
            ? normalized
            : $".{normalized}";
    }

    private ThresholdUnitOption SelectBestThresholdUnit(int thresholdKb)
    {
        if (thresholdKb > 0 && thresholdKb % (1024 * 1024) == 0)
        {
            return _thresholdUnitOptions.First(option => option.ValueInKb == 1024 * 1024);
        }

        if (thresholdKb > 0 && thresholdKb % 1024 == 0)
        {
            return _thresholdUnitOptions.First(option => option.ValueInKb == 1024);
        }

        return _thresholdUnitOptions.First(option => option.ValueInKb == 1);
    }

    private string FormatThresholdValue(int thresholdKb, int unitSizeInKb)
    {
        if (unitSizeInKb <= 0)
        {
            return thresholdKb.ToString(CultureInfo.CurrentCulture);
        }

        decimal value = thresholdKb / (decimal)unitSizeInKb;
        return value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.CurrentCulture)
            : value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private bool TryParseThresholdKb(out int thresholdKb)
    {
        thresholdKb = 0;
        string rawValue = LargeFileThresholdTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (!TryParseDecimal(rawValue, out decimal value) || value < 0)
        {
            return false;
        }

        int unitSizeInKb = LargeFileThresholdUnitComboBox.SelectedValue is int selectedUnit && selectedUnit > 0
            ? selectedUnit
            : 1;

        decimal thresholdValueInKb = value * unitSizeInKb;
        if (thresholdValueInKb > int.MaxValue)
        {
            return false;
        }

        thresholdKb = (int)Math.Round(thresholdValueInKb, MidpointRounding.AwayFromZero);
        return true;
    }

    private void UpdateLargeFileThresholdSummary()
    {
        if (!TryParseThresholdKb(out int thresholdKb))
        {
            LargeFileThresholdSummaryTextBlock.Text = UiText(
                "Enter a valid threshold to see the effective large-file rule.",
                "Saisissez un seuil valide pour voir la regle effective.");
            return;
        }

        if (thresholdKb == 0)
        {
            LargeFileThresholdSummaryTextBlock.Text = UiText(
                "Large-file rule disabled. Any file size is allowed in the standard flow.",
                "Regle gros fichiers desactivee. Toutes les tailles passent dans le flux standard.");
            return;
        }

        decimal thresholdInMb = thresholdKb / 1024m;
        decimal thresholdInGb = thresholdKb / 1024m / 1024m;
        LargeFileThresholdSummaryTextBlock.Text = UiText(
            $"Files larger than {FormatDecimal(thresholdInMb)} MB ({FormatDecimal(thresholdInGb)} GB) are treated as large files.",
            $"Les fichiers de plus de {FormatDecimal(thresholdInMb)} Mo ({FormatDecimal(thresholdInGb)} Go) sont traites comme gros fichiers.");
    }

    private static bool TryParseDecimal(string rawValue, out decimal value)
    {
        return decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatDecimal(decimal value)
    {
        return value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.CurrentCulture)
            : value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private string BuildTransferMode(BackupState state)
    {
        IReadOnlyList<string> priorityExtensions = _priorityExtensions.Count > 0
            ? _priorityExtensions
            : RuntimeStoragePaths.GetPriorityExtensions();
        int priorityRank = priorityExtensions
            .Select((extension, index) => new { extension, index })
            .Where(item => string.Equals(item.extension, state.CurrentPriorityExtension, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.index + 1)
            .DefaultIfEmpty(0)
            .First();

        string priorityLabel = state.CurrentFilePriority == FileTransferPriority.Priority
            ? priorityRank > 0
                ? $"P{priorityRank} {state.CurrentPriorityExtension}"
                : UiText("Priority", "Prioritaire")
            : UiText("Normal", "Normal");
        string sizeLabel = state.IsLargeFileTransfer
            ? UiText("Large file", "Gros fichier")
            : UiText("Standard", "Standard");

        if (state.IsPriorityWorkPending)
        {
            return $"{priorityLabel} | {sizeLabel} | {UiText("priority queue active", "file prioritaire active")}";
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

    private static int WeekdaySortOrder(DayOfWeek weekday)
    {
        return weekday == DayOfWeek.Sunday ? 7 : (int)weekday;
    }

    private string GetShortWeekdayName(DayOfWeek weekday)
    {
        return weekday switch
        {
            DayOfWeek.Monday => UiText("Mon", "Lun"),
            DayOfWeek.Tuesday => UiText("Tue", "Mar"),
            DayOfWeek.Wednesday => UiText("Wed", "Mer"),
            DayOfWeek.Thursday => UiText("Thu", "Jeu"),
            DayOfWeek.Friday => UiText("Fri", "Ven"),
            DayOfWeek.Saturday => UiText("Sat", "Sam"),
            DayOfWeek.Sunday => UiText("Sun", "Dim"),
            _ => string.Empty
        };
    }

    private sealed record LanguageOption(string LanguageCode, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
    private sealed record LogFormatOption(string Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
    private sealed record LogStorageModeOption(string Value, string DisplayName)
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
    private sealed record ThresholdUnitOption(string DisplayName, int ValueInKb)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record LogHistoryEntry(
        DateTime Date,
        string DisplayName,
        string Detail,
        IReadOnlyList<string> FilePaths)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Detail)
                ? DisplayName
                : $"{DisplayName}  -  {Detail}";
        }
    }

    private sealed record LocalLogFileEntry(DateTime Date, string FilePath, string FileName);

    private enum DashboardSection
    {
        Overview,
        Tasks,
        Schedules,
        Execution,
        StateLogs,
        Settings
    }
}
