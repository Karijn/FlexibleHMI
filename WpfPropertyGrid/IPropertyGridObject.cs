namespace WpfPropertyGrid;
/// <summary>
/// Defines callbacks for customizing property discovery and editor lifecycle in the property grid.
/// </summary>
public interface IPropertyGridObject
{
    /// <summary>
    /// Allows an object to modify its discovered properties before they are displayed.
    /// </summary>
    /// <param name="dataProvider">The provider describing the selected object.</param>
    /// <param name="properties">The mutable property list to finalize.</param>
    void FinalizeProperties(PropertyGridDataProvider dataProvider, IList<PropertyGridProperty> properties);
    /// <summary>
    /// Offers the object an opportunity to display a custom editor for a property.
    /// </summary>
    /// <param name="property">The property being edited.</param>
    /// <param name="editor">The editor selected by the property grid, if any.</param>
    /// <param name="result">When handled, contains the editor result; null indicates no Boolean result.</param>
    /// <returns>true if the object handled the editor request; otherwise, false.</returns>
    bool TryShowEditor(PropertyGridProperty property, object? editor, out bool? result);
    /// <summary>
    /// Notifies the object that an editor has closed.
    /// </summary>
    /// <param name="property">The property whose editor closed.</param>
    /// <param name="editor">The editor instance that was shown, if any.</param>
    void EditorClosed(PropertyGridProperty property, object? editor);
}