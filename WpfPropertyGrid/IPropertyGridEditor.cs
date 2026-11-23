namespace WpfPropertyGrid;
/// <summary>
/// Defines an editor that receives the property and parameter it is editing.
/// </summary>
public interface IPropertyGridEditor
{
    /// <summary>
    /// Sets the property and optional parameter that the editor should edit.
    /// </summary>
    /// <param name="property">The property to edit.</param>
    /// <param name="parameter">The editor parameter, if specified.</param>
    /// <returns>true if the editor accepts the context; otherwise, false.</returns>
    bool SetContext(PropertyGridProperty property, object? parameter);
}