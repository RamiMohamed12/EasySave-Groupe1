public class ArgumentParser
{
    private readonly ApplicationTextService _textService;

    public ArgumentParser(ApplicationTextService textService)
    {
        _textService = textService;
    }

    public CliCommand Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new CliCommand
            {
                Type = CliCommandType.ShowJobs
            };
        }

        if (args.Length != 1)
        {
            throw new ArgumentException(_textService.GetSingleArgumentExpectedMessage());
        }

        if (IsHelpArgument(args[0]))
        {
            return new CliCommand
            {
                Type = CliCommandType.ShowHelp
            };
        }

        return new CliCommand
        {
            Type = CliCommandType.RunSelection,
            SelectedJobNumbers = ParseJobSelection(args[0])
        };
    }

    public List<int> ParseJobSelection(string selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
        {
            throw new ArgumentException(_textService.GetSelectionRequiredMessage());
        }

        if (selection.Contains(';'))
        {
            return selection
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseSingleIndex)
                .Distinct()
                .OrderBy(index => index)
                .ToList();
        }

        if (selection.Contains('-'))
        {
            string[] rangeParts = selection.Split('-', StringSplitOptions.TrimEntries);

            if (rangeParts.Length != 2)
            {
                throw new ArgumentException(_textService.GetInvalidRangeFormatMessage());
            }

            int start = ParseSingleIndex(rangeParts[0]);
            int end = ParseSingleIndex(rangeParts[1]);

            if (start > end)
            {
                throw new ArgumentException(_textService.GetInvalidRangeOrderMessage());
            }

            return Enumerable.Range(start, end - start + 1).ToList();
        }

        return new List<int> { ParseSingleIndex(selection) };
    }

    private int ParseSingleIndex(string value)
    {
        if (!int.TryParse(value, out int index) || index < 1 || index > 5)
        {
            throw new ArgumentException(_textService.GetInvalidJobNumberMessage());
        }

        return index;
    }

    private static bool IsHelpArgument(string argument)
    {
        return argument.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("-h", StringComparison.OrdinalIgnoreCase);
    }
}
