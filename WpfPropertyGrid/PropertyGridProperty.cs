namespace WpfPropertyGrid;
/// <summary>
/// Represents an editable property, including its value, metadata, editor settings, and validation state.
/// </summary>
public class PropertyGridProperty : DictionaryObject, IComparable, IComparable<PropertyGridProperty>
{
    /// <summary>
    /// Occurs when event.
    /// </summary>
    public event EventHandler<PropertyGridEventArgs>? Event;
    private string? _defaultEditorResourceKey;
    private object? _clonedValue;
    private bool _valueCloned;
    /// <summary>
    /// Initializes a new instance of PropertyGridProperty.
    /// </summary>
    public PropertyGridProperty(PropertyGridDataProvider dataProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProvider);

        DataProvider = dataProvider;
        PropertyType = typeof(object);
        Attributes = dataProvider.CreateDynamicObject() ?? throw new NotSupportedException();
        TypeAttributes = dataProvider.CreateDynamicObject() ?? throw new NotSupportedException();
    }
    /// <summary>
    /// Gets or sets the data provider.
    /// </summary>
    public PropertyGridDataProvider DataProvider { get; }
    /// <summary>
    /// Gets or sets the has errors.
    /// </summary>
    public bool HasErrors => DictionaryObjectHasErrors;
    /// <summary>
    /// Gets or sets the has no error.
    /// </summary>
    public bool HasNoError => DictionaryObjectHasNoError;
    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public virtual int SortOrder { get; set; }
    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    public virtual Utilities.DynamicObject Attributes { get; }
    /// <summary>
    /// Gets or sets the type attributes.
    /// </summary>
    public virtual Utilities.DynamicObject TypeAttributes { get; }
    /// <summary>
    /// Gets or sets the display and editing options for the property.
    /// </summary>
    public virtual PropertyGridOptionsAttribute? Options { get; set; }
    /// <summary>
    /// Gets or sets application-specific data associated with the property.
    /// </summary>
    public virtual object? Tag { get; set; }
    /// <summary>
    /// Gets or sets the declared type of the property.
    /// </summary>
    public virtual Type? PropertyType { get => DictionaryObjectGetPropertyValue<Type>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public virtual string? Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets whether an error occurred while reading or editing the property.
    /// </summary>
    public virtual bool IsError { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets whether the property value is an enum.
    /// </summary>
    public virtual bool IsEnum { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets whether the enum supports combining flag values.
    /// </summary>
    public virtual bool IsFlagsEnum { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the category used to group the property.
    /// </summary>
    public virtual string? Category { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the user-facing property name.
    /// </summary>
    public virtual string? DisplayName { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the property description.
    /// </summary>
    public virtual string? Description { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets whether a default value is defined for the property.
    /// </summary>
    public virtual bool HasDefaultValue { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the descriptor that reads and writes the property value.
    /// </summary>
    public virtual PropertyDescriptor? Descriptor { get => DictionaryObjectGetPropertyValue<PropertyDescriptor>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the converter used to format or convert the property value.
    /// </summary>
    public virtual TypeConverter? Converter { get => DictionaryObjectGetPropertyValue<TypeConverter>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the method used to retrieve the property value.
    /// </summary>
    public virtual MethodInfo? GetMethod { get => DictionaryObjectGetPropertyValue<MethodInfo>(); set => DictionaryObjectSetPropertyValue(value); }
    /// <summary>
    /// Gets or sets the tool tip.
    /// </summary>
    public virtual string? ToolTip => Description.Nullify() ?? DisplayName.Nullify() ?? Name;
    /// <summary>
    /// Gets or sets the default value used by the property editor.
    /// </summary>
    public virtual object? DefaultValue
    {
        get => DictionaryObjectGetPropertyValue<object>();
        set
        {
            if (DictionaryObjectSetPropertyValue(value))
            {
                OnPropertyChanged(nameof(IsDefaultValue));
            }
        }
    }
    /// <summary>
    /// Gets or sets the default editor resource key.
    /// </summary>
    public virtual string DefaultEditorResourceKey
    {
        get
        {
            if (_defaultEditorResourceKey != null)
                return _defaultEditorResourceKey;

            if (IsCollection)
                return "CollectionEditorWindow";

            return "ObjectEditorWindow";
        }
        set => _defaultEditorResourceKey = value;
    }
    /// <summary>
    /// Gets or sets the collection item property type.
    /// </summary>
    public virtual Type? CollectionItemPropertyType
    {
        get
        {
            if (!IsCollection || PropertyType == null)
                return null;

            return Extensions.GetElementType(PropertyType);
        }
    }
    /// <summary>
    /// Gets whether the current value equals the configured default value.
    /// </summary>
    public virtual bool IsDefaultValue
    {
        get
        {
            if (!HasDefaultValue)
                return false;

            if (DefaultValue == null)
                return Value == null;

            return DefaultValue.Equals(Value);
        }
    }
    /// <summary>
    /// Gets or sets whether the property can be edited.
    /// </summary>
    public bool IsReadWrite { get => !IsReadOnly; set => IsReadOnly = !value; }
    /// <summary>
    /// Gets or sets whether the property is read-only.
    /// </summary>
    public virtual bool IsReadOnly
    {
        get
        {
            var def = false;
            if (DataProvider != null && DataProvider.Grid != null && DataProvider.Grid.IsReadOnly)
            {
                def = true;
            }

            return DictionaryObjectGetPropertyValue(def);
        }
        set
        {
            if (DictionaryObjectSetPropertyValue(value))
            {
                OnPropertyChanged(nameof(IsReadWrite));
            }
        }
    }
    /// <summary>
    /// Gets whether the collection element type is a value type.
    /// </summary>
    public virtual bool IsCollectionItemValueType => CollectionItemPropertyType != null && CollectionItemPropertyType.IsValueType;
    /// <summary>
    /// Gets whether the property type is a value type.
    /// </summary>
    public virtual bool IsValueType => PropertyType != null && PropertyType.IsValueType;
    /// <summary>
    /// Gets the collection count.
    /// </summary>
    public virtual int CollectionCount
    {
        get
        {
            if (Value is IEnumerable enumerable)
                return enumerable.Cast<object>().Count();

            return 0;
        }
    }
    /// <summary>
    /// Gets whether the property type represents a non-string enumerable.
    /// </summary>
    public virtual bool IsCollection
    {
        get
        {
            if (PropertyType == null)
                return false;

            if (PropertyType == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(PropertyType);
        }
    }
    /// <summary>
    /// Gets or sets the property value as a nullable Boolean.
    /// </summary>
    public bool? BooleanValue
    {
        get
        {
            if (Value == null)
                return null;

            var def = HasDefaultValue && ConversionService.ConvertType(DefaultValue, false);
            return ConversionService.ConvertType(Value, def);
        }
        set
        {
            if (value == null)
            {
                Value = null;
                return;
            }

            Value = value.Value;
        }
    }
    /// <summary>
    /// Gets a clone of the current value when it implements ICloneable; otherwise, gets the value itself.
    /// </summary>
    public virtual object? ClonedValue
    {
        get
        {
            CloneValue(false);
            return _clonedValue;
        }
    }
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public virtual object? Value
    {
        get => DictionaryObjectGetPropertyValue<object>();
        set
        {
            object? changedValue = null;
            if (PropertyType != null && !TryChangeType(value, PropertyType, CultureInfo.CurrentCulture, out changedValue))
            {
                //throw new ArgumentException("Cannot convert value {" + value + "} to type '" + PropertyType.FullName + "'.");
            }
            if (Descriptor != null)
            {
                try
                {
                    Descriptor.SetValue(DataProvider.Data, changedValue);
                    var finalValue = Descriptor.GetValue(DataProvider.Data);
                    DictionaryObjectSetPropertyValue(finalValue);
                    OnValueChanged();
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Cannot set value {" + value + "} to object.", e);
                }
            }
        }
    }
    /// <summary>
    /// Gets or sets the current value using the property converter or configured conversion service.
    /// </summary>
    public virtual string? TextValue
    {
        get
        {
            if (Converter != null && Converter.CanConvertTo(typeof(string)))
                return Converter.ConvertTo(Value, typeof(string)) as string;

            return ConversionService.ConvertType<string>(Value);
        }
        set
        {
            if (Converter != null)
            {
                if (Converter.CanConvertFrom(typeof(string)) && value != null)
                {
                    Value = Converter.ConvertFrom(value);
                    return;
                }

                if (Descriptor != null && Converter.CanConvertTo(Descriptor.PropertyType))
                {
                    Value = Converter.ConvertTo(value, Descriptor.PropertyType);
                    return;
                }
            }

            if (Descriptor != null)
            {
                if (ConversionService.TryConvertObjectType(value, Descriptor.PropertyType, out var v))
                {
                    Value = v;
                    return;
                }
            }
            Value = value;
        }
    }
    /// <inheritdoc/>
    public override string ToString() => Name ?? string.Empty;
    /// <summary>
    /// Notifies dependent property-grid values that the property value has changed.
    /// </summary>
    public virtual void OnValueChanged()
    {
        OnPropertyChanged(nameof(TextValue));
        OnPropertyChanged(nameof(BooleanValue));
        OnPropertyChanged(nameof(IsCollection));
        OnPropertyChanged(nameof(CollectionCount));
        OnPropertyChanged(nameof(IsDefaultValue));
    }
    /// <summary>
    /// Marks the cached clone as stale so it will be recreated on the next access.
    /// </summary>
    public void ResetClonedValue() => _valueCloned = false;
    /// <summary>
    /// Refreshes the cached clone of the current value.
    /// </summary>
    public virtual void CloneValue(bool refresh)
    {
        if (_valueCloned && !refresh)
            return;

        _clonedValue = Value is ICloneable c ? c.Clone() : Value;
        _valueCloned = true;
    }
    /// <summary>
    /// Determines whether a routed command can execute for the represented property.
    /// </summary>
    public virtual void CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (Value is IPropertyGridCommandHandler handler)
        {
            handler.CanExecute(this, sender, e);
        }
    }
    /// <summary>
    /// Handles execution of a routed command for the represented property.
    /// </summary>
    public virtual void Executed(object? sender, ExecutedRoutedEventArgs e)
    {
        if (Value is IPropertyGridCommandHandler handler)
        {
            handler.Executed(this, sender, e);
        }
    }
    /// <summary>
    /// Raises the property event with the supplied event arguments.
    /// </summary>
    public virtual void OnEvent(object? sender, PropertyGridEventArgs e) => Event?.Invoke(sender, e);
    /// <summary>
    /// Updates matching bindings in the property-grid cell for this property.
    /// </summary>
    public void UpdateCellBindings(Action<BindingExpression> action) => UpdateCellBindings(null, null, action);
    /// <summary>
    /// Updates matching bindings in the property-grid cell for this property.
    /// </summary>
    public void UpdateCellBindings(string? childName, Action<BindingExpression> action) => UpdateCellBindings(childName, null, action);
    /// <summary>
    /// Updates matching bindings in the property-grid cell for this property.
    /// </summary>
    public virtual void UpdateCellBindings(string? childName, Func<Binding, bool>? where, Action<BindingExpression> action) => DataProvider.Grid.UpdateCellBindings(this, childName, where, action);
    /// <summary>
    /// Attempts to convert the supplied value to the property type.
    /// </summary>
    protected virtual bool TryChangeType(object? value, Type type, IFormatProvider? provider, out object? changedValue)
    {
        ArgumentNullException.ThrowIfNull(type);
        return ConversionService.TryConvertObjectType(value, type, provider, out changedValue);
    }
    /// <summary>
    /// Runs after the property has been populated from its descriptor.
    /// </summary>
    public virtual void OnDescribed()
    {
    }
    /// <summary>
    /// Refreshes the cached value from its descriptor or configured getter method.
    /// </summary>
    public virtual void RefreshValueFromDescriptor(DictionaryObjectPropertySetOptions options = DictionaryObjectPropertySetOptions.None)
    {
        if (Descriptor == null && GetMethod == null)
            return;

        try
        {
            var valueSet = false;
            object? value = null;
            if (GetMethod != null)
            {
                var objects = new List<object?>();
                var parameters = GetMethod.GetParameters();
                if (parameters.Length == 0)
                {
                    valueSet = true;
                }
                else if (parameters.Length == 1)
                {
                    objects.Add(this);
                    valueSet = true;
                }

                if (valueSet)
                {
                    if (GetMethod.IsStatic)
                    {
                        value = GetMethod.Invoke(null, [.. objects]);
                    }
                    else
                    {
                        value = GetMethod.Invoke(DataProvider.Data, [.. objects]);
                    }
                }
            }

            if (!valueSet)
            {
                value = Descriptor!.GetValue(DataProvider.Data);
            }

            DictionaryObjectSetPropertyValue(value, options, nameof(Value));
            OnValueChanged();
        }
        catch (Exception e)
        {
            if (PropertyType == typeof(string))
            {
                Value = e.GetAllMessages();
            }
            IsError = true;
        }
    }

    int IComparable.CompareTo(object? obj) => CompareTo(obj as PropertyGridProperty);
    /// <summary>
    /// Compares properties by sort order and then by display name.
    /// </summary>
    public virtual int CompareTo(PropertyGridProperty? other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (SortOrder != 0)
            return SortOrder.CompareTo(other.SortOrder);

        if (other.SortOrder != 0)
            return -other.SortOrder.CompareTo(0);

        if (DisplayName == null)
            return 1;

        return string.Compare(DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Determines whether a type is an enum or nullable enum and returns its underlying enum type.
    /// </summary>
    public static bool IsEnumOrNullableEnum(Type type, [NotNullWhen(true)] out Type? enumType, out bool nullable)
    {
        ArgumentNullException.ThrowIfNull(type);

        nullable = false;
        if (type.IsEnum)
        {
            enumType = type;
            return true;
        }

        if (type.Name == typeof(Nullable<>).Name)
        {
            var args = type.GetGenericArguments();
            if (args.Length == 1 && args[0].IsEnum)
            {
                enumType = args[0];
                nullable = true;
                return true;
            }
        }

        enumType = null;
        return false;
    }
    /// <summary>
    /// Gets the property-grid property associated with the routed event source, if any.
    /// </summary>
    public static PropertyGridProperty? FromEvent(RoutedEventArgs e)
    {
        if (e == null)
            return null;

        if (e.OriginalSource is not FrameworkElement fe)
            return null;

        return fe.DataContext as PropertyGridProperty;
    }
}