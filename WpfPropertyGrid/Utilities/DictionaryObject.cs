namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides dictionary-backed properties, change notifications, validation, and cloning for derived objects.
/// </summary>
public abstract class DictionaryObject : INotifyPropertyChanged, INotifyPropertyChanging, IDataErrorInfo, INotifyDataErrorInfo
{
    /// <summary>
    /// Initializes change and validation notifications for the dictionary-backed object.
    /// </summary>
    protected DictionaryObject()
    {
        DictionaryObjectRaiseOnPropertyChanging = true;
        DictionaryObjectRaiseOnPropertyChanged = true;
        DictionaryObjectRaiseOnErrorsChanged = true;
    }

    /// <summary>
    /// Provides the new extension point for derived controls.
    /// </summary>
    protected virtual ConcurrentDictionary<string, DictionaryObjectProperty?> DictionaryObjectProperties { get; } = new(StringComparer.Ordinal); // properties are case sensitive in .NET
    /// <summary>
    /// Occurs when property changing.
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;
    /// <summary>
    /// Occurs when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// Occurs when errors changed.
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    /// <summary>
    /// Occurs when property rollback.
    /// </summary>
    public event EventHandler<DictionaryObjectPropertyRollbackEventArgs>? PropertyRollback;

    /// <summary>
    /// Gets or sets whether property-changing notifications are raised.
    /// </summary>
    protected virtual bool DictionaryObjectRaiseOnPropertyChanging { get; set; }
    /// <summary>
    /// Gets or sets whether property-changed notifications are raised.
    /// </summary>
    protected virtual bool DictionaryObjectRaiseOnPropertyChanged { get; set; }
    /// <summary>
    /// Gets or sets whether validation-error notifications are raised.
    /// </summary>
    protected virtual bool DictionaryObjectRaiseOnErrorsChanged { get; set; }

    /// <summary>
    /// Gets the combined validation message for the specified property.
    /// </summary>
    protected string? DictionaryObjectError => DictionaryObjectGetError(null);
    /// <summary>
    /// Gets validation errors for the specified property.
    /// </summary>
    protected bool DictionaryObjectHasErrors => DictionaryObjectGetErrors(null)?.OfType<object>().Any() == true;
    /// <summary>
    /// Gets whether the object has no validation errors.
    /// </summary>
    protected bool DictionaryObjectHasNoError => !DictionaryObjectHasErrors;

    /// <summary>
    /// Gets the combined validation message for the specified property.
    /// </summary>
    protected virtual string? DictionaryObjectGetError(string? propertyName)
    {
        var errors = DictionaryObjectGetErrors(propertyName);
        if (errors == null)
            return null;

        var error = string.Join(Environment.NewLine, errors.Cast<object>().Select(e => string.Format("{0}", e)));
        return !string.IsNullOrEmpty(error) ? error : null;
    }

    /// <summary>
    /// Gets validation errors for the specified property.
    /// </summary>
    protected virtual IEnumerable DictionaryObjectGetErrors(string? propertyName) => Enumerable.Empty<object>();

    /// <summary>
    /// Raises ErrorsChanged and updates the aggregate error-state properties.
    /// </summary>
    protected virtual void OnErrorsChanged(string? name)
    {
        OnErrorsChanged(this, new DataErrorsChangedEventArgs(name));
        OnPropertyChanged(nameof(DictionaryObjectHasErrors));
        OnPropertyChanged(nameof(DictionaryObjectHasNoError));
    }

    /// <summary>
    /// Raises PropertyChanged for the specified property or event arguments.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => OnPropertyChanged(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Raises ErrorsChanged and updates the aggregate error-state properties.
    /// </summary>
    protected virtual void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(sender, e);
    /// <summary>
    /// Raises PropertyRollback after a property update is reverted.
    /// </summary>
    protected virtual void OnPropertyRollback(object sender, DictionaryObjectPropertyRollbackEventArgs e) => PropertyRollback?.Invoke(sender, e);
    /// <summary>
    /// Raises PropertyChanging before a property value is updated.
    /// </summary>
    protected virtual void OnPropertyChanging(object sender, PropertyChangingEventArgs e) => PropertyChanging?.Invoke(sender, e);
    /// <summary>
    /// Raises PropertyChanged for the specified property or event arguments.
    /// </summary>
    protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(sender, e);

    /// <summary>
    /// Creates a new instance of the runtime object type for cloning.
    /// </summary>
    protected virtual DictionaryObject DictionaryObjectCreateInstance() => (DictionaryObject)Activator.CreateInstance(GetType())!;
    /// <summary>
    /// Creates a clone of this object and copies its dictionary-backed properties.
    /// </summary>
    public virtual object Clone()
    {
        var clone = DictionaryObjectCreateInstance();
        clone.CopyFrom(this);
        return clone;
    }
    /// <summary>
    /// Copies dictionary-backed property values from another object.
    /// </summary>
    public virtual void CopyFrom(DictionaryObject from)
    {
        ArgumentNullException.ThrowIfNull(from);
        foreach (var kv in from.DictionaryObjectProperties)
        {
            DictionaryObjectSetPropertyValue(kv.Value?.Value, kv.Key);
        }
    }

    /// <summary>
    /// Gets a string property value, converting empty text to null.
    /// </summary>
    protected string? DictionaryObjectGetNullifiedPropertyValue(string? defaultValue = null, [CallerMemberName] string? name = null) => DictionaryObjectGetPropertyValue(defaultValue, name)?.Nullify();
    /// <summary>
    /// Gets a dictionary-backed value, converting it to the requested type when needed.
    /// </summary>
    protected T? DictionaryObjectGetPropertyValue<T>([CallerMemberName] string? name = null) => DictionaryObjectGetPropertyValue<T>(default, name);
    /// <summary>
    /// Gets a dictionary-backed value, converting it to the requested type when needed.
    /// </summary>
    protected virtual T? DictionaryObjectGetPropertyValue<T>(T? defaultValue, [CallerMemberName] string? name = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        DictionaryObjectProperties.TryGetValue(name, out var property);
        if (property == null)
            return defaultValue;

        if (!ConversionService.TryConvertType<T>(property.Value, out var value))
            return defaultValue;

        return value;
    }

    /// <summary>
    /// Determines whether two property values are equal.
    /// </summary>
    protected virtual bool DictionaryObjectAreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null)
            return value2 == null;

        if (value2 == null)
            return false;

        return value1.Equals(value2);
    }

    private class ObjectComparer(DictionaryObject obj) : IEqualityComparer<object>
    {
        /// <inheritdoc/>
        public new bool Equals(object? x, object? y) => obj.DictionaryObjectAreValuesEqual(x, y);
        /// <inheritdoc/>
        public int GetHashCode(object obj) => (obj?.GetHashCode()).GetValueOrDefault();
    }

    /// <summary>
    /// Determines whether two validation-error sequences contain the same errors.
    /// </summary>
    protected virtual bool DictionaryObjectAreErrorsEqual(IEnumerable? errors1, IEnumerable? errors2)
    {
        if (errors1 == null && errors2 == null)
            return true;

        var dic = new Dictionary<object, int>(new ObjectComparer(this));
        var left = errors1 != null ? errors1.Cast<object>() : [];
        foreach (var obj in left)
        {
            if (dic.TryGetValue(obj, out var value))
            {
                dic[obj] = ++value;
            }
            else
            {
                dic.Add(obj, 1);
            }
        }

        if (errors2 == null)
            return dic.Count == 0;

        foreach (var obj in errors2)
        {
            if (dic.TryGetValue(obj, out var value))
            {
                dic[obj] = --value;
            }
            else
                return false;
        }
        return dic.Values.All(c => c == 0);
    }

    /// <summary>
    /// Allows a derived type to customize a property before equality testing and notifications.
    /// </summary>
    protected virtual DictionaryObjectProperty? DictionaryObjectUpdatingProperty(DictionaryObjectPropertySetOptions options, string name, DictionaryObjectProperty? oldProperty, DictionaryObjectProperty newProperty) => null;
    /// <summary>
    /// Allows a derived type to customize a property after a value change is accepted.
    /// </summary>
    protected virtual DictionaryObjectProperty? DictionaryObjectUpdatedProperty(DictionaryObjectPropertySetOptions options, string name, DictionaryObjectProperty? oldProperty, DictionaryObjectProperty newProperty) => null;
    /// <summary>
    /// Allows a derived type to customize the property restored after a validation failure.
    /// </summary>
    protected virtual DictionaryObjectProperty? DictionaryObjectRollbackProperty(DictionaryObjectPropertySetOptions options, string name, DictionaryObjectProperty? oldProperty, DictionaryObjectProperty newProperty) => null;
    /// <summary>
    /// Creates the storage object used for a dictionary-backed property.
    /// </summary>
    protected virtual DictionaryObjectProperty? DictionaryObjectCreateProperty() => new();

    /// <summary>
    /// Sets a dictionary-backed property and applies the specified notifications and rollback options.
    /// </summary>
    protected bool DictionaryObjectSetPropertyValue(object? value, [CallerMemberName] string? name = null) => DictionaryObjectSetPropertyValue(value, DictionaryObjectPropertySetOptions.None, name);
    /// <summary>
    /// Sets a dictionary-backed property and applies the specified notifications and rollback options.
    /// </summary>
    protected virtual bool DictionaryObjectSetPropertyValue(object? value, DictionaryObjectPropertySetOptions options, [CallerMemberName] string? name = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        object[]? oldErrors = null;
        var rollbackOnError = options.HasFlag(DictionaryObjectPropertySetOptions.RollbackChangeOnError);
        var onErrorsChanged = !options.HasFlag(DictionaryObjectPropertySetOptions.DontRaiseOnErrorsChanged);
        if (!DictionaryObjectRaiseOnErrorsChanged)
        {
            onErrorsChanged = false;
        }

        if (onErrorsChanged || rollbackOnError)
        {
            oldErrors = DictionaryObjectGetErrors(name)?.OfType<object>()?.ToArray();
        }

        var forceChanged = options.HasFlag(DictionaryObjectPropertySetOptions.ForceRaiseOnPropertyChanged);
        var onChanged = !options.HasFlag(DictionaryObjectPropertySetOptions.DontRaiseOnPropertyChanged);
        if (!DictionaryObjectRaiseOnPropertyChanged)
        {
            onChanged = false;
            forceChanged = false;
        }

        var newProp = DictionaryObjectCreateProperty() ?? throw new InvalidOperationException();
        newProp.Value = value;
        DictionaryObjectProperty? oldProp = null;
        var finalProp = DictionaryObjectProperties.AddOrUpdate(name, newProp, (k, o) =>
        {
            oldProp = o;
            var updating = DictionaryObjectUpdatingProperty(options, k, o, newProp);
            if (updating != null)
                return updating;

            var testEquality = !options.HasFlag(DictionaryObjectPropertySetOptions.DontTestValuesForEquality);
            if (testEquality && o != null && DictionaryObjectAreValuesEqual(value, o.Value))
            {
                onChanged = false;
                return o;
            }

            var onChanging = !options.HasFlag(DictionaryObjectPropertySetOptions.DontRaiseOnPropertyChanging);
            if (!DictionaryObjectRaiseOnPropertyChanging)
            {
                onChanging = false;
            }

            if (onChanging)
            {
                var e = new DictionaryObjectPropertyChangingEventArgs(name, oldProp, newProp);
                OnPropertyChanging(this, e);
                if (e.Cancel)
                    return o;
            }

            var updated = DictionaryObjectUpdatedProperty(options, k, o, newProp);
            if (updated != null)
                return updated;

            return newProp;
        });

        if (forceChanged || onChanged)
        {
            var rollbacked = false;
            if (rollbackOnError)
            {
                if ((DictionaryObjectGetErrors(name)?.Cast<object>().Any()) == true)
                {
                    var rolled = DictionaryObjectRollbackProperty(options, name, oldProp, newProp) ?? oldProp;
                    if (rolled == null)
                    {
                        DictionaryObjectProperties.TryRemove(name, out var dop);
                    }
                    else
                    {
                        DictionaryObjectProperties.AddOrUpdate(name, rolled, (k, o) => rolled);
                    }

                    var e = new DictionaryObjectPropertyRollbackEventArgs(name, rolled, value);
                    OnPropertyRollback(this, e);
                    rollbacked = true;
                }
            }

            if (!rollbacked)
            {
                var e = new DictionaryObjectPropertyChangedEventArgs(name, oldProp, newProp);
                OnPropertyChanged(this, e);

                if (onErrorsChanged)
                {
                    var newErrors = DictionaryObjectGetErrors(name);
                    if (!DictionaryObjectAreErrorsEqual(oldErrors, newErrors))
                    {
                        OnErrorsChanged(name);
                    }
                }
                return true;
            }
        }

        return false;
    }

    string IDataErrorInfo.Error => DictionaryObjectError!;
    string IDataErrorInfo.this[string columnName] => DictionaryObjectGetError(columnName)!;
    bool INotifyDataErrorInfo.HasErrors => !DictionaryObjectHasErrors;
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => DictionaryObjectGetErrors(propertyName);
}
