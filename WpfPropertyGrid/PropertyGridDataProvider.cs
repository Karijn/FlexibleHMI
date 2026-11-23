namespace WpfPropertyGrid;
/// <summary>
/// Builds property descriptors and editable property models for the property grid.
/// </summary>
public class PropertyGridDataProvider : IListSource
{
    /// <summary>
    /// Initializes a new instance of PropertyGridDataProvider.
    /// </summary>
    public PropertyGridDataProvider(PropertyGrid grid, object data)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(data);

        Grid = grid;
        Data = data;
        ScanProperties();
    }

    bool IListSource.ContainsListCollection => false;
    IList IListSource.GetList() => Properties;
    /// <summary>
    /// Gets the grid.
    /// </summary>
    public PropertyGrid Grid { get; }
    /// <summary>
    /// Gets the data.
    /// </summary>
    public object Data { get; }
    /// <summary>
    /// Gets the properties.
    /// </summary>
    public virtual ObservableCollection<PropertyGridProperty> Properties { get; } = [];
    /// <summary>
    /// Gets whether the provider has any properties to display.
    /// </summary>
    public static bool HasProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        foreach (var descriptor in TypeDescriptor.GetProperties(type).OfType<PropertyDescriptor>())
        {
            if (!descriptor.IsBrowsable)
                continue;

            return true;
        }
        return false;
    }
    /// <summary>
    /// Adds a property descriptor and creates its property model.
    /// </summary>
    public virtual PropertyGridProperty? AddProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        var prop = Properties.FirstOrDefault(p => p.Name == propertyName);
        if (prop != null)
            return prop;

        var desc = TypeDescriptor.GetProperties(Data).OfType<PropertyDescriptor>().FirstOrDefault(p => p.Name == propertyName);
        if (desc != null)
        {
            prop = CreateProperty(desc);
            if (prop != null)
            {
                Properties.Add(prop);
            }
        }
        return prop;
    }
    /// <summary>
    /// Creates a dynamic object using the configured object activator.
    /// </summary>
    public virtual Utilities.DynamicObject? CreateDynamicObject() => ActivatorService.CreateInstance<Utilities.DynamicObject>();
    /// <summary>
    /// Creates the property model associated with a descriptor.
    /// </summary>
    public virtual PropertyGridProperty CreateProperty() => ActivatorService.CreateInstance<PropertyGridProperty>(this) ?? throw new NotSupportedException();

    /// <summary>
    /// Populates a property model with descriptor metadata, values, and editor options.
    /// </summary>
    protected virtual void Describe(PropertyGridProperty property, PropertyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(descriptor);

        property.Descriptor = descriptor;
        property.Name = descriptor.Name;
        property.PropertyType = descriptor.PropertyType;
        property.Category = string.IsNullOrWhiteSpace(descriptor.Category) || descriptor.Category.EqualsIgnoreCase(CategoryAttribute.Default.Category) ? Grid.DefaultCategoryName : descriptor.Category;
        property.IsReadOnly = descriptor.IsReadOnly;
        property.Description = descriptor.Description;
        property.DisplayName = descriptor.DisplayName;
        if (Grid.DecamelizePropertiesDisplayNames && property.DisplayName == descriptor.Name)
        {
            property.DisplayName = DecamelizationService.Decamelize(property.DisplayName);
        }

        property.IsEnum = descriptor.PropertyType.IsEnum;
        property.IsFlagsEnum = descriptor.PropertyType.IsEnum && Extensions.IsFlagsEnum(descriptor.PropertyType);

        var options = descriptor.Attributes.OfType<PropertyGridOptionsAttribute>().FirstOrDefault();
        if (options != null)
        {
            if (options.SortOrder != 0)
            {
                property.SortOrder = options.SortOrder;
            }

            property.IsEnum = options.IsEnum;
            property.IsFlagsEnum = options.IsFlagsEnum;

            var getMethod = options.GetValueMethodName.Nullify();
            if (getMethod != null)
            {
                var type = property.DataProvider.Data.GetType();
                var method = type.GetMethod(getMethod, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) ?? throw new InvalidOperationException($"Cannot find method '{getMethod}' on object of type '{type.FullName}'.");
                if (method.GetParameters().Length > 1)
                    throw new InvalidOperationException($"Method '{getMethod}' on object of type '{type.FullName}' must have zero argument or one argument of type '{nameof(PropertyGridProperty)}'.");

                property.GetMethod = method;
            }
        }

        var att = descriptor.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
        if (att != null)
        {
            property.HasDefaultValue = true;
            property.DefaultValue = att.Value;
        }
        else if (options != null)
        {
            if (options.HasDefaultValue)
            {
                property.HasDefaultValue = true;
                property.DefaultValue = options.DefaultValue;
            }
            else
            {
                if (PropertyGridComboBoxExtension.TryGetDefaultValue(options, out var defaultValue))
                {
                    property.DefaultValue = defaultValue;
                    property.HasDefaultValue = true;
                }
            }
        }

        AddDynamicProperties(descriptor.Attributes.OfType<PropertyGridAttribute>(), property.Attributes);
        AddDynamicProperties(descriptor.PropertyType.GetCustomAttributes<PropertyGridAttribute>(), property.TypeAttributes);
    }
    /// <summary>
    /// Adds property-grid metadata as dynamic properties on the target object.
    /// </summary>
    public static void AddDynamicProperties(IEnumerable<PropertyGridAttribute> attributes, Utilities.DynamicObject dynamicObject)
    {
        if (attributes == null || dynamicObject == null)
            return;

        foreach (var attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Name) || attribute.Type == null)
                continue;

            var prop = dynamicObject.AddProperty(attribute.Name, attribute.Type, null);
            prop.SetValue(dynamicObject, attribute.Value);
        }
    }
    /// <summary>
    /// Creates the property model associated with a descriptor.
    /// </summary>
    public virtual PropertyGridProperty CreateProperty(PropertyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var forceReadWrite = false;
        PropertyGridProperty? property = null;
        var options = descriptor.Attributes.OfType<PropertyGridOptionsAttribute>().FirstOrDefault();
        if (options != null)
        {
            forceReadWrite = options.ForceReadWrite;
            if (options.PropertyType != null)
            {
                property = Activator.CreateInstance(options.PropertyType, this) as PropertyGridProperty;
            }
        }

        if (property == null)
        {
            options = descriptor.PropertyType.GetCustomAttribute<PropertyGridOptionsAttribute>();
            if (options != null)
            {
                if (!forceReadWrite)
                {
                    forceReadWrite = options.ForceReadWrite;
                }
                if (options.PropertyType != null)
                {
                    property = Activator.CreateInstance(options.PropertyType, this) as PropertyGridProperty;
                }
            }
        }

        property ??= CreateProperty();

        Describe(property, descriptor);
        if (forceReadWrite)
        {
            property.IsReadOnly = false;
        }
        property.OnDescribed();
        property.RefreshValueFromDescriptor();
        return property;
    }

    /// <summary>
    /// Discovers and creates property models for the selected object.
    /// </summary>
    protected virtual void ScanProperties()
    {
        Properties.Clear();
        var props = new List<PropertyGridProperty>();
        foreach (var descriptor in TypeDescriptor.GetProperties(Data).OfType<PropertyDescriptor>())
        {
            if (!descriptor.IsBrowsable)
                continue;

            var property = CreateProperty(descriptor);
            if (property != null)
            {
                props.Add(property);
            }
        }

        if (Data is IPropertyGridObject pga)
        {
            pga.FinalizeProperties(this, props);
        }

        props.Sort();
        foreach (var property in props)
        {
            Properties.Add(property);
        }
    }
}