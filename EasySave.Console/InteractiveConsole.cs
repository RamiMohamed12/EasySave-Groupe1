public sealed class InteractiveConsole
{
    private const int MinimumFrameWidth = 64;
    private const int MinimumFrameHeight = 20;
    private const int HorizontalMargin = 4;
    private const int VerticalMargin = 2;

    private static readonly ConsoleTheme Theme = new(
        ConsoleColor.Black,
        ConsoleColor.Cyan,
        ConsoleColor.White,
        ConsoleColor.DarkCyan,
        ConsoleColor.Yellow,
        ConsoleColor.Red);

    private static readonly string[] Banner =
    [
        " _____                _____                 ",
        "| ____|__ _ ___ _   _/ ___|  __ ___   _____ ",
        "|  _| / _` / __| | | \\___ \\ / _` \\ \\ / / _ \\",
        "| |__| (_| \\__ \\ |_| |___) | (_| |\\ V /  __/",
        "|_____\\__,_|___/\\__, |____/ \\__,_| \\_/ \\___|",
        "                |___/                       "
    ];

    public enum ScreenLineKind
    {
        Normal,
        Muted,
        Success,
        Warning,
        Error,
        Accent
    }

    public readonly record struct ScreenLine(string Text, ScreenLineKind Kind = ScreenLineKind.Normal);

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
                        ResetColors();
                        return (int?)selectedIndex;
                    case ConsoleKey.Escape when allowBack:
                        ResetColors();
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
                        if (errorMessage != null)
                        {
                            errorMessage = null;
                            needsFullRender = true;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex == options.Count - 1 ? 0 : selectedIndex + 1;
                        if (errorMessage != null)
                        {
                            errorMessage = null;
                            needsFullRender = true;
                        }
                        break;
                    case ConsoleKey.Spacebar:
                        if (!selectedIndices.Add(selectedIndex))
                        {
                            selectedIndices.Remove(selectedIndex);
                        }

                        errorMessage = null;
                        needsFullRender = false;
                        RewriteOptionLine(
                            menuLayout.Value,
                            selectedIndex,
                            FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, selectedIndex),
                            selected: true);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndices.Count == 0)
                        {
                            errorMessage = emptySelectionError;
                            needsFullRender = true;
                            break;
                        }

                        ResetColors();
                        return selectedIndices.OrderBy(index => index).ToArray();
                    case ConsoleKey.Escape:
                        ResetColors();
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
        bool originalVisibility = TryGetCursorVisibility();

        try
        {
            while (true)
            {
                PromptLayout layout = RenderPrompt(title, prompt, contextLines, helpText, errorMessage);
                SetCursorVisible(true);
                Console.SetCursorPosition(layout.InputLeft, layout.InputTop);
                string input = Console.ReadLine()?.Trim() ?? string.Empty;
                SetCursorVisible(false);

                if (string.IsNullOrWhiteSpace(input))
                {
                    ResetColors();
                    return null;
                }

                string? validationError = validate?.Invoke(input);
                if (string.IsNullOrWhiteSpace(validationError))
                {
                    ResetColors();
                    return input;
                }

                errorMessage = validationError;
            }
        }
        finally
        {
            SetCursorVisible(originalVisibility);
        }
    }

    public void RenderOutputScreen(
        string title,
        IReadOnlyList<ScreenLine> lines,
        string? footer = null,
        IReadOnlyList<string>? contextLines = null)
    {
        if (!CanRenderInteractiveOutput())
        {
            RenderPlainOutput(title, lines, footer, contextLines);
            return;
        }

        RenderOutputScreen(title, lines, footer, contextLines, scrollOffset: 0, showOverflow: true);
    }

    public void BrowseOutputScreen(
        string title,
        IReadOnlyList<ScreenLine> lines,
        IReadOnlyList<string>? contextLines = null)
    {
        if (!CanRenderInteractiveOutput() || Console.IsInputRedirected)
        {
            RenderPlainOutput(title, lines, footer: null, contextLines);
            return;
        }

        WithHiddenCursor(() =>
        {
            int scrollOffset = 0;
            OutputLayout layout = RenderOutputScreen(
                title,
                lines,
                "[Up/Down->Scroll] [PgUp/PgDn->Page] [Home/End->Jump] [Esc->Back]",
                contextLines,
                scrollOffset,
                showOverflow: false);

            while (true)
            {
                int maximumScrollOffset = Math.Max(0, lines.Count - layout.VisibleRows);
                scrollOffset = Math.Min(scrollOffset, maximumScrollOffset);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                int previousScrollOffset = scrollOffset;
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        scrollOffset = Math.Max(0, scrollOffset - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        scrollOffset = Math.Min(maximumScrollOffset, scrollOffset + 1);
                        break;
                    case ConsoleKey.PageUp:
                        scrollOffset = Math.Max(0, scrollOffset - layout.VisibleRows);
                        break;
                    case ConsoleKey.PageDown:
                        scrollOffset = Math.Min(maximumScrollOffset, scrollOffset + layout.VisibleRows);
                        break;
                    case ConsoleKey.Home:
                        scrollOffset = 0;
                        break;
                    case ConsoleKey.End:
                        scrollOffset = maximumScrollOffset;
                        break;
                    case ConsoleKey.Escape:
                        ResetColors();
                        return;
                }

                if (scrollOffset != previousScrollOffset)
                {
                    RewriteOutputLines(layout, lines, scrollOffset);
                }
            }
        });
    }

    private static bool CanRenderInteractiveOutput()
    {
        try
        {
            return !Console.IsOutputRedirected;
        }
        catch
        {
            return false;
        }
    }

    private static void RenderPlainOutput(
        string title,
        IReadOnlyList<ScreenLine> lines,
        string? footer,
        IReadOnlyList<string>? contextLines)
    {
        Console.WriteLine(title);

        if (contextLines is { Count: > 0 })
        {
            foreach (string contextLine in contextLines)
            {
                Console.WriteLine(contextLine);
            }
        }

        foreach (ScreenLine line in lines)
        {
            Console.WriteLine(line.Text);
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            Console.WriteLine(footer);
        }
    }

    private static OutputLayout RenderOutputScreen(
        string title,
        IReadOnlyList<ScreenLine> lines,
        string? footer,
        IReadOnlyList<string>? contextLines,
        int scrollOffset,
        bool showOverflow)
    {
        Console.Clear();
        LayoutBox box = CreateLayoutBox(
            lines.Select(line => line.Text).DefaultIfEmpty(string.Empty).ToArray(),
            contextLines,
            footer ?? string.Empty,
            includeBanner: false);

        DrawFrame(box, title);
        int row = DrawHeaderContent(box, title, contextLines, includeBanner: false);
        int footerRows = string.IsNullOrWhiteSpace(footer) ? 1 : 2;
        int maxRows = Math.Max(1, box.Bottom - row - footerRows);
        int visibleRows = Math.Min(lines.Count, maxRows);
        int safeScrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, lines.Count - visibleRows));

        for (int index = 0; index < visibleRows; index++)
        {
            ScreenLine line = lines[safeScrollOffset + index];
            WriteAt(box.ContentLeft, row + index, Truncate(line.Text, box.ContentWidth).PadRight(box.ContentWidth), GetColor(line.Kind));
        }

        if (showOverflow && visibleRows < lines.Count)
        {
            string overflow = $"... {lines.Count - visibleRows} more line(s) not shown";
            WriteAt(box.ContentLeft, row + visibleRows, Truncate(overflow, box.ContentWidth).PadRight(box.ContentWidth), Theme.Accent);
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            WriteAt(box.ContentLeft, box.Bottom - 1, Truncate(footer, box.ContentWidth).PadRight(box.ContentWidth), Theme.Accent);
        }

        return new OutputLayout(box.ContentLeft, row, box.ContentWidth, visibleRows);
    }

    public void WaitForKey()
    {
        Console.ReadKey(true);
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
            ResetColors();

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

    private static void WithHiddenCursor(Action action)
    {
        WithHiddenCursor(() =>
        {
            action();
            return true;
        });
    }

    private static MenuLayout RenderMenu(
        string title,
        IReadOnlyList<string>? contextLines,
        IReadOnlyList<string> options,
        string helpText,
        int selectedIndex)
    {
        Console.Clear();
        LayoutBox box = CreateLayoutBox(options, contextLines, helpText, includeBanner: IsMainMenu(title));
        DrawFrame(box, title);
        int row = DrawHeaderContent(box, title, contextLines, includeBanner: IsMainMenu(title));
        int menuTop = row;

        for (int index = 0; index < options.Count; index++)
        {
            WriteOptionLine(box.ContentLeft, row + index, box.ContentWidth, FormatMenuOption(options, selectedIndex, index), index == selectedIndex);
        }

        DrawShortcutBar(box, helpText, multiSelect: false);
        return new MenuLayout(box.ContentLeft, menuTop, box.ContentWidth);
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
        LayoutBox box = CreateLayoutBox(options, contextLines, helpText, includeBanner: false, errorMessage);
        DrawFrame(box, title);
        int row = DrawHeaderContent(box, title, contextLines, includeBanner: false);
        int menuTop = row;

        for (int index = 0; index < options.Count; index++)
        {
            WriteOptionLine(
                box.ContentLeft,
                row + index,
                box.ContentWidth,
                FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, index),
                index == selectedIndex);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            WriteAt(box.ContentLeft, row + options.Count + 1, errorMessage, Theme.Error);
        }

        DrawShortcutBar(box, helpText, multiSelect: true);
        return new MenuLayout(box.ContentLeft, menuTop, box.ContentWidth);
    }

    private static PromptLayout RenderPrompt(
        string title,
        string prompt,
        IReadOnlyList<string>? contextLines,
        string helpText,
        string? errorMessage)
    {
        Console.Clear();
        IReadOnlyList<string> promptLines = [prompt, helpText];
        LayoutBox box = CreateLayoutBox(promptLines, contextLines, helpText, includeBanner: false, errorMessage);
        DrawFrame(box, title);
        int row = DrawHeaderContent(box, title, contextLines, includeBanner: false);

        const string inputLabel = "Input: ";
        WriteAt(box.ContentLeft, row, inputLabel, Theme.Muted);
        WriteAt(box.ContentLeft + inputLabel.Length, row, prompt, Theme.Primary);
        int inputLeft = Math.Min(box.ContentLeft + inputLabel.Length + prompt.Length, box.Right - 2);
        int inputTop = row;

        WriteAt(box.ContentLeft, row + 2, helpText, Theme.Muted);

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            WriteAt(box.ContentLeft, row + 4, errorMessage, Theme.Error);
        }

        DrawShortcutBar(box, "[Enter->Confirm] [Empty->Back]", multiSelect: false);
        return new PromptLayout(inputLeft, inputTop);
    }

    private static int DrawHeaderContent(LayoutBox box, string title, IReadOnlyList<string>? contextLines, bool includeBanner)
    {
        int row = box.Top + 2;

        if (includeBanner)
        {
            foreach (string line in Banner)
            {
                int left = box.Left + Math.Max(2, (box.Width - line.Length) / 2);
                WriteAt(left, row++, line, Theme.Primary);
            }

            WriteCentered(box, row++, "Portable backup manager - TUI mode", Theme.Accent);
            row++;
        }
        else
        {
            WriteAt(box.ContentLeft, row++, title.ToUpperInvariant(), Theme.Primary);
            WriteAt(box.ContentLeft, row++, new string('-', Math.Min(title.Length, box.ContentWidth)), Theme.Border);
            row++;
        }

        if (contextLines != null && contextLines.Any(line => !string.IsNullOrWhiteSpace(line)))
        {
            DrawPanel(box.ContentLeft, row, box.ContentWidth, contextLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray());
            row += contextLines.Count(line => !string.IsNullOrWhiteSpace(line)) + 3;
        }

        return row;
    }

    private static void DrawFrame(LayoutBox box, string title)
    {
        string horizontal = new('-', box.Width - 2);
        WriteAt(box.Left, box.Top, $"+{horizontal}+", Theme.Border);

        for (int row = box.Top + 1; row < box.Bottom; row++)
        {
            WriteAt(box.Left, row, "|", Theme.Border);
            WriteAt(box.Right, row, "|", Theme.Border);
        }

        WriteAt(box.Left, box.Bottom, $"+{horizontal}+", Theme.Border);
        string titleText = $"[ {title} ]";
        int titleLeft = Math.Min(box.Left + 2, box.Right - titleText.Length);
        WriteAt(titleLeft, box.Top, titleText, Theme.Primary);

        const string appText = "EasySave TUI";
        WriteAt(Math.Max(box.Left + 2, box.Right - appText.Length - 2), box.Top, appText, Theme.Accent);
    }

    private static void DrawPanel(int left, int top, int width, IReadOnlyList<string> lines)
    {
        WriteAt(left, top, $"+{new string('-', width - 2)}+", Theme.Border);
        for (int index = 0; index < lines.Count; index++)
        {
            WriteAt(left, top + index + 1, "|", Theme.Border);
            WriteAt(left + width - 1, top + index + 1, "|", Theme.Border);
            WriteLabelValue(left + 2, top + index + 1, GetLabel(lines[index]), GetValue(lines[index]));
        }

        WriteAt(left, top + lines.Count + 1, $"+{new string('-', width - 2)}+", Theme.Border);
    }

    private static void DrawShortcutBar(LayoutBox box, string helpText, bool multiSelect)
    {
        string shortcuts = multiSelect
            ? "[Up/Down->Move] [Space->Toggle] [Enter->Confirm] [Esc->Back]"
            : "[Up/Down->Move] [Enter->Select] [Esc->Back]";

        if (!string.IsNullOrWhiteSpace(helpText) && helpText.Contains("empty", StringComparison.OrdinalIgnoreCase))
        {
            shortcuts = helpText;
        }

        int row = box.Bottom - 1;
        WriteAt(box.ContentLeft, row, shortcuts.PadRight(box.ContentWidth), Theme.Accent);
    }

    private static ConsoleColor GetColor(ScreenLineKind kind)
    {
        return kind switch
        {
            ScreenLineKind.Muted => Theme.Muted,
            ScreenLineKind.Success => ConsoleColor.Green,
            ScreenLineKind.Warning => Theme.Accent,
            ScreenLineKind.Error => Theme.Error,
            ScreenLineKind.Accent => Theme.Accent,
            _ => Theme.Primary
        };
    }

    private static void UpdateMenuSelection(
        MenuLayout layout,
        IReadOnlyList<string> options,
        int lastSelectedIndex,
        int selectedIndex)
    {
        RewriteOptionLine(layout, lastSelectedIndex, FormatMenuOption(options, selectedIndex, lastSelectedIndex), selected: false);
        RewriteOptionLine(layout, selectedIndex, FormatMenuOption(options, selectedIndex, selectedIndex), selected: true);
    }

    private static void UpdateMultiSelectMenuSelection(
        MenuLayout layout,
        IReadOnlyList<string> options,
        IReadOnlySet<int> selectedIndices,
        int lastSelectedIndex,
        int selectedIndex)
    {
        RewriteOptionLine(layout, lastSelectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, lastSelectedIndex), selected: false);
        RewriteOptionLine(layout, selectedIndex, FormatMultiSelectMenuOption(options, selectedIndices, selectedIndex, selectedIndex), selected: true);
    }

    private static void RewriteOptionLine(MenuLayout layout, int optionIndex, string text, bool selected)
    {
        WriteOptionLine(layout.Left, layout.MenuTop + optionIndex, layout.Width, text, selected);
    }

    private static void RewriteOutputLines(
        OutputLayout layout,
        IReadOnlyList<ScreenLine> lines,
        int scrollOffset)
    {
        for (int index = 0; index < layout.VisibleRows; index++)
        {
            int lineIndex = scrollOffset + index;
            ScreenLine line = lineIndex < lines.Count
                ? lines[lineIndex]
                : new ScreenLine(string.Empty);

            WriteAt(
                layout.Left,
                layout.Top + index,
                Truncate(line.Text, layout.Width).PadRight(layout.Width),
                GetColor(line.Kind));
        }
    }

    private static void WriteOptionLine(int left, int top, int width, string text, bool selected)
    {
        ConsoleColor color = selected ? Theme.Accent : Theme.Primary;
        WriteAt(left, top, Truncate(text, width).PadRight(width), color);
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

    private static LayoutBox CreateLayoutBox(
        IReadOnlyList<string> contentLines,
        IReadOnlyList<string>? contextLines,
        string helpText,
        bool includeBanner,
        string? errorMessage = null)
    {
        int windowWidth = Math.Max(Console.WindowWidth, MinimumFrameWidth);
        int windowHeight = Math.Max(Console.WindowHeight, MinimumFrameHeight);
        int contextWidth = contextLines?
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Length + 4)
            .DefaultIfEmpty(0)
            .Max() ?? 0;
        int contentWidth = contentLines.DefaultIfEmpty(string.Empty).Max(line => line.Length);
        int bannerWidth = includeBanner ? Banner.Max(line => line.Length) : 0;
        int requestedWidth = Math.Max(MinimumFrameWidth, Math.Max(Math.Max(contextWidth, contentWidth), bannerWidth) + 8);
        int width = Math.Min(requestedWidth, Math.Max(24, windowWidth - HorizontalMargin));

        int contextHeight = contextLines?.Count(line => !string.IsNullOrWhiteSpace(line)) > 0
            ? contextLines.Count(line => !string.IsNullOrWhiteSpace(line)) + 3
            : 0;
        int bannerHeight = includeBanner ? Banner.Length + 2 : 3;
        int errorHeight = string.IsNullOrWhiteSpace(errorMessage) ? 0 : 2;
        int requestedHeight = Math.Max(MinimumFrameHeight, bannerHeight + contextHeight + contentLines.Count + errorHeight + 6);
        int height = Math.Min(requestedHeight, Math.Max(10, windowHeight - VerticalMargin));
        int left = Math.Max(0, (Console.WindowWidth - width) / 2);
        int top = Math.Max(0, (Console.WindowHeight - height) / 2);

        return new LayoutBox(left, top, width, height);
    }

    private static void WriteLabelValue(int left, int top, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            WriteAt(left, top, label, Theme.Muted);
            return;
        }

        WriteAt(left, top, $"{label}: ", Theme.Muted);
        WriteAt(left + label.Length + 2, top, value, Theme.Primary);
    }

    private static void WriteCentered(LayoutBox box, int top, string text, ConsoleColor color)
    {
        int left = box.Left + Math.Max(2, (box.Width - text.Length) / 2);
        WriteAt(left, top, text, color);
    }

    private static void WriteAt(int left, int top, string text, ConsoleColor color)
    {
        if (top < 0 || top >= Console.BufferHeight || left >= Console.BufferWidth)
        {
            return;
        }

        int safeLeft = Math.Max(0, left);
        string safeText = Truncate(text, Math.Max(0, Console.BufferWidth - safeLeft));
        Console.SetCursorPosition(safeLeft, top);
        Console.ForegroundColor = color;
        Console.BackgroundColor = Theme.Background;
        Console.Write(safeText);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (maxLength <= 0)
        {
            return string.Empty;
        }

        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string GetLabel(string line)
    {
        int separatorIndex = line.IndexOf(':');
        return separatorIndex <= 0 ? line : line[..separatorIndex];
    }

    private static string GetValue(string line)
    {
        int separatorIndex = line.IndexOf(':');
        return separatorIndex < 0 || separatorIndex == line.Length - 1
            ? string.Empty
            : line[(separatorIndex + 1)..].Trim();
    }

    private static bool IsMainMenu(string title)
    {
        return title.Contains("main", StringComparison.OrdinalIgnoreCase)
            || title.Contains("principal", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch
        {
            // Ignore cursor visibility issues when the host does not support it.
        }
    }

    private static bool TryGetCursorVisibility()
    {
        try
        {
            return Console.CursorVisible;
        }
        catch
        {
            return true;
        }
    }

    private static void ResetColors()
    {
        Console.ResetColor();
    }

    private readonly record struct ConsoleTheme(
        ConsoleColor Background,
        ConsoleColor Border,
        ConsoleColor Primary,
        ConsoleColor Muted,
        ConsoleColor Accent,
        ConsoleColor Error);

    private readonly record struct LayoutBox(int Left, int Top, int Width, int Height)
    {
        public int Right => Left + Width - 1;
        public int Bottom => Top + Height - 1;
        public int ContentLeft => Left + 3;
        public int ContentWidth => Math.Max(1, Width - 6);
    }

    private readonly record struct MenuLayout(int Left, int MenuTop, int Width);

    private readonly record struct OutputLayout(int Left, int Top, int Width, int VisibleRows);

    private readonly record struct PromptLayout(int InputLeft, int InputTop);
}
