using System.Windows;

namespace FlexibleHMI;

public sealed class ThemeService : IThemeService
{
    private const string LightThemeSource = "/FlexibleHMI;component/Themes/LightTheme.xaml";
    private const string DarkThemeSource = "/FlexibleHMI;component/Themes/DarkTheme.xaml";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public void ApplyTheme(AppTheme theme)
    {
        var source = theme switch
        {
            AppTheme.Light => LightThemeSource,
            AppTheme.Dark => DarkThemeSource,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unsupported theme.")
        };

        var resources = Application.Current.Resources;
        var existingTheme = resources.MergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source.OriginalString is LightThemeSource or DarkThemeSource);

        if (existingTheme is not null)
        {
            resources.MergedDictionaries.Remove(existingTheme);
        }

        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        });

        CurrentTheme = theme;
    }
}
