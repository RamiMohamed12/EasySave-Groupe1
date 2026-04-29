namespace EasySave.Wpf;

public sealed class JobRow
{
    public bool IsSelected { get; set; }
    public int JobNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
}
