public class ArgumentParser
{
    public List<int> ParseJobSelection(string selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
        {
            throw new ArgumentException("A job selection is required.");
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
                throw new ArgumentException("Invalid range format. Use values like 1-3.");
            }

            int start = ParseSingleIndex(rangeParts[0]);
            int end = ParseSingleIndex(rangeParts[1]);

            if (start > end)
            {
                throw new ArgumentException("Range start must be less than or equal to range end.");
            }

            return Enumerable.Range(start, end - start + 1).ToList();
        }

        return new List<int> { ParseSingleIndex(selection) };
    }

    private static int ParseSingleIndex(string value)
    {
        if (!int.TryParse(value, out int index) || index < 1 || index > 5)
        {
            throw new ArgumentException("Job numbers must be between 1 and 5.");
        }

        return index;
    }
}