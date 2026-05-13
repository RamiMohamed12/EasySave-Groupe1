public class RuntimeStorageSettings
{
    public string StorageDirectory { get; set; }
    public string LanguageCode { get; set; }
    public string LogFileFormat { get; set; }
    public List<string> EncryptedExtensions { get; set; }
    public List<string> PriorityExtensions { get; set; }
    public string CryptoSoftKey { get; set; }
    public string CryptoSoftPath { get; set; }
    public List<string> BlockedProcessNames { get; set; }
    public string ThemeMode { get; set; }
    public int LargeFileThresholdKb { get; set; }
    public int MaxConcurrentJobs { get; set; }

    public RuntimeStorageSettings()
    {
        StorageDirectory = string.Empty;
        LanguageCode = string.Empty;
        LogFileFormat = string.Empty;
        EncryptedExtensions = new List<string>();
        PriorityExtensions = new List<string>();
        CryptoSoftKey = string.Empty;
        CryptoSoftPath = string.Empty;
        BlockedProcessNames = new List<string>();
        ThemeMode = string.Empty;
        LargeFileThresholdKb = 0;
        MaxConcurrentJobs = 2;
    }
}
