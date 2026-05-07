using System.Windows;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace EasySave.Wpf;

public partial class BackupProgressWindow : Window
{
    private readonly DispatcherTimer _updateTimer;
    private readonly ApplicationTextService _textService;

    public BackupProgressWindow(ApplicationTextService textService)
    {
        InitializeComponent();
        _textService = textService;

        _updateTimer = new DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(500);
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();

        // Afficher le contenu initial
        StatusTitleTextBlock.Text = "⏳  SAUVEGARDE EN COURS";
        ReasonTextBlock.Text = "";
        ActionTextBlock.Text = "Si un logiciel métier est lancé, la sauvegarde se mettra automatiquement en pause.";
        CurrentJobTextBlock.Text = "Sauvegarde en cours...";
        ProgressTextBlock.Text = "Progression: 0% (0/0 fichiers)";
        TransferredTextBlock.Text = "Transféré: 0 B / 0 B";
        ProgressBar.Value = 0;

        UpdateStatus();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            string stateFilePath = RuntimeStoragePaths.StateFilePath;
            if (!File.Exists(stateFilePath))
            {
                return;
            }

            string json = File.ReadAllText(stateFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            // Parse backup state - utiliser les noms corrects avec minuscules/camelCase
            string backupName = GetJsonString(root, "BackupName");
            string status = GetJsonString(root, "Status");
            string errorMessage = GetJsonString(root, "ErrorMessage");
            
            bool isRunning = GetJsonBool(root, "IsRunning");
            long transferredBytes = GetJsonLong(root, "TransferredBytes");
            long totalBytes = GetJsonLong(root, "TotalEligibleBytes");
            int transferredFiles = GetJsonInt(root, "TransferredFileCount");
            int totalFiles = GetJsonInt(root, "TotalEligibleFileCount");

            // Update status title and reason
            if (status == "Paused")
            {
                StatusTitleTextBlock.Text = "⏸️  EN PAUSE";
                ReasonTextBlock.Text = $"Raison: {errorMessage}";
                ActionTextBlock.Text = "Action: Fermez l'application pour reprendre la sauvegarde automatiquement";
            }
            else if (isRunning)
            {
                StatusTitleTextBlock.Text = "⏳  SAUVEGARDE EN COURS";
                ReasonTextBlock.Text = "";
                ActionTextBlock.Text = "Si un logiciel métier est lancé, la sauvegarde se mettra automatiquement en pause.";
            }
            else
            {
                StatusTitleTextBlock.Text = "✅  TERMINÉE";
                ReasonTextBlock.Text = "";
                ActionTextBlock.Text = "";
            }

            // Update details
            CurrentJobTextBlock.Text = !string.IsNullOrEmpty(backupName) ? $"Sauvegarde: {backupName}" : "Sauvegarde en cours...";
            
            // Calculate and display progress
            double progressPercent = totalBytes > 0 ? (double)transferredBytes / totalBytes * 100 : 0;
            ProgressBar.Value = progressPercent;
            ProgressTextBlock.Text = $"Progression: {progressPercent:F1}% ({transferredFiles}/{totalFiles} fichiers)";
            
            // Format transferred bytes
            string transferredFormatted = FormatBytes(transferredBytes);
            string totalFormatted = FormatBytes(totalBytes);
            TransferredTextBlock.Text = $"Transféré: {transferredFormatted} / {totalFormatted}";
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            System.Diagnostics.Debug.WriteLine($"BackupProgressWindow Error: {ex.Message}");
        }
    }

    private static string GetJsonString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetString() ?? "";
        }
        return "";
    }

    private static bool GetJsonBool(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetBoolean();
        }
        return false;
    }

    private static long GetJsonLong(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetInt64();
        }
        return 0;
    }

    private static int GetJsonInt(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetInt32();
        }
        return 0;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _updateTimer.Stop();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer.Stop();
        base.OnClosed(e);
    }
}
