namespace WpfPropertyGrid;

/// <summary>
/// Specifies sizing behavior for property-grid editor windows.
/// </summary>
[Flags]
public enum PropertyGridWindowOptions
{
    /// <summary>
    /// Uses the default editor-window behavior.
    /// </summary>
    None = 0x0,
    /// <summary>
    /// Uses the size defined by the editor window instead of sizing it to content.
    /// </summary>
    UseDefinedSize = 0x1,
}