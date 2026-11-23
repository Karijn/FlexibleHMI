namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Describes a property exposed by a dynamic object.
/// </summary>
public class DynamicObjectProperty : PropertyDescriptor
{
    private Type _type;
    private bool _isReadOnly;
    private object? _defaultValue;
    /// <summary>
    /// Initializes a new instance of DynamicObjectProperty.
    /// </summary>
    public DynamicObjectProperty(PropertyDescriptor descriptor)
        : base(descriptor)
    {
        var atts = new List<Attribute>();
        foreach (Attribute att in descriptor.Attributes)
        {
            atts.Add(att);
        }
        Construct(descriptor.Name, descriptor.PropertyType, atts);
    }
    /// <summary>
    /// Initializes a new instance of DynamicObjectProperty.
    /// </summary>
    public DynamicObjectProperty(string name, Type type, IEnumerable<Attribute>? attributes)
        : base(name, GetAttributes(attributes))
    {
        Construct(name, type, attributes);
    }
    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public virtual object? DefaultValue { get => _defaultValue; set { _defaultValue = ConversionService.ConvertObjectType(value, _type); } }
    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public virtual int SortOrder { get; set; }
    /// <summary>
    /// Gets or sets the has default value.
    /// </summary>
    public virtual bool HasDefaultValue { get; set; }
    /// <inheritdoc/>
    public override Type ComponentType => typeof(DynamicObject);
    /// <summary>
    /// Gets whether the property is read-only.
    /// </summary>
    public override bool IsReadOnly => _isReadOnly;
    /// <summary>
    /// Gets the property type.
    /// </summary>
    public override Type PropertyType => _type;
    /// <inheritdoc/>
    public override string ToString() => Name + " (" + _type.FullName + ")";
    /// <inheritdoc/>
    public override bool ShouldSerializeValue(object component) => false;
    /// <inheritdoc/>
    public override bool CanResetValue(object component) => HasDefaultValue;
    /// <inheritdoc/>
    public override object? GetValue(object? component)
    {
        if (component is DynamicObject obj)
            return obj.GetPropertyValue(Name, _defaultValue);

        throw new ArgumentException("Component is not of the DynamicObject type", nameof(component));
    }

    /// <summary>
    /// Initializes the descriptor name, type, and attributes.
    /// </summary>
    [MemberNotNull(nameof(_type))]
    protected virtual void Construct(string name, Type type, IEnumerable<Attribute>? attributes)
    {
        _type = type;

        var ro = attributes?.OfType<ReadOnlyAttribute>().FirstOrDefault();
        if (ro != null)
        {
            _isReadOnly = ro.IsReadOnly;
        }

        var dv = attributes?.OfType<DefaultValueAttribute>().FirstOrDefault();
        if (dv != null)
        {
            HasDefaultValue = true;
            _defaultValue = ConversionService.ConvertObjectType(dv.Value, _type);
        }
        else
        {
            _defaultValue = ConversionService.ConvertObjectType(null, _type);
        }
    }
    /// <inheritdoc/>
    public override void ResetValue(object component)
    {
        if (HasDefaultValue)
        {
            SetValue(component, _defaultValue);
        }
    }
    /// <inheritdoc/>
    public override void SetValue(object? component, object? value)
    {
        if (component is DynamicObject obj)
        {
            obj.SetPropertyValue(Name, value);
            return;
        }

        throw new ArgumentException("Component is not of the DynamicObject type", nameof(component));
    }

    private static Attribute[] GetAttributes(IEnumerable<Attribute>? attributes)
    {
        var list = attributes == null ? [] : new List<Attribute>(attributes);
        return [.. list];
    }
}