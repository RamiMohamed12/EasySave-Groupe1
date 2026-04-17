using System.Runtime.InteropServices;

public static class RuntimeStoragePaths
{
    public static string BackupStateDirectory
    {
        get
        {
            string baseDirectory = GetBaseDirectory();
            string backupStateDirectory = Path.Combine(baseDirectory, "BackupState");

            Directory.CreateDirectory(backupStateDirectory);
            return backupStateDirectory;
        }
    }

    public static string StateFilePath => Path.Combine(BackupStateDirectory, "state.json");

    public static string GetDailyLogFilePath(DateTime timestamp)
    {
        return Path.Combine(BackupStateDirectory, $"{timestamp:yyyy-MM-dd}.json");
    }

    private static string GetBaseDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
            return systemDrive + Path.DirectorySeparatorChar;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}