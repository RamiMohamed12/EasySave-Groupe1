public class ArgumentParser
{
    private readonly ApplicationTextService _textService;

    public ArgumentParser(ApplicationTextService textService)
    {
        _textService = textService;
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
}
