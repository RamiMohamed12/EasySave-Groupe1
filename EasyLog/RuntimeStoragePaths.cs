using System.Text.Json;

public static class RuntimeStoragePaths
{
    private static readonly object SyncRoot = new();
    private static RuntimeStorageSettings? _settings;

    public static string ConfigurationFilePath => Path.Combine(GetBaseDirectory(), "storage-settings.json");
    public static string BackupStateDirectory => GetStorageDirectory();
    public static string JobsFilePath => Path.Combine(BackupStateDirectory, "jobs.json");
    public static string StateFilePath => Path.Combine(BackupStateDirectory, "state.json");
    public static string BackupHistoryFilePath => Path.Combine(BackupStateDirectory, "backup-history.json");
    public static string LogsDirectoryPath => BackupStateDirectory;

    public static string GetDailyLogFilePath(DateTime timestamp)
    {
        return Path.Combine(LogsDirectoryPath, $"{timestamp:yyyy-MM-dd}.json");
    }

    public static void SetStorageDirectory(string storageDirectory)
    {
        string resolvedStorageDirectory = ResolveDirectoryPath(storageDirectory);
        UpdateSettings(settings => settings.StorageDirectory = resolvedStorageDirectory);
    }

    public static string GetLanguageCode()
    {
        return NormalizeLanguageCode(GetSettings().LanguageCode);
    }

    public static void SetLanguageCode(string languageCode)
    {
        string normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        UpdateSettings(settings => settings.LanguageCode = normalizedLanguageCode);
    }

    public static void Reload()
    {
        lock (SyncRoot)
        {
            _settings = null;
        }
    }

    public static string GetBaseDirectory()
    {
        return AppContext.BaseDirectory;
    }

    private static RuntimeStorageSettings GetSettings()
    {
        lock (SyncRoot)
        {
            _settings ??= LoadSettings();
            return _settings;
        }
    }

    private static RuntimeStorageSettings LoadSettings()
    {
        if (!File.Exists(ConfigurationFilePath))
        {
            return new RuntimeStorageSettings();
        }

        string json = File.ReadAllText(ConfigurationFilePath);
        RuntimeStorageSettings? settings = JsonSerializer.Deserialize<RuntimeStorageSettings>(json);

        return settings ?? new RuntimeStorageSettings();
    }

    private static string GetStorageDirectory()
    {
        string configuredDirectory = GetSettings().StorageDirectory;
        string storageDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? GetBaseDirectory()
            : ResolveDirectoryPath(configuredDirectory);

        Directory.CreateDirectory(storageDirectory);
        return storageDirectory;
    }

    private static void UpdateSettings(Action<RuntimeStorageSettings> updateSettings)
    {
        lock (SyncRoot)
        {
            RuntimeStorageSettings settings = LoadSettings();
            updateSettings(settings);

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigurationFilePath, json);
            _settings = settings;
        }
    }

    private static string ResolveDirectoryPath(string path)
    {
        string trimmedPath = path.Trim();
        string resolvedPath = Path.IsPathRooted(trimmedPath)
            ? trimmedPath
            : Path.Combine(GetBaseDirectory(), trimmedPath);

        return Path.GetFullPath(resolvedPath);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? string.Empty
            : languageCode.Trim().ToLowerInvariant();
    }
}
