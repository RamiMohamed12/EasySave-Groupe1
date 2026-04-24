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
            if (IsConfigureArgument(args))
            {
                return ParseConfigureCommand(args);
            }

            if (IsStorageDirectoryArgument(args))
            {
                return ParseStorageDirectoryCommand(args);
            }

            throw new ArgumentException(_textService.GetInvalidCommandMessage());
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

    private CliCommand ParseConfigureCommand(string[] args)
    {
        if (args.Length != 4)
        {
            throw new ArgumentException(_textService.GetInvalidConfigureCommandMessage());
        }

        int jobNumber = ParseSingleIndex(args[1]);
        JobPathField pathField = ParsePathField(args[2]);
        string pathValue = args[3]?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(pathValue))
        {
            throw new ArgumentException(_textService.GetPathValueRequiredMessage());
        }

        return new CliCommand
        {
            Type = CliCommandType.ConfigureJobPath,
            JobNumber = jobNumber,
            PathField = pathField,
            PathValue = pathValue
        };
    }

    private JobPathField ParsePathField(string value)
    {
        if (value.Equals("source", StringComparison.OrdinalIgnoreCase))
        {
            return JobPathField.Source;
        }

        if (value.Equals("target", StringComparison.OrdinalIgnoreCase))
        {
            return JobPathField.Target;
        }

        throw new ArgumentException(_textService.GetInvalidConfigureFieldMessage());
    }

    private static bool IsConfigureArgument(string[] args)
    {
        return args.Length > 0
            && args[0].Equals("--configure", StringComparison.OrdinalIgnoreCase);
    }

    private CliCommand ParseStorageDirectoryCommand(string[] args)
    {
        if (args.Length != 2)
        {
            throw new ArgumentException(_textService.GetInvalidStorageDirectoryCommandMessage());
        }

        string pathValue = args[1]?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(pathValue))
        {
            throw new ArgumentException(_textService.GetPathValueRequiredMessage());
        }

        return new CliCommand
        {
            Type = CliCommandType.ConfigureStorageDirectory,
            PathValue = pathValue
        };
    }

    private static bool IsStorageDirectoryArgument(string[] args)
    {
        return args.Length > 0
            && args[0].Equals("--storage-dir", StringComparison.OrdinalIgnoreCase);
    }
}
