using System.Diagnostics;

public class CryptoSoftService : ICryptoService
{
    public const long ProcessTimeoutErrorCode = -93;
    public const long InvalidKeyErrorCode = -94;
    public const long InvalidArgumentsErrorCode = -95;
    public const long BusyTimeoutErrorCode = -96;
    public const long CryptoSoftMissingErrorCode = -97;
    public const long MissingKeyErrorCode = -98;
    public const long LaunchErrorCode = -99;
    private const int CryptoSoftProcessTimeoutMilliseconds = 10 * 60 * 1000;

    public long EncryptIfRequired(string filePath)
    {
        if (!ShouldEncrypt(filePath))
        {
            return 0;
        }

        string key = RuntimeStoragePaths.GetCryptoSoftKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            return MissingKeyErrorCode;
        }

        string cryptoSoftPath = RuntimeStoragePaths.GetCryptoSoftPath();
        if (!File.Exists(cryptoSoftPath))
        {
            return CryptoSoftMissingErrorCode;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cryptoSoftPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(filePath);
            startInfo.ArgumentList.Add(key);

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return LaunchErrorCode;
            }

            if (!process.WaitForExit(CryptoSoftProcessTimeoutMilliseconds))
            {
                TryKillProcess(process);
                return ProcessTimeoutErrorCode;
            }

            long exitCode = process.ExitCode;

            if (exitCode < 0)
            {
                return exitCode;
            }

            return Math.Max(1, exitCode);
        }
        catch
        {
            return LaunchErrorCode;
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch
        {
        }
    }

    private static bool ShouldEncrypt(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return RuntimeStoragePaths.GetEncryptedExtensions()
            .Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

}
