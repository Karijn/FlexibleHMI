namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides details about a dictionary-backed property value rollback.
/// </summary>
public class DictionaryObjectPropertyRollbackEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of DictionaryObjectPropertyRollbackEventArgs.
    /// </summary>
    public DictionaryObjectPropertyRollbackEventArgs(string propertyName, DictionaryObjectProperty? existingProperty, object? invalidValue)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        // existingProperty may be null

        PropertyName = propertyName;
        ExistingProperty = existingProperty;
        InvalidValue = invalidValue;
    }
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string PropertyName { get; }
    /// <summary>
    /// Gets the existing property.
    /// </summary>
    public DictionaryObjectProperty? ExistingProperty { get; }
    /// <summary>
    /// Gets the invalid value.
    /// </summary>
    public object? InvalidValue { get; }
}
