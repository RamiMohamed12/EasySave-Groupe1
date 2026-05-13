using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class AtomicRuntimeFileStore
{
    private const int RetryCount = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    public T ReadJson<T>(string path, JsonSerializerOptions options, Func<T> defaultFactory)
    {
        string? json = ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return defaultFactory();
        }

        T? value = JsonSerializer.Deserialize<T>(json, options);
        return value is null ? defaultFactory() : value;
    }

    public void WriteJson<T>(string path, T value, JsonSerializerOptions options)
    {
        string json = JsonSerializer.Serialize(value, options);
        WriteAllTextAtomic(path, json);
    }

    public string? ReadAllText(string path)
    {
        return ExecuteWithRetry(() =>
            WithFileLock(path, () =>
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                return File.ReadAllText(path);
            }));
    }

    public void WriteAllTextAtomic(string path, string content)
    {
        ExecuteWithRetry(() =>
            WithFileLock(path, () =>
            {
                string directory = Path.GetDirectoryName(path) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = Path.Combine(
                    string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory,
                    $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

                try
                {
                    File.WriteAllText(tempPath, content, Encoding.UTF8);

                    if (File.Exists(path))
                    {
                        File.Replace(tempPath, path, null, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tempPath, path);
                    }
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }));
    }

    private static T ExecuteWithRetry<T>(Func<T> action)
    {
        int attempt = 0;

        while (true)
        {
            try
            {
                return action();
            }
            catch (IOException) when (attempt < RetryCount - 1)
            {
                attempt++;
                Thread.Sleep(RetryDelay);
            }
            catch (UnauthorizedAccessException) when (attempt < RetryCount - 1)
            {
                attempt++;
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static void ExecuteWithRetry(Action action)
    {
        ExecuteWithRetry(() =>
        {
            action();
            return true;
        });
    }

    private static T WithFileLock<T>(string path, Func<T> action)
    {
        using var mutex = CreateMutex(path);
        bool lockTaken = false;

        try
        {
            lockTaken = mutex.WaitOne(TimeSpan.FromSeconds(5));
            if (!lockTaken)
            {
                throw new IOException($"Timed out waiting for file lock on '{path}'.");
            }

            return action();
        }
        finally
        {
            if (lockTaken)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static void WithFileLock(string path, Action action)
    {
        WithFileLock(path, () =>
        {
            action();
            return true;
        });
    }

    private static Mutex CreateMutex(string path)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToLowerInvariant()));
        string mutexName = $"Local\\EasySave_{Convert.ToHexString(hash)}";
        return new Mutex(initiallyOwned: false, mutexName);
    }
}
