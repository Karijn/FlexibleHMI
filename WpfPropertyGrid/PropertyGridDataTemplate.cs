namespace WpfPropertyGrid;

/// <summary>
/// Associates a data template with the property or collection-item types it can edit.
/// </summary>
[ContentProperty(nameof(DataTemplate))]
public class PropertyGridDataTemplate
{
    private class NullableEnum { }
    /// <summary>
    /// Gets the marker type used to identify nullable-enum template registrations.
    /// </summary>
    public static readonly Type NullableEnumType = typeof(NullableEnum);

    private readonly Lazy<List<Type>> _resolvedPropertyTypes;
    private readonly Lazy<List<Type>> _resolvedCollectionItemPropertyTypes;
    /// <summary>
    /// Initializes a new instance of PropertyGridDataTemplate.
    /// </summary>
    public PropertyGridDataTemplate()
    {
        _resolvedPropertyTypes = new(GetResolvedPropertyTypes);
        _resolvedCollectionItemPropertyTypes = new(GetResolvedCollectionItemPropertyTypes);
    }
    /// <summary>
    /// Gets or sets the type names for properties to which this template applies.
    /// </summary>
    public string? PropertyType { get; set; }
    /// <summary>
    /// Gets or sets the type names for collection items to which this template applies.
    /// </summary>
    public string? CollectionItemPropertyType { get; set; }
    /// <summary>
    /// Gets or sets the data template returned when this definition matches.
    /// </summary>
    public DataTemplate? DataTemplate { get; set; }
    /// <summary>
    /// Gets or sets whether the template applies to collection properties.
    /// </summary>
    public bool? IsCollection { get; set; }
    /// <summary>
    /// Gets or sets whether the editor is read-only.
    /// </summary>
    public bool? IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets whether template resolution produced an error.
    /// </summary>
    public bool? IsError { get; set; }
    /// <summary>
    /// Gets or sets whether the template configuration is valid.
    /// </summary>
    public bool? IsValid { get; set; }
    /// <summary>
    /// Gets or sets whether the template represents a flags enum.
    /// </summary>
    public bool? IsFlagsEnum { get; set; }
    /// <summary>
    /// Gets or sets whether collection items are value types.
    /// </summary>
    public bool? IsCollectionItemValueType { get; set; }
    /// <summary>
    /// Gets or sets whether the property values are value types.
    /// </summary>
    public bool? IsValueType { get; set; }
    /// <summary>
    /// Gets or sets the property category required for a match.
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// Gets or sets the property name required for a match.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets the types resolved from PropertyType.
    /// </summary>
    public virtual IReadOnlyList<Type> ResolvedPropertyTypes => _resolvedPropertyTypes.Value;
    /// <summary>
    /// Gets the types resolved from CollectionItemPropertyType.
    /// </summary>
    public virtual IReadOnlyList<Type> ResolvedCollectionItemPropertyTypes => _resolvedCollectionItemPropertyTypes.Value;

    private List<Type> GetResolvedPropertyTypes()
    {
        var types = new List<Type>();
        var names = PropertyType?.SplitToList<string>('|') ?? [];
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            Type? type;
            if (name == "System.Nullable`1[System.Enum]")
            {
                type = NullableEnumType;
            }
            else
            {
                type = TypeResolutionService.ResolveType(name);
            }

            if (type != null)
            {
                types.Add(type);
            }
        }
        return types;
    }

    private List<Type> GetResolvedCollectionItemPropertyTypes()
    {
        var types = new List<Type>();
        var names = CollectionItemPropertyType?.SplitToList<string>('|') ?? [];
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var type = TypeResolutionService.ResolveType(name);
            if (type != null)
            {
                types.Add(type);
            }
        }
        return types;
    }
}