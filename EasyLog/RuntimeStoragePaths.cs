public static class RuntimeStoragePaths
{
    public static string BackupStateDirectory=>GetBaseDirectory();
    public static string StateFilePath => Path.Combine(BackupStateDirectory, "state.json");

    public static string GetDailyLogFilePath(DateTime timestamp)
    {
        return Path.Combine(BackupStateDirectory, $"{timestamp:yyyy-MM-dd}.json");
    }

    public static string GetBaseDirectory()=> AppContext.BaseDirectory; 
 
}