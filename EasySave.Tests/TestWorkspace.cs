namespace EasySave.Tests;

public sealed class TestWorkspace : IDisposable
{
    private readonly bool _hadExistingStorageSettings;
    private readonly string? _existingStorageSettingsContent;
    private readonly string? _existingConfigurationDirectoryOverride;

    public string RootPath { get; }
    public string RuntimePath { get; }

    public TestWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "EasySaveTests", Guid.NewGuid().ToString("N"));
        RuntimePath = Path.Combine(RootPath, "runtime");
        Directory.CreateDirectory(RuntimePath);
        string configurationPath = Path.Combine(RootPath, "config");
        Directory.CreateDirectory(configurationPath);

        _existingConfigurationDirectoryOverride = Environment.GetEnvironmentVariable("EASYSAVE_CONFIGURATION_DIRECTORY");
        Environment.SetEnvironmentVariable("EASYSAVE_CONFIGURATION_DIRECTORY", configurationPath);
        RuntimeStoragePaths.Reload();

        _hadExistingStorageSettings = File.Exists(RuntimeStoragePaths.ConfigurationFilePath);
        _existingStorageSettingsContent = _hadExistingStorageSettings
            ? File.ReadAllText(RuntimeStoragePaths.ConfigurationFilePath)
            : null;

        RuntimeStoragePaths.SetStorageDirectory(RuntimePath);
        RuntimeStoragePaths.SetBlockedProcessNames(Array.Empty<string>());
        RuntimeStoragePaths.SetLanguageCode(string.Empty);
        RuntimeStoragePaths.SetLogFileFormat(RuntimeStoragePaths.JsonLogFileFormat);
        RuntimeStoragePaths.SetLogStorageMode(RuntimeStoragePaths.LocalLogStorageMode);
        RuntimeStoragePaths.SetCentralLogServerUrl(string.Empty);
        RuntimeStoragePaths.SetCentralLogUserName(string.Empty);
        RuntimeStoragePaths.SetCentralLogApiKey(string.Empty);
    }

    public string GetPath(params string[] parts)
    {
        return parts.Aggregate(RootPath, Path.Combine);
    }

    public string CreateDirectory(params string[] parts)
    {
        string path = GetPath(parts);
        Directory.CreateDirectory(path);
        return path;
    }

    public string CreateFile(string relativePath, string content)
    {
        string fullPath = GetPath(relativePath);
        string? directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        if (_hadExistingStorageSettings && _existingStorageSettingsContent is not null)
        {
            File.WriteAllText(RuntimeStoragePaths.ConfigurationFilePath, _existingStorageSettingsContent);
        }
        else if (File.Exists(RuntimeStoragePaths.ConfigurationFilePath))
        {
            File.Delete(RuntimeStoragePaths.ConfigurationFilePath);
        }

        Environment.SetEnvironmentVariable("EASYSAVE_CONFIGURATION_DIRECTORY", _existingConfigurationDirectoryOverride);
        RuntimeStoragePaths.Reload();

        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }
}
