namespace WpfPropertyGrid;
/// <summary>
/// Provides access to the configured property-grid object activator.
/// </summary>
public static class ActivatorService
{
    /// <summary>
    /// Creates instance.
    /// </summary>
    public static object? CreateInstance(Type type, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(type);
        return PropertyGridServiceProvider.Current.GetService<IActivator>().CreateInstance(type, args);
    }
    /// <summary>
    /// Creates instance.
    /// </summary>
    public static T? CreateInstance<T>(params object?[]? args) => PropertyGridServiceProvider.Current.GetService<IActivator>().CreateInstance<T>(args);
}
