public class RuntimeStorageSettings
{
    public string StorageDirectory { get; set; }
    public string LanguageCode { get; set; }
    public string LogFileFormat { get; set; }
    public List<string> EncryptedExtensions { get; set; }
    public string CryptoSoftKey { get; set; }
    public string CryptoSoftPath { get; set; }

    public RuntimeStorageSettings()
    {
        StorageDirectory = string.Empty;
        LanguageCode = string.Empty;
        LogFileFormat = string.Empty;
        EncryptedExtensions = new List<string>();
        CryptoSoftKey = string.Empty;
        CryptoSoftPath = string.Empty;
    }
}
