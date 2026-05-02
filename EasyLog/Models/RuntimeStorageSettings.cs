public class RuntimeStorageSettings
{
    public string StorageDirectory { get; set; }
    public string LanguageCode { get; set; }
    public string LogFileFormat { get; set; }
    public List<string> BlockedProcessNames { get; set; }

    public RuntimeStorageSettings()
    {
        StorageDirectory = string.Empty;
        LanguageCode = string.Empty;
        LogFileFormat = string.Empty;
        BlockedProcessNames = new List<string>();
    }
}
