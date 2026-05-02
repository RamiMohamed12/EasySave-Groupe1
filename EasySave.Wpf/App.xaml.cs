using System.Windows;

namespace EasySave.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string LightThemeSource = "Themes/Light.xaml";
    private const string DarkThemeSource = "Themes/Dark.xaml";

    public static void ApplyConfiguredTheme()
    {
        ApplyTheme(RuntimeStoragePaths.GetThemeMode());
    }

    public static void ApplyTheme(string themeMode)
    {
        Current.ThemeMode = ResolveFluentThemeMode(themeMode);
        string resolvedThemeSource = ResolveThemeSource(themeMode);
        ResourceDictionary themeDictionary = new()
        {
            Source = new Uri(resolvedThemeSource, UriKind.Relative)
        };

        var dictionaries = Current.Resources.MergedDictionaries;
        ResourceDictionary? existingTheme = dictionaries.FirstOrDefault(IsThemeDictionary);
        if (existingTheme is not null)
        {
            dictionaries.Remove(existingTheme);
        }

        dictionaries.Add(themeDictionary);
    }

    private static string ResolveThemeSource(string themeMode)
    {
        if (themeMode == RuntimeStoragePaths.DarkThemeMode)
        {
            return DarkThemeSource;
        }

        if (themeMode == RuntimeStoragePaths.SystemThemeMode && IsSystemDarkTheme())
        {
            return DarkThemeSource;
        }

        return LightThemeSource;
    }

    private static ThemeMode ResolveFluentThemeMode(string themeMode)
    {
        return themeMode switch
        {
            RuntimeStoragePaths.LightThemeMode => ThemeMode.Light,
            RuntimeStoragePaths.DarkThemeMode => ThemeMode.Dark,
            RuntimeStoragePaths.SystemThemeMode => ThemeMode.System,
            _ => ThemeMode.System
        };
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        string source = dictionary.Source?.OriginalString ?? string.Empty;
        return source.EndsWith(LightThemeSource, StringComparison.OrdinalIgnoreCase)
            || source.EndsWith(DarkThemeSource, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            object? value = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKey)?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
