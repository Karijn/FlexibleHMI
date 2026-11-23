namespace WpfPropertyGrid;

/// <summary>
/// Selects the first data template whose DataType matches the supplied item.
/// </summary>
[ContentProperty("DataTemplates")]
public class TypeDataTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets the templates available to the selector.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ObservableCollection<DataTemplate> DataTemplates { get; } = [];
    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        foreach (var template in DataTemplates.Where(dt => dt.DataType is Type))
        {
            var type = template.DataType as Type;
            if (type == null)
                continue;

            if (item == null)
            {
                if (!type.IsValueType)
                    return template;
            }
            else
            {
                if (type.IsInstanceOfType(item))
                    return template;
            }
        }
        return DataTemplates.FirstOrDefault(dt => dt.DataType == null);
    }
}