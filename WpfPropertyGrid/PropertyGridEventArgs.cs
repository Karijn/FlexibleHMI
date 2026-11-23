namespace WpfPropertyGrid;
/// <summary>
/// Provides the property and optional context associated with a property-grid event.
/// </summary>
public class PropertyGridEventArgs(PropertyGridProperty property, object? context = null) : CancelEventArgs
{
    /// <summary>
    /// Gets or sets the property.
    /// </summary>
    public PropertyGridProperty Property { get; } = property;
    /// <summary>
    /// Gets or sets the context.
    /// </summary>
    public object? Context { get; set; } = context;
}