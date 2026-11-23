namespace WpfPropertyGrid;

/// <summary>
/// Configures property-grid metadata, enum choices, and custom editors for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class PropertyGridOptionsAttribute : Attribute
{
    private Lazy<string[]?> _finalEnumNames;
    private string[]? _enumNames;
    /// <summary>
    /// Initializes a new instance of PropertyGridOptionsAttribute.
    /// </summary>
    public PropertyGridOptionsAttribute()
    {
        _finalEnumNames = new(GetFinalEnumNames);
    }
    /// <summary>
    /// Gets or sets the enum names.
    /// </summary>
    public string[]? EnumNames
    {
        get => _enumNames;
        set
        {
            if (_enumNames == value)
                return;

            _enumNames = value;
            _finalEnumNames = new(GetFinalEnumNames);
        }
    }
    /// <summary>
    /// Gets or sets the enum values.
    /// </summary>
    public object[]? EnumValues { get; set; }
    /// <summary>
    /// Gets or sets whether the property is displayed using enum choices.
    /// </summary>
    public bool IsEnum { get; set; }
    /// <summary>
    /// Gets or sets whether the property is a flags enum.
    /// </summary>
    public bool IsFlagsEnum { get; set; }
    /// <summary>
    /// Gets or sets the highest flag bit included when expanding enum choices.
    /// </summary>
    public int EnumMaxPower { get; set; }
    /// <summary>
    /// Gets or sets the collection editor has only one column.
    /// </summary>
    public bool CollectionEditorHasOnlyOneColumn { get; set; }
    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }
    /// <summary>
    /// Gets or sets the editor data template property path.
    /// </summary>
    public string? EditorDataTemplatePropertyPath { get; set; }
    /// <summary>
    /// Gets or sets the editor data template selector property path.
    /// </summary>
    public string? EditorDataTemplateSelectorPropertyPath { get; set; }
    /// <summary>
    /// Gets or sets the editor type.
    /// </summary>
    public Type? EditorType { get; set; }
    /// <summary>
    /// Gets or sets the editor resource key.
    /// </summary>
    public string? EditorResourceKey { get; set; }
    /// <summary>
    /// Gets or sets the editor data template resource key.
    /// </summary>
    public object? EditorDataTemplateResourceKey { get; set; }
    /// <summary>
    /// Gets or sets the type used by the property editor.
    /// </summary>
    public Type? PropertyType { get; set; }
    /// <summary>
    /// Gets or sets the force read write.
    /// </summary>
    public bool ForceReadWrite { get; set; }
    /// <summary>
    /// Gets or sets the has default value.
    /// </summary>
    public bool HasDefaultValue { get; set; }
    /// <summary>
    /// Gets or sets the force property changed.
    /// </summary>
    public bool ForcePropertyChanged { get; set; }
    /// <summary>
    /// Gets or sets the default value used to initialize or reset the property.
    /// </summary>
    public object? DefaultValue { get; set; }
    /// <summary>
    /// Gets or sets the get value method name.
    /// </summary>
    public string? GetValueMethodName { get; set; }

    internal string[]? FinalEnumNames => _finalEnumNames.Value;
    private string[]? GetFinalEnumNames()
    {
        if (EnumNames == null)
            return null;

        var list = new List<string>();
        foreach (var name in EnumNames)
        {
            if (name != null)
            {
                // note the second char is not a comma (unicode character #44), it's unicode character #130 but it looks the same
                // it's a trick so we can still use commas in enum names and it doesn't break parsing
                list.Add(name.Replace(',', '‚'));
            }
        }
        return [.. list];
    }
    /// <summary>
    /// Selects a data template for the supplied property-grid item.
    /// </summary>
    public static DataTemplate? SelectTemplate(PropertyGridProperty property, object? item, DependencyObject container)
    {
        ArgumentNullException.ThrowIfNull(property);

        var att = FromProperty(property);
        if (att == null)
            return null;

        if (att.EditorDataTemplateResourceKey != null)
        {
            if (Application.Current != null)
            {
                var dt = (DataTemplate)Application.Current.TryFindResource(att.EditorDataTemplateResourceKey);
                if (dt != null)
                    return dt;
            }

            if (container is FrameworkElement fe)
            {
                var obj = fe.TryFindResource(att.EditorDataTemplateResourceKey);
                if (obj is DataTemplate dt)
                    return dt;
            }

            return null;
        }

        if (att.EditorType != null)
        {
            var editor = Activator.CreateInstance(att.EditorType);
            if (att.EditorDataTemplateSelectorPropertyPath != null)
            {
                var dts = DataBindingEvaluator.GetPropertyValue(editor, att.EditorDataTemplateSelectorPropertyPath) as DataTemplateSelector;
                return dts?.SelectTemplate(item, container);
            }

            if (att.EditorDataTemplatePropertyPath != null)
                return DataBindingEvaluator.GetPropertyValue(editor, att.EditorDataTemplatePropertyPath) as DataTemplate;

            if (editor is ContentControl cc)
            {
                if (cc.ContentTemplateSelector != null)
                {
                    var template = cc.ContentTemplateSelector.SelectTemplate(item, container);
                    if (template != null)
                        return template;
                }

                return cc.ContentTemplate;
            }

            if (editor is ContentPresenter cp)
            {
                if (cp.ContentTemplateSelector != null)
                {
                    var template = cp.ContentTemplateSelector.SelectTemplate(item, container);
                    if (template != null)
                        return template;
                }

                return cp.ContentTemplate;
            }
        }
        return null;
    }
    /// <summary>
    /// Gets the property-grid options associated with a property, if present.
    /// </summary>
    public static PropertyGridOptionsAttribute? FromProperty(PropertyGridProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var att = property.Options;
        if (att != null)
            return att;

        if (property.Descriptor != null)
        {
            att = property.Descriptor.Attributes.OfType<PropertyGridOptionsAttribute>().FirstOrDefault();
        }
        return att;
    }
}