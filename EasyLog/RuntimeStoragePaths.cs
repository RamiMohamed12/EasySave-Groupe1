using System.Text.Json;

public static class RuntimeStoragePaths
{
    public const string JsonLogFileFormat = "json";
    public const string XmlLogFileFormat = "xml";
    private const string SharedApplicationDirectoryName = "EasySave";
    private const string SharedStorageDirectoryName = "runtime";

    private static readonly object SyncRoot = new();
    private static RuntimeStorageSettings? _settings;

    public static string ConfigurationFilePath => Path.Combine(GetSharedConfigurationDirectory(), "storage-settings.json");
    public static string BackupStateDirectory => GetStorageDirectory();
    public static string JobsFilePath => Path.Combine(BackupStateDirectory, "jobs.json");
    public static string StateFilePath => Path.Combine(BackupStateDirectory, "state.json");
    public static string BackupHistoryFilePath => Path.Combine(BackupStateDirectory, "backup-history.json");
    public static string LogsDirectoryPath => BackupStateDirectory;

    public static string GetDailyLogFilePath(DateTime timestamp)
    {
        return Path.Combine(LogsDirectoryPath, $"{timestamp:yyyy-MM-dd}.{GetLogFileFormat()}");
    }

    public static IReadOnlyList<string> GetSupportedLogFilePatterns()
    {
        return
        [
            $"????-??-??.{JsonLogFileFormat}",
            $"????-??-??.{XmlLogFileFormat}"
        ];
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

    public static string GetLogFileFormat()
    {
        return NormalizeLogFileFormat(GetSettings().LogFileFormat);
    }

    public static void SetLanguageCode(string languageCode)
    {
        string normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        UpdateSettings(settings => settings.LanguageCode = normalizedLanguageCode);
    }

    public static void SetLogFileFormat(string logFileFormat)
    {
        string normalizedLogFileFormat = NormalizeLogFileFormat(logFileFormat);
        UpdateSettings(settings => settings.LogFileFormat = normalizedLogFileFormat);
    }

    public static IReadOnlyList<string> GetBlockedProcessNames()
    {
        return GetSettings()
            .BlockedProcessNames
            .Select(NormalizeProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void SetBlockedProcessNames(IEnumerable<string> processNames)
    {
        var normalizedNames = processNames
            .Select(NormalizeProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        UpdateSettings(settings => settings.BlockedProcessNames = normalizedNames);
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
            ? GetDefaultSharedStorageDirectory()
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

    private static string NormalizeLogFileFormat(string? logFileFormat)
    {
        string normalizedLogFileFormat = string.IsNullOrWhiteSpace(logFileFormat)
            ? JsonLogFileFormat
            : logFileFormat.Trim().ToLowerInvariant();

        return normalizedLogFileFormat == XmlLogFileFormat
            ? XmlLogFileFormat
            : JsonLogFileFormat;
    }

    private static string NormalizeProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return string.Empty;
        }

        string normalized = processName.Trim().ToLowerInvariant();
        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }

    private static string GetSharedConfigurationDirectory()
    {
        string localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string rootDirectory = string.IsNullOrWhiteSpace(localApplicationDataPath)
            ? GetBaseDirectory()
            : localApplicationDataPath;
        string configurationDirectory = Path.Combine(rootDirectory, SharedApplicationDirectoryName);
        Directory.CreateDirectory(configurationDirectory);
        return configurationDirectory;
    }

    private static string GetDefaultSharedStorageDirectory()
    {
        string storageDirectory = Path.Combine(GetSharedConfigurationDirectory(), SharedStorageDirectoryName);
        Directory.CreateDirectory(storageDirectory);
        return storageDirectory;
    }
}
