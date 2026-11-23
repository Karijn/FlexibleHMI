namespace WpfPropertyGrid;
/// <summary>
/// Represents an enum-valued property and its selected enum-member metadata.
/// </summary>
public class PropertyGridEnumProperty(PropertyGridDataProvider provider) : PropertyGridProperty(provider)
{
    /// <summary>
    /// Gets the enum attributes.
    /// </summary>
    public virtual Utilities.DynamicObject EnumAttributes { get; } = provider.CreateDynamicObject() ?? throw new NotSupportedException();
    /// <inheritdoc/>
    public override void OnValueChanged()
    {
        base.OnValueChanged();
        EnumAttributes.Properties.Clear();
        if (PropertyType != null)
        {
            foreach (var fi in PropertyType.GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (fi.Name.Equals(string.Format("{0}", base.Value)))
                {
                    PropertyGridDataProvider.AddDynamicProperties(fi.GetCustomAttributes<PropertyGridAttribute>(), EnumAttributes);
                }
            }
        }
    }
}