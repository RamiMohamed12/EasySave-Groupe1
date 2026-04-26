
public class BackupJob
{
    public string Name {get; set;}
    public string Source {get; set;}
    public string Target {get; set;}
    public BackupType Type {get; set;}
    
    public BackupJob()
    {
        Name = string.Empty;
        Source = string.Empty;
        Target = string.Empty;
        Type = BackupType.Full;
    }
    
}