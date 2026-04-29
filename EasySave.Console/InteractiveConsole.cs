public sealed class InteractiveConsole
{
    public int? SelectOption(
        string title,
        IReadOnlyList<string> options,
        IReadOnlyList<string>? contextLines,
        string helpText,
        bool allowBack = true,
        int initialIndex = 0)
    {
        if (options.Count == 0)
        {
            throw new ArgumentException("At least one option is required.", nameof(options));
        }

        return WithHiddenCursor(() =>
        {
            int selectedIndex = Math.Clamp(initialIndex, 0, options.Count - 1);

            while (true)
            {
                RenderMenu(title, contextLines, options, helpText, selectedIndex);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex == 0 ? options.Count - 1 : selectedIndex - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex == options.Count - 1 ? 0 : selectedIndex + 1;
                        break;
                    case ConsoleKey.Enter:
                        return (int?)selectedIndex;
                    case ConsoleKey.Escape when allowBack:
                        return null;
                }
            }
        });
    }

    public IReadOnlyList<int>? SelectMultipleOptions(
        string title,
        IReadOnlyList<string> options,
        IReadOnlyList<string>? contextLines,
        string helpText,
        string emptySelectionError)
    {
        if (options.Count == 0)
        {
            throw new ArgumentException("At least one option is required.", nameof(options));
        }

        return WithHiddenCursor(() =>
        {
            int selectedIndex = 0;
            string? errorMessage = null;
            var selectedIndices = new HashSet<int>();

            while (true)
            {
                RenderMultiSelectMenu(title, contextLines, options, helpText, selectedIndex, selectedIndices, errorMessage);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex == 0 ? options.Count - 1 : selectedIndex - 1;
                        errorMessage = null;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex == options.Count - 1 ? 0 : selectedIndex + 1;
                        errorMessage = null;
                        break;
                    case ConsoleKey.Spacebar:
                        if (!selectedIndices.Add(selectedIndex))
                        {
                            selectedIndices.Remove(selectedIndex);
                        }

                        errorMessage = null;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndices.Count == 0)
                        {
                            errorMessage = emptySelectionError;
                            break;
                        }

                        return selectedIndices.OrderBy(index => index).ToArray();
                    case ConsoleKey.Escape:
                        return null;
                }
            }
        });
    }

    public string? PromptLine(
        string title,
        string prompt,
        IReadOnlyList<string>? contextLines,
        string helpText,
        Func<string, string?>? validate = null)
    {
        string? errorMessage = null;

        while (true)
        {
            Console.Clear();
            WriteSectionHeader(title);
            WriteContextLines(contextLines);
            Console.WriteLine(helpText);
            Console.WriteLine();

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                WriteError(errorMessage);
                Console.WriteLine();
            }

            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            string? validationError = validate?.Invoke(input);
            if (string.IsNullOrWhiteSpace(validationError))
            {
                return input;
            }

            errorMessage = validationError;
        }
    }

    private static T WithHiddenCursor<T>(Func<T> action)
    {
        bool originalVisibility = false;
        bool canRestoreCursor = true;

        try
        {
            originalVisibility = Console.CursorVisible;
            Console.CursorVisible = false;
        }
        catch
        {
            canRestoreCursor = false;
        }

        try
        {
            return action();
        }
        finally
        {
            if (canRestoreCursor)
            {
                try
                {
                    Console.CursorVisible = originalVisibility;
                }
                catch
                {
                    // Ignore cursor restoration issues when the host does not support it.
                }
            }
        }
    }

    private static void RenderMenu(
        string title,
        IReadOnlyList<string>? contextLines,
        IReadOnlyList<string> options,
        string helpText,
        int selectedIndex)
    {
        Console.Clear();
        WriteSectionHeader(title);
        WriteContextLines(contextLines);

        for (int index = 0; index < options.Count; index++)
        {
            string prefix = index == selectedIndex ? "> " : "  ";
            Console.WriteLine($"{prefix}{options[index]}");
        }

        Console.WriteLine();
        Console.WriteLine(helpText);
    }

    private static void RenderMultiSelectMenu(
        string title,
        IReadOnlyList<string>? contextLines,
        IReadOnlyList<string> options,
        string helpText,
        int selectedIndex,
        IReadOnlySet<int> selectedIndices,
        string? errorMessage)
    {
        Console.Clear();
        WriteSectionHeader(title);
        WriteContextLines(contextLines);

        for (int index = 0; index < options.Count; index++)
        {
            string pointer = index == selectedIndex ? "> " : "  ";
            string marker = selectedIndices.Contains(index) ? "[x]" : "[ ]";
            Console.WriteLine($"{pointer}{marker} {options[index]}");
        }

        Console.WriteLine();
        Console.WriteLine(helpText);

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            Console.WriteLine();
            WriteError(errorMessage);
        }
    }

    private static void WriteContextLines(IReadOnlyList<string>? contextLines)
    {
        if (contextLines == null || contextLines.Count == 0)
        {
            return;
        }

        foreach (string line in contextLines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            Console.WriteLine(line);
        }

        Console.WriteLine();
    }

    private static void WriteSectionHeader(string title)
    {
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine();
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
