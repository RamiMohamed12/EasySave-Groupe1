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
            int? menuTop = null;
            int lastSelectedIndex = selectedIndex;

            while (true)
            {
                menuTop ??= RenderMenu(title, contextLines, options, helpText, selectedIndex);
                if (lastSelectedIndex != selectedIndex)
                {
                    UpdateMenuSelection(menuTop.Value, options, lastSelectedIndex, selectedIndex);
                    lastSelectedIndex = selectedIndex;
                }

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
            int? menuTop = null;
            int lastSelectedIndex = selectedIndex;
            bool needsFullRender = true;

            while (true)
            {
                if (needsFullRender || menuTop == null)
                {
                    menuTop = RenderMultiSelectMenu(title, contextLines, options, helpText, selectedIndex, selectedIndices, errorMessage);
                    lastSelectedIndex = selectedIndex;
                    needsFullRender = false;
                }
                else if (lastSelectedIndex != selectedIndex)
                {
                    UpdateMultiSelectMenuSelection(menuTop.Value, options, selectedIndices, lastSelectedIndex, selectedIndex);
                    lastSelectedIndex = selectedIndex;
                }

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
                        RewriteConsoleLine(menuTop.Value + selectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, selectedIndex));
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndices.Count == 0)
                        {
                            errorMessage = emptySelectionError;
                            needsFullRender = true;
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

    private static int RenderMenu(
        string title,
        IReadOnlyList<string>? contextLines,
        IReadOnlyList<string> options,
        string helpText,
        int selectedIndex)
    {
        Console.Clear();
        WriteSectionHeader(title);
        WriteContextLines(contextLines);
        int menuTop = Console.CursorTop;

        for (int index = 0; index < options.Count; index++)
        {
            Console.WriteLine(FormatMenuOption(options, selectedIndex, index));
        }

        Console.WriteLine();
        Console.WriteLine(helpText);
        return menuTop;
    }

    private static int RenderMultiSelectMenu(
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
        int menuTop = Console.CursorTop;

        for (int index = 0; index < options.Count; index++)
        {
            Console.WriteLine(FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, index));
        }

        Console.WriteLine();
        Console.WriteLine(helpText);

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            Console.WriteLine();
            WriteError(errorMessage);
        }

        return menuTop;
    }

    private static void UpdateMenuSelection(
        int menuTop,
        IReadOnlyList<string> options,
        int lastSelectedIndex,
        int selectedIndex)
    {
        RewriteConsoleLine(menuTop + lastSelectedIndex, FormatMenuOption(options, selectedIndex, lastSelectedIndex));
        RewriteConsoleLine(menuTop + selectedIndex, FormatMenuOption(options, selectedIndex, selectedIndex));
    }

    private static void UpdateMultiSelectMenuSelection(
        int menuTop,
        IReadOnlyList<string> options,
        IReadOnlySet<int> selectedIndices,
        int lastSelectedIndex,
        int selectedIndex)
    {
        RewriteConsoleLine(menuTop + lastSelectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, lastSelectedIndex));
        RewriteConsoleLine(menuTop + selectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, selectedIndex));
    }

    private static string FormatMenuOption(IReadOnlyList<string> options, int selectedIndex, int index)
    {
        string prefix = index == selectedIndex ? "> " : "  ";
        return $"{prefix}{options[index]}";
    }

    private static string FormatMultiSelectMenuOption(
        IReadOnlyList<string> options,
        IReadOnlySet<int> selectedIndices,
        int selectedIndex,
        int index)
    {
        string pointer = index == selectedIndex ? "> " : "  ";
        string marker = selectedIndices.Contains(index) ? "[x]" : "[ ]";
        return $"{pointer}{marker} {options[index]}";
    }

    private static void RewriteConsoleLine(int top, string text)
    {
        int left = Console.CursorLeft;
        int currentTop = Console.CursorTop;
        int clearWidth = Math.Max(0, Console.BufferWidth - 1);

        Console.SetCursorPosition(0, top);
        Console.Write(text.PadRight(clearWidth));
        Console.SetCursorPosition(left, currentTop);
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
