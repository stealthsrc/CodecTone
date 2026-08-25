using System.Windows;

namespace AudioConverter.Desktop.Services;

public static class ThemeService
{
    public static void Apply(string theme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        if (dictionaries.Count == 0) return;
        dictionaries[0] = new ResourceDictionary
        {
            Source = new Uri(theme.Equals("White", StringComparison.OrdinalIgnoreCase) ? "Themes/White.xaml" : "Themes/Oled.xaml", UriKind.Relative),
        };
        if (Application.Current.MainWindow is { } window)
        {
            TitleBarTheme.Apply(window, theme);
        }
    }
}
