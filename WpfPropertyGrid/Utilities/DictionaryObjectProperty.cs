namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Stores a dynamic property value and its validation errors.
/// </summary>
public class DictionaryObjectProperty
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public virtual object? Value { get; set; }
    /// <inheritdoc/>
    public override string ToString()
    {
        var value = Value;
        if (value == null)
            return string.Empty;

        if (value is string svalue)
            return svalue;

        return string.Format("{0}", value);
    }
}
