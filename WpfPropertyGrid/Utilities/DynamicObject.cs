namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides a mutable descriptor-based object with runtime-defined properties and metadata.
/// </summary>
public class DynamicObject : ICustomTypeDescriptor, IFormattable, INotifyPropertyChanged, IDataErrorInfo
{
    private readonly List<Attribute> _attributes = [];
    private readonly List<EventDescriptor> _events = [];
    private readonly List<PropertyDescriptor> _properties = [];
    private readonly Dictionary<Type, object?> _editors = [];
    private readonly Dictionary<string, object?> _values = [];
    /// <summary>
    /// Occurs when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// Defines and returns a dynamic property with the supplied type and metadata.
    /// </summary>
    public virtual DynamicObjectProperty AddProperty(string name, Type type, IEnumerable<Attribute>? attributes)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);

        if (_properties.Find(x => x.Name == name) != null)
            throw new ArgumentException("Property '" + name + "' is already defined", nameof(name));

        var dop = CreateProperty(name, type, attributes);
        _properties.Add(dop);
        return dop;
    }
    /// <summary>
    /// Defines and returns a dynamic property with the supplied type and metadata.
    /// </summary>
    public virtual DynamicObjectProperty AddProperty(string name, Type type, object? defaultValue, bool readOnly, int sortOrder, Attribute[]? attributes)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);
        if (_properties.Find(x => x.Name == name) != null)
            throw new ArgumentException("Property '" + name + "' is already defined", nameof(name));

        List<Attribute> newAtts;
        if (attributes != null)
        {
            newAtts = [.. attributes];
        }
        else
        {
            newAtts = [];
        }

        newAtts.RemoveAll(a => a is ReadOnlyAttribute);
        newAtts.RemoveAll(a => a is DefaultValueAttribute);

        if (readOnly)
        {
            newAtts.Add(new ReadOnlyAttribute(true));
        }
        newAtts.Add(new DefaultValueAttribute(defaultValue));

        var dop = CreateProperty(name, type, [.. newAtts]);
        dop.SortOrder = sortOrder;
        _properties.Add(dop);
        return dop;
    }
    /// <summary>
    /// Sorts dynamic properties using the supplied comparer.
    /// </summary>
    public void SortProperties(IComparer<PropertyDescriptor> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        _properties.Sort(comparer);
    }
    /// <summary>
    /// Gets a named dynamic property value, converting it to the requested type when needed.
    /// </summary>
    public virtual object? GetPropertyValue(string name, Type type, object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);

        defaultValue = ConversionService.ConvertObjectType(defaultValue, type);
        if (_values.TryGetValue(name, out var obj))
            return ConversionService.ConvertObjectType(obj, type, defaultValue);

        return defaultValue;
    }
    /// <summary>
    /// Gets a named dynamic property value, converting it to the requested type when needed.
    /// </summary>
    public virtual T? GetPropertyValue<T>(string name, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_values.TryGetValue(name, out var obj))
            return ConversionService.ConvertType(obj, defaultValue);

        return defaultValue;
    }
    /// <summary>
    /// Attempts to get a named dynamic property value and reports whether it exists.
    /// </summary>
    public virtual bool TryGetPropertyValue(string name, out object? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _values.TryGetValue(name, out value);
    }
    /// <summary>
    /// Gets a named dynamic property value, converting it to the requested type when needed.
    /// </summary>
    public virtual object? GetPropertyValue(string name, object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_values.TryGetValue(name, out var obj))
            return obj;

        return defaultValue;
    }
    /// <summary>
    /// Sets a named dynamic property value and raises a change notification when it changes.
    /// </summary>
    public virtual void SetPropertyValue(string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        var exists = _values.TryGetValue(name, out var existing);
        if (!exists)
        {
            _values.Add(name, value);
        }
        else
        {
            if (value == null)
            {
                if (existing == null)
                    return;

            }
            else if (value.Equals(existing))
                return;

            _values[name] = value;
        }
        OnPropertyChanged(name);
    }

    /// <summary>
    /// Creates a descriptor for a runtime-defined property.
    /// </summary>
    protected virtual DynamicObjectProperty CreateProperty(string name, Type type, IEnumerable<Attribute>? attributes)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new DynamicObjectProperty(name, type, attributes);
    }
    /// <summary>
    /// Gets or sets the to string name.
    /// </summary>
    public virtual string? ToStringName { get; set; }
    /// <inheritdoc/>
    public override string ToString() => ToStringName ?? base.ToString() ?? string.Empty;
    /// <inheritdoc/>
    public virtual string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format))
            return ToString();

        return Extensions.Format(this, format, formatProvider);
    }
    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    public virtual IList<Attribute> Attributes => _attributes;
    AttributeCollection ICustomTypeDescriptor.GetAttributes() => new([.. _attributes]);
    /// <summary>
    /// Gets or sets the class name.
    /// </summary>
    public virtual string? ClassName { get; set; }
    string? ICustomTypeDescriptor.GetClassName() => ClassName;
    /// <summary>
    /// Gets or sets the component name.
    /// </summary>
    public virtual string? ComponentName { get; set; }
    string? ICustomTypeDescriptor.GetComponentName() => ComponentName;
    /// <summary>
    /// Gets or sets the converter.
    /// </summary>
    public virtual TypeConverter? Converter { get; set; }
    TypeConverter? ICustomTypeDescriptor.GetConverter() => Converter;
    /// <summary>
    /// Gets or sets the default event.
    /// </summary>
    public virtual EventDescriptor? DefaultEvent { get; set; }
    EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => DefaultEvent;
    /// <summary>
    /// Gets or sets the default property.
    /// </summary>
    public virtual PropertyDescriptor? DefaultProperty { get; set; }
    PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => DefaultProperty;
    /// <summary>
    /// Gets the editors.
    /// </summary>
    public virtual IDictionary<Type, object?> Editors => _editors;
    object? ICustomTypeDescriptor.GetEditor(Type editorBaseType)
    {
        ArgumentNullException.ThrowIfNull(editorBaseType);
        if (_editors.TryGetValue(editorBaseType, out var editor))
            return editor;

        return null;
    }
    /// <summary>
    /// Gets the events.
    /// </summary>
    public virtual IList<EventDescriptor> Events => _events;

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes)
    {
        if (attributes == null || attributes.Length == 0)
            return ((ICustomTypeDescriptor)this).GetEvents();

        var list = new List<EventDescriptor>();
        foreach (EventDescriptor evt in _events)
        {
            if (evt.Attributes.Count == 0)
                continue;

            var cont = false;
            foreach (var att in attributes)
            {
                if (!HasMatchingAttribute(evt, att))
                {
                    cont = true;
                    break;
                }
            }

            if (!cont)
            {
                list.Add(evt);
            }
        }
        return new EventDescriptorCollection([.. list]);
    }

    private static bool HasMatchingAttribute(MemberDescriptor member, Attribute attribute)
    {
        var att = member.Attributes[attribute.GetType()];
        if (att == null)
            return attribute.IsDefaultAttribute();

        return attribute.Match(att);
    }

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => new([.. _events]);

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
    {
        if (attributes == null || attributes.Length == 0)
            return ((ICustomTypeDescriptor)this).GetProperties();

        var list = new List<PropertyDescriptor>();
        foreach (PropertyDescriptor prop in _properties)
        {
            if (prop.Attributes.Count == 0)
                continue;

            var cont = false;
            foreach (Attribute att in attributes)
            {
                if (!HasMatchingAttribute(prop, att))
                {
                    cont = true;
                    break;
                }
            }

            if (!cont)
            {
                list.Add(prop);
            }
        }
        return new PropertyDescriptorCollection([.. list]);
    }

    /// <summary>
    /// Raises PropertyChanged for the named dynamic property.
    /// </summary>
    protected virtual void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    /// <summary>
    /// Gets the properties.
    /// </summary>
    public virtual IList<PropertyDescriptor> Properties => _properties;

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => new([.. _properties]);
    object? ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;
    /// <summary>
    /// Validates the specified member and returns any validation messages.
    /// </summary>
    public string? ValidateMember(CultureInfo? culture = null, string? memberName = null) => ValidateMember(culture, memberName, null);
    /// <summary>
    /// Validates the specified member and returns any validation messages.
    /// </summary>
    public virtual string? ValidateMember(CultureInfo? culture, string? memberName, string? separator)
    {
        culture ??= Thread.CurrentThread.CurrentUICulture;
        separator ??= Environment.NewLine;
        var list = new List<ValidationException>();
        ValidateMember(list, culture, memberName);
        if (list.Count == 0)
            return null;

        return string.Join(separator, list.Select(l => l.GetAllMessages(separator)));
    }
    /// <summary>
    /// Validates the specified member and returns any validation messages.
    /// </summary>
    public virtual void ValidateMember(IList<ValidationException> list, CultureInfo? culture = null, string? memberName = null) => ArgumentNullException.ThrowIfNull(list);

    string IDataErrorInfo.Error => ValidateMember(null, null) ?? string.Empty;
    string IDataErrorInfo.this[string? columnName] => ValidateMember(null, columnName) ?? string.Empty;
}
