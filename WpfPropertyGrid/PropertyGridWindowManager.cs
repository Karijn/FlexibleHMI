namespace WpfPropertyGrid;
/// <summary>
/// Provides attached options for property-grid editor windows.
/// </summary>
public class PropertyGridWindowManager
{
    /// <summary>
    /// Identifies the attached dependency property containing property-grid window options.
    /// </summary>
    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.RegisterAttached("Options", typeof(PropertyGridWindowOptions), typeof(PropertyGridWindowManager), new PropertyMetadata(PropertyGridWindowOptions.None, (d, e) => { }));

    /// <summary>
    /// Gets the options attached to a dependency object.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static PropertyGridWindowOptions GetOptions(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (PropertyGridWindowOptions)element.GetValue(OptionsProperty);
    }
    /// <summary>
    /// Sets the options attached to a dependency object.
    /// </summary>
    public static void SetOptions(DependencyObject element, PropertyGridWindowOptions value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(OptionsProperty, value);
    }
}