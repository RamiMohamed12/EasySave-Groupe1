public class CliCommand
{
    public CliCommandType Type { get; set; }
    public IReadOnlyList<int> SelectedJobNumbers { get; set; }
    public int JobNumber { get; set; }
    public JobPathField? PathField { get; set; }
    public string PathValue { get; set; }
    public string LanguageCode { get; set; }

    public CliCommand()
    {
        Type = CliCommandType.ShowJobs;
        SelectedJobNumbers = Array.Empty<int>();
        JobNumber = 0;
        PathField = null;
        PathValue = string.Empty;
        LanguageCode = string.Empty;
    }
}
