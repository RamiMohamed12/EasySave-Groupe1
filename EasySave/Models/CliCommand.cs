public class CliCommand
{
    public CliCommandType Type { get; set; }
    public IReadOnlyList<int> SelectedJobNumbers { get; set; }

    public CliCommand()
    {
        Type = CliCommandType.ShowJobs;
        SelectedJobNumbers = Array.Empty<int>();
    }
}
