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
            MenuLayout? menuLayout = null;
            int lastSelectedIndex = selectedIndex;

            while (true)
            {
                menuLayout ??= RenderMenu(title, contextLines, options, helpText, selectedIndex);
                if (lastSelectedIndex != selectedIndex)
                {
                    UpdateMenuSelection(menuLayout.Value, options, lastSelectedIndex, selectedIndex);
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
            MenuLayout? menuLayout = null;
            int lastSelectedIndex = selectedIndex;
            bool needsFullRender = true;

            while (true)
            {
                if (needsFullRender || menuLayout == null)
                {
                    menuLayout = RenderMultiSelectMenu(title, contextLines, options, helpText, selectedIndex, selectedIndices, errorMessage);
                    lastSelectedIndex = selectedIndex;
                    needsFullRender = false;
                }
                else if (lastSelectedIndex != selectedIndex)
                {
                    UpdateMultiSelectMenuSelection(menuLayout.Value, options, selectedIndices, lastSelectedIndex, selectedIndex);
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
                        RewriteConsoleLine(
                            menuLayout.Value.Left,
                            menuLayout.Value.MenuTop + selectedIndex,
                            FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, selectedIndex));
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

    private static MenuLayout RenderMenu(
        string title,
        IReadOnlyList<string>? contextLines,
        IReadOnlyList<string> options,
        string helpText,
        int selectedIndex)
    {
        Console.Clear();
        var lines = new List<string>();
        AddSectionHeader(lines, title);
        AddContextLines(lines, contextLines);
        int menuTopOffset = lines.Count;

        for (int index = 0; index < options.Count; index++)
        {
            lines.Add(FormatMenuOption(options, selectedIndex, index));
        }

        lines.Add(string.Empty);
        lines.Add(helpText);
        MenuLayout layout = CreateCenteredLayout(lines, menuTopOffset);
        WriteLines(layout.Left, layout.Top, lines);
        return layout;
    }

    private static MenuLayout RenderMultiSelectMenu(
        string title,
        IReadOnlyList<string>? contextLines,
        IReadOnlyList<string> options,
        string helpText,
        int selectedIndex,
        IReadOnlySet<int> selectedIndices,
        string? errorMessage)
    {
        Console.Clear();
        var lines = new List<string>();
        AddSectionHeader(lines, title);
        AddContextLines(lines, contextLines);
        int menuTopOffset = lines.Count;

        for (int index = 0; index < options.Count; index++)
        {
            lines.Add(FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, index));
        }

        lines.Add(string.Empty);
        lines.Add(helpText);

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            lines.Add(string.Empty);
            lines.Add(errorMessage);
        }

        MenuLayout layout = CreateCenteredLayout(lines, menuTopOffset);
        WriteLines(layout.Left, layout.Top, lines, errorMessage);
        return layout;
    }

    private static void UpdateMenuSelection(
        MenuLayout layout,
        IReadOnlyList<string> options,
        int lastSelectedIndex,
        int selectedIndex)
    {
        RewriteConsoleLine(layout.Left, layout.MenuTop + lastSelectedIndex, FormatMenuOption(options, selectedIndex, lastSelectedIndex));
        RewriteConsoleLine(layout.Left, layout.MenuTop + selectedIndex, FormatMenuOption(options, selectedIndex, selectedIndex));
    }

    private static void UpdateMultiSelectMenuSelection(
        MenuLayout layout,
        IReadOnlyList<string> options,
        IReadOnlySet<int> selectedIndices,
        int lastSelectedIndex,
        int selectedIndex)
    {
        RewriteConsoleLine(layout.Left, layout.MenuTop + lastSelectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, lastSelectedIndex));
        RewriteConsoleLine(layout.Left, layout.MenuTop + selectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, selectedIndex));
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

    private static MenuLayout CreateCenteredLayout(IReadOnlyList<string> lines, int menuTopOffset)
    {
        int width = lines.Count == 0 ? 0 : lines.Max(line => line.Length);
        int left = Math.Max(0, (Console.WindowWidth - width) / 2);
        int top = Math.Max(0, (Console.WindowHeight - lines.Count) / 2);

        return new MenuLayout(left, top, top + menuTopOffset);
    }

    private static void WriteLines(int left, int top, IReadOnlyList<string> lines, string? errorMessage = null)
    {
        for (int index = 0; index < lines.Count; index++)
        {
            Console.SetCursorPosition(left, top + index);

            if (!string.IsNullOrWhiteSpace(errorMessage) && lines[index] == errorMessage)
            {
                WriteError(errorMessage);
            }
            else
            {
                Console.WriteLine(lines[index]);
            }
        }
    }

    private static void RewriteConsoleLine(int left, int top, string text)
    {
        int currentLeft = Console.CursorLeft;
        int currentTop = Console.CursorTop;
        int clearWidth = Math.Max(0, Console.BufferWidth - left - 1);

        Console.SetCursorPosition(left, top);
        Console.Write(text.PadRight(clearWidth));
        Console.SetCursorPosition(currentLeft, currentTop);
    }

    private static void AddContextLines(ICollection<string> lines, IReadOnlyList<string>? contextLines)
    {
        if (contextLines == null || contextLines.Count == 0)
        {
            return;
        }

        foreach (string line in contextLines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            lines.Add(line);
        }

        lines.Add(string.Empty);
    }

    private static void AddSectionHeader(ICollection<string> lines, string title)
    {
        lines.Add(title);
        lines.Add(new string('=', title.Length));
        lines.Add(string.Empty);
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

    private readonly record struct MenuLayout(int Left, int Top, int MenuTop);
}
