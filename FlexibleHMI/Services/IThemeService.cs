namespace FlexibleHMI.Services;

/// <summary>Defines the application's available visual themes.</summary>
public enum AppTheme
{
    /// <summary>The light color scheme.</summary>
    Light,
    /// <summary>The dark color scheme.</summary>
    Dark
}

/// <summary>Applies and reports the active application theme.</summary>
public interface IThemeService
{
    /// <summary>Gets the currently applied theme.</summary>
    AppTheme CurrentTheme { get; }

    /// <summary>Applies the specified theme to the application resources.</summary>
    /// <param name="theme">The theme to apply.</param>
    void ApplyTheme(AppTheme theme);
}
