namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides details about a proposed dictionary-backed property value change.
/// </summary>
public class DictionaryObjectPropertyChangingEventArgs : PropertyChangingEventArgs
{
    /// <summary>
    /// Initializes a new instance of DictionaryObjectPropertyChangingEventArgs.
    /// </summary>
    public DictionaryObjectPropertyChangingEventArgs(string propertyName, DictionaryObjectProperty? existingProperty, DictionaryObjectProperty newProperty)
        : base(propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(newProperty);

        // existingProperty may be null

        ExistingProperty = existingProperty;
        NewProperty = newProperty;
    }
    /// <summary>
    /// Gets or sets the existing property.
    /// </summary>
    public DictionaryObjectProperty? ExistingProperty { get; }
    /// <summary>
    /// Gets or sets the new property.
    /// </summary>
    public DictionaryObjectProperty NewProperty { get; }
    /// <summary>
    /// Gets or sets the cancel.
    /// </summary>
    public virtual bool Cancel { get; set; }
}
