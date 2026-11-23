namespace WpfPropertyGrid;
/// <summary>
/// Provides access to the configured property-grid decamelizer.
/// </summary>
public static class DecamelizationService
{
    /// <summary>
    /// Formats identifier-like text according to the supplied decamelization options.
    /// </summary>
    public static string? Decamelize(string? text, DecamelizeOptions? options = null) => PropertyGridServiceProvider.Current.GetService<IDecamelizer>().Decamelize(text, options);
}
