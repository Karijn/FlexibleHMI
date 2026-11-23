namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides details about a dictionary-backed property value change.
/// </summary>
public class DictionaryObjectPropertyChangedEventArgs : PropertyChangedEventArgs
{
    /// <summary>
    /// Initializes a new instance of DictionaryObjectPropertyChangedEventArgs.
    /// </summary>
    public DictionaryObjectPropertyChangedEventArgs(string propertyName, DictionaryObjectProperty? existingProperty, DictionaryObjectProperty newProperty)
        : base(propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(newProperty);

        // existingProperty may be null
        ExistingProperty = existingProperty;
        NewProperty = newProperty;
    }
    /// <summary>
    /// Gets the existing property.
    /// </summary>
    public DictionaryObjectProperty? ExistingProperty { get; }
    /// <summary>
    /// Gets the new property.
    /// </summary>
    public DictionaryObjectProperty NewProperty { get; }
}
