
public class BackupJob
{
    public string Id { get; set; }
    public string Name {get; set;}
    public string Source {get; set;}
    public string Target {get; set;}
    public BackupType Type {get; set;}
    
    public BackupJob()
    {
        Id = string.Empty;
        Name = string.Empty;
        Source = string.Empty;
        Target = string.Empty;
        Type = BackupType.Full;
    }
    
}
