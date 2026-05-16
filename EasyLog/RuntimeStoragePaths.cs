using System.Text.Json;

public static class RuntimeStoragePaths
{
    public const string JsonLogFileFormat = "json";
    public const string XmlLogFileFormat = "xml";
    public const string LocalLogStorageMode = "local";
    public const string CentralizedLogStorageMode = "centralized";
    public const string BothLogStorageMode = "both";
    public const string LightThemeMode = "Light";
    public const string DarkThemeMode = "Dark";
    public const string SystemThemeMode = "System";
    private const string SharedApplicationDirectoryName = "EasySave";
    private const string SharedStorageDirectoryName = "runtime";
    private const string ConfigurationDirectoryEnvironmentVariable = "EASYSAVE_CONFIGURATION_DIRECTORY";

    private static readonly object SyncRoot = new();
    private static RuntimeStorageSettings? _settings;

    public static string ConfigurationFilePath => Path.Combine(GetSharedConfigurationDirectory(), "storage-settings.json");
    public static string BackupStateDirectory => GetStorageDirectory();
    public static string JobsFilePath => Path.Combine(BackupStateDirectory, "jobs.json");
    public static string StateFilePath => Path.Combine(BackupStateDirectory, "state.json");
    public static string BackupHistoryFilePath => Path.Combine(BackupStateDirectory, "backup-history.json");
    public static string LogsDirectoryPath => BackupStateDirectory;
    public static string DefaultCryptoSoftExecutablePath => Path.Combine(GetBaseDirectory(), "CryptoSoft", "CryptoSoft.exe");

    public static string GetDailyLogFilePath(DateTime timestamp)
    {
        return Path.Combine(LogsDirectoryPath, $"{timestamp:yyyy-MM-dd}.{GetLogFileFormat()}");
    }

    public static IReadOnlyList<string> GetSupportedLogFilePatterns()
    {
        return
        [
            $"????-??-??.{JsonLogFileFormat}",
            $"????-??-??.{XmlLogFileFormat}",
            "????-??-??.jsonl"
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

    public static string GetLogStorageMode()
    {
        return NormalizeLogStorageMode(GetSettings().LogStorageMode);
    }

    public static string GetCentralLogServerUrl()
    {
        return NormalizeCentralLogServerUrl(GetSettings().CentralLogServerUrl);
    }

    public static string GetCentralLogUserName()
    {
        string configuredUserName = GetSettings().CentralLogUserName;
        return string.IsNullOrWhiteSpace(configuredUserName)
            ? Environment.UserName
            : configuredUserName.Trim();
    }

    public static string GetCentralLogApiKey()
    {
        return GetSettings().CentralLogApiKey?.Trim() ?? string.Empty;
    }

    public static string GetClientId()
    {
        lock (SyncRoot)
        {
            RuntimeStorageSettings settings = LoadSettings();
            if (!string.IsNullOrWhiteSpace(settings.ClientId))
            {
                _settings = settings;
                return settings.ClientId.Trim();
            }

            settings.ClientId = Guid.NewGuid().ToString("N");
            SaveSettings(settings);
            _settings = settings;
            return settings.ClientId;
        }
    }

    public static bool ShouldWriteLocalLogs()
    {
        string mode = GetLogStorageMode();
        return mode == LocalLogStorageMode || mode == BothLogStorageMode;
    }

    public static bool ShouldWriteCentralizedLogs()
    {
        string mode = GetLogStorageMode();
        return mode == CentralizedLogStorageMode || mode == BothLogStorageMode;
    }

    public static string GetThemeMode()
    {
        return NormalizeThemeMode(GetSettings().ThemeMode);
    }

    public static IReadOnlyList<string> GetEncryptedExtensions()
    {
        return NormalizeEncryptedExtensions(GetSettings().EncryptedExtensions);
    }

    public static IReadOnlyList<string> GetPriorityExtensions()
    {
        return NormalizeExtensionsPreservingOrder(GetSettings().PriorityExtensions);
    }

    public static string GetCryptoSoftKey()
    {
        return GetSettings().CryptoSoftKey ?? string.Empty;
    }

    public static int GetLargeFileThresholdKb()
    {
        return NormalizeLargeFileThresholdKb(GetSettings().LargeFileThresholdKb);
    }

    public static int GetMaxConcurrentJobs()
    {
        return NormalizeMaxConcurrentJobs(GetSettings().MaxConcurrentJobs);
    }

    public static string GetCryptoSoftPath()
    {
        string configuredPath = GetSettings().CryptoSoftPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return DefaultCryptoSoftExecutablePath;
        }

        return ResolveFilePath(configuredPath);
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

    public static void SetLogStorageMode(string logStorageMode)
    {
        string normalizedLogStorageMode = NormalizeLogStorageMode(logStorageMode);
        UpdateSettings(settings => settings.LogStorageMode = normalizedLogStorageMode);
    }

    public static void SetCentralLogServerUrl(string serverUrl)
    {
        string normalizedServerUrl = NormalizeCentralLogServerUrl(serverUrl);
        UpdateSettings(settings => settings.CentralLogServerUrl = normalizedServerUrl);
    }

    public static void SetCentralLogUserName(string userName)
    {
        UpdateSettings(settings => settings.CentralLogUserName = userName?.Trim() ?? string.Empty);
    }

    public static void SetCentralLogApiKey(string apiKey)
    {
        UpdateSettings(settings => settings.CentralLogApiKey = apiKey?.Trim() ?? string.Empty);
    }

    public static void SetThemeMode(string themeMode)
    {
        string normalizedThemeMode = NormalizeThemeMode(themeMode);
        UpdateSettings(settings => settings.ThemeMode = normalizedThemeMode);
    }

    public static void SetEncryptedExtensions(IEnumerable<string> extensions)
    {
        List<string> normalizedExtensions = NormalizeEncryptedExtensions(extensions).ToList();
        UpdateSettings(settings => settings.EncryptedExtensions = normalizedExtensions);
    }

    public static void SetPriorityExtensions(IEnumerable<string> extensions)
    {
        List<string> normalizedExtensions = NormalizeExtensionsPreservingOrder(extensions).ToList();
        UpdateSettings(settings => settings.PriorityExtensions = normalizedExtensions);
    }

    public static void SetCryptoSoftKey(string key)
    {
        UpdateSettings(settings => settings.CryptoSoftKey = key?.Trim() ?? string.Empty);
    }

    public static void SetLargeFileThresholdKb(int thresholdKb)
    {
        int normalizedThresholdKb = NormalizeLargeFileThresholdKb(thresholdKb);
        UpdateSettings(settings => settings.LargeFileThresholdKb = normalizedThresholdKb);
    }

    public static void SetMaxConcurrentJobs(int maxConcurrentJobs)
    {
        int normalizedMaxConcurrentJobs = NormalizeMaxConcurrentJobs(maxConcurrentJobs);
        UpdateSettings(settings => settings.MaxConcurrentJobs = normalizedMaxConcurrentJobs);
    }

    public static void SetCryptoSoftPath(string path)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : ResolveFilePath(path);
        UpdateSettings(settings => settings.CryptoSoftPath = normalizedPath);
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

        return EnsureSettingsDefaults(settings);
    }

    private static RuntimeStorageSettings EnsureSettingsDefaults(RuntimeStorageSettings? settings)
    {
        settings ??= new RuntimeStorageSettings();
        settings.StorageDirectory ??= string.Empty;
        settings.LanguageCode ??= string.Empty;
        settings.LogFileFormat ??= string.Empty;
        settings.LogStorageMode ??= string.Empty;
        settings.CentralLogServerUrl ??= string.Empty;
        settings.CentralLogUserName ??= string.Empty;
        settings.CentralLogApiKey ??= string.Empty;
        settings.ClientId ??= string.Empty;
        settings.EncryptedExtensions ??= new List<string>();
        settings.PriorityExtensions ??= new List<string>();
        settings.CryptoSoftKey ??= string.Empty;
        settings.CryptoSoftPath ??= string.Empty;
        settings.BlockedProcessNames ??= new List<string>();
        settings.ThemeMode ??= string.Empty;
        settings.LargeFileThresholdKb = NormalizeLargeFileThresholdKb(settings.LargeFileThresholdKb);
        settings.MaxConcurrentJobs = NormalizeMaxConcurrentJobs(settings.MaxConcurrentJobs);
        return settings;
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

            SaveSettings(settings);
            _settings = settings;
        }
    }

    private static void SaveSettings(RuntimeStorageSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigurationFilePath, json);
    }

    private static string ResolveDirectoryPath(string path)
    {
        string trimmedPath = path.Trim();
        string resolvedPath = Path.IsPathRooted(trimmedPath)
            ? trimmedPath
            : Path.Combine(GetBaseDirectory(), trimmedPath);

        return Path.GetFullPath(resolvedPath);
    }

    private static string ResolveFilePath(string path)
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

    private static string NormalizeLogStorageMode(string? logStorageMode)
    {
        string normalizedLogStorageMode = string.IsNullOrWhiteSpace(logStorageMode)
            ? LocalLogStorageMode
            : logStorageMode.Trim().ToLowerInvariant();

        return normalizedLogStorageMode switch
        {
            CentralizedLogStorageMode => CentralizedLogStorageMode,
            BothLogStorageMode => BothLogStorageMode,
            _ => LocalLogStorageMode
        };
    }

    private static string NormalizeCentralLogServerUrl(string? serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            return string.Empty;
        }

        return serverUrl.Trim().TrimEnd('/');
    }

    private static string NormalizeThemeMode(string? themeMode)
    {
        if (string.IsNullOrWhiteSpace(themeMode))
        {
            return DarkThemeMode;
        }

        string normalizedThemeMode = themeMode.Trim();
        if (normalizedThemeMode.Equals(DarkThemeMode, StringComparison.OrdinalIgnoreCase))
        {
            return DarkThemeMode;
        }

        if (normalizedThemeMode.Equals(LightThemeMode, StringComparison.OrdinalIgnoreCase))
        {
            return LightThemeMode;
        }

        return SystemThemeMode;
    }

    private static IReadOnlyList<string> NormalizeEncryptedExtensions(IEnumerable<string>? extensions)
    {
        return NormalizeExtensions(extensions);
    }

    private static IReadOnlyList<string> NormalizeExtensionsPreservingOrder(IEnumerable<string>? extensions)
    {
        if (extensions is null)
        {
            return Array.Empty<string>();
        }

        List<string> normalized = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string extension in extensions
            .SelectMany(extension => (extension ?? string.Empty)
                .Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeExtension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension)))
        {
            if (seen.Add(extension))
            {
                normalized.Add(extension);
            }
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeExtensions(IEnumerable<string>? extensions)
    {
        if (extensions is null)
        {
            return Array.Empty<string>();
        }

        return extensions
            .SelectMany(extension => (extension ?? string.Empty)
                .Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeExtension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int NormalizeLargeFileThresholdKb(int thresholdKb)
    {
        return thresholdKb < 0 ? 0 : thresholdKb;
    }

    private static int NormalizeMaxConcurrentJobs(int maxConcurrentJobs)
    {
        return maxConcurrentJobs <= 0 ? 2 : maxConcurrentJobs;
    }

    private static string NormalizeExtension(string extension)
    {
        string trimmedExtension = extension.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(trimmedExtension))
        {
            return string.Empty;
        }

        return trimmedExtension.StartsWith('.')
            ? trimmedExtension
            : $".{trimmedExtension}";
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
        string? configuredRootDirectory = Environment.GetEnvironmentVariable(ConfigurationDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRootDirectory))
        {
            string resolvedConfigurationDirectory = Path.GetFullPath(configuredRootDirectory);
            Directory.CreateDirectory(resolvedConfigurationDirectory);
            return resolvedConfigurationDirectory;
        }

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
