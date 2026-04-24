namespace EasySave.Tests;

public sealed class TestWorkspace : IDisposable
{
    private readonly bool _hadExistingStorageSettings;
    private readonly string? _existingStorageSettingsContent;

    public string RootPath { get; }
    public string RuntimePath { get; }

    public TestWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "EasySaveTests", Guid.NewGuid().ToString("N"));
        RuntimePath = Path.Combine(RootPath, "runtime");
        Directory.CreateDirectory(RuntimePath);

        _hadExistingStorageSettings = File.Exists(RuntimeStoragePaths.ConfigurationFilePath);
        _existingStorageSettingsContent = _hadExistingStorageSettings
            ? File.ReadAllText(RuntimeStoragePaths.ConfigurationFilePath)
            : null;

        RuntimeStoragePaths.SetStorageDirectory(RuntimePath);
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

        RuntimeStoragePaths.Reload();

        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }
}
