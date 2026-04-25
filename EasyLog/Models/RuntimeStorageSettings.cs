public class RuntimeStorageSettings
{
    public string StorageDirectory { get; set; }
    public string LanguageCode { get; set; }

    public RuntimeStorageSettings()
    {
        StorageDirectory = string.Empty;
        LanguageCode = string.Empty;
    }
}
