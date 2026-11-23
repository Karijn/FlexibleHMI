namespace WpfPropertyGrid;
/// <summary>
/// Defines the service used to format identifier-like text for display.
/// </summary>
public interface IDecamelizer
{
    /// <summary>
    /// Formats identifier-like text for display.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <param name="options">Options controlling casing, escapes, and separators; defaults are used when null.</param>
    /// <returns>The formatted text, or null when the input is null.</returns>
    string? Decamelize(string? text, DecamelizeOptions? options = null);
}
