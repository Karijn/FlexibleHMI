namespace WpfPropertyGrid;

/// <summary>
/// Attaches a named, typed metadata value to a member for use by the property grid.
/// </summary>
/// <summary>
/// Attaches display metadata to a member for use by the property grid.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class PropertyGridAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public object? Value { get; set; }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public Type? Type { get; set; } = typeof(object);
    /// <summary>
    /// Gets the type id.
    /// </summary>
    public override object TypeId => Name ?? string.Empty;
}