namespace FlexibleHMI;

public enum AppTheme
{
    Light,
    Dark
}

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void ApplyTheme(AppTheme theme);
}
