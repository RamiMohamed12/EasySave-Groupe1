using System.Diagnostics;

public class CryptoSoftService : ICryptoService
{
    public const long CryptoSoftMissingErrorCode = -97;
    public const long MissingKeyErrorCode = -98;
    public const long LaunchErrorCode = -99;

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
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add(filePath);
            startInfo.ArgumentList.Add(key);

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return LaunchErrorCode;
            }

            process.WaitForExit();
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
