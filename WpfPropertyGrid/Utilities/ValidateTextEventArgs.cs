namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides the text being validated and cancellation state for a text-validation event.
/// </summary>
public class ValidateTextEventArgs(string? text) : CancelEventArgs
{
    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; } = text;
    /// <summary>
    /// Gets or sets the replacement text.
    /// </summary>
    public virtual string? ReplacementText { get; set; }
}