namespace WpfPropertyGrid;
/// <summary>
/// Specifies the text-formatting options used when decamelizing a string.
/// </summary>
public class DecamelizeOptions
{
    /// <summary>
    /// Gets or sets the text options.
    /// </summary>
    public virtual DecamelizeTextOptions TextOptions { get; set; } = DecamelizeTextOptions.Default;
}
