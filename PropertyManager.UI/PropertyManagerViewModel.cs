using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace PropertyManager.UI;

/// <summary>Manages property and struct editors for a document-tree item.</summary>
public partial class PropertyManagerViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentItemDetached
{
    private readonly IVariableHandler _variableHandler;
    private readonly IStructTypeRegistry _structTypeRegistry;
    private bool _isDetached;

    /// <summary>Creates an editor connected to the shared property store and type registry.</summary>
    /// <param name="variableHandler">The shared variable store.</param>
    /// <param name="structTypeRegistry">The registry of struct definitions.</param>
    public PropertyManagerViewModel(IVariableHandler variableHandler, IStructTypeRegistry structTypeRegistry)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        _structTypeRegistry = structTypeRegistry ?? throw new ArgumentNullException(nameof(structTypeRegistry));

        Name = PropertyManagerCreator.defaultname;
        Description = PropertyManagerCreator.defaultdescription;
        Image = PropertyManagerCreator.GetIcon();
        _variableHandler.VariableChanged += OnVariableChanged;
        _structTypeRegistry.Changed += OnStructTypesChanged;
        LoadProperties();
        RefreshStructTypes();
    }

    /// <summary>Gets the property rows displayed in the editor.</summary>
    public ObservableCollection<NameValue> Properties { get; } = [];

    /// <summary>Unsubscribes from shared property and type changes when the item is detached.</summary>
    public void OnDocumentItemDetached()
    {
        _isDetached = true;
        _variableHandler.VariableChanged -= OnVariableChanged;
        _structTypeRegistry.Changed -= OnStructTypesChanged;
    }

    /// <summary>Restores change subscriptions after the item is reattached.</summary>
    public void OnDocumentItemReattached()
    {
        if (_isDetached)
        {
            _isDetached = false;
            _variableHandler.VariableChanged += OnVariableChanged;
            _structTypeRegistry.Changed += OnStructTypesChanged;
        }
    }

    /// <summary>Gets the property types offered by the editor.</summary>
    public ObservableCollection<string> PropertyTypes { get; } = ["Integer", "Double", "Boolean", "String", "DateTime", "TimeSpan", "Color", "Struct", "Array"];

    /// <summary>Gets the registered struct type names.</summary>
    public ObservableCollection<string> StructTypeNames { get; } = [];

    /// <summary>Gets suggested types for struct fields.</summary>
    public ObservableCollection<string> StructFieldTypes { get; } = ["Integer", "Double", "Boolean", "String", "DateTime", "TimeSpan", "Color"];

    /// <summary>Gets suggested array element types.</summary>
    public ObservableCollection<string> ArrayElementTypes { get; } = ["Integer", "Double", "Boolean", "String", "DateTime", "TimeSpan", "Color"];

    /// <summary>Gets the fields in the current struct editor.</summary>
    public ObservableCollection<StructFieldViewModel> StructFields { get; } = [];

    /// <summary>Gets the registered struct definitions displayed by the editor.</summary>
    public ObservableCollection<StructTypeDefinition> StructTypes { get; } = [];

    /// <summary>Gets or sets whether the editor is expanded.</summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>Gets or sets the selected property row.</summary>
    [ObservableProperty]
    private NameValue? _selectedProperty;

    /// <summary>Gets or sets the property name being edited.</summary>
    [ObservableProperty]
    private string _propertyName = string.Empty;

    /// <summary>Gets or sets the selected property type.</summary>
    [ObservableProperty]
    private string _selectedType = "Integer";

    /// <summary>Gets or sets the text entered for the property value.</summary>
    [ObservableProperty]
    private string _valueInput = string.Empty;

    /// <summary>Gets or sets the Boolean value being edited.</summary>
    [ObservableProperty]
    private bool _booleanValue;

    /// <summary>Gets or sets the editor's error message.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Gets or sets the selected struct type for a property.</summary>
    [ObservableProperty]
    private string? _selectedStructType;

    /// <summary>Gets or sets the selected array element type.</summary>
    [ObservableProperty]
    private string _selectedArrayElementType = "Integer";

    /// <summary>Gets or sets the struct definition selected for editing.</summary>
    [ObservableProperty]
    private StructTypeDefinition? _selectedStructTypeDefinition;

    /// <summary>Gets or sets the struct type name being edited.</summary>
    [ObservableProperty]
    private string _structTypeName = string.Empty;

    /// <summary>Gets whether an existing property is selected for editing.</summary>
    public bool IsEditing => SelectedProperty is not null;

    /// <summary>Gets whether an existing struct type is selected for editing.</summary>
    public bool IsEditingStructType => SelectedStructTypeDefinition is not null;

    [RelayCommand]
    private void NewProperty()
    {
        SelectedProperty = null;
        PropertyName = string.Empty;
        SelectedType = "Integer";
        ValueInput = string.Empty;
        BooleanValue = false;
        SelectedArrayElementType = "Integer";
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void SaveProperty()
    {
        var name = PropertyName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Enter a property name.";
            return;
        }

        if (!TryCreateValue(out var value))
        {
            return;
        }

        var originalName = SelectedProperty?.Name;
        var nameExists = _variableHandler.GetVariableNames().Contains(name, StringComparer.Ordinal);
        if (nameExists && !string.Equals(name, originalName, StringComparison.Ordinal))
        {
            ErrorMessage = $"A property named '{name}' already exists.";
            return;
        }

        _variableHandler.Set(name, value);
        if (originalName is not null && !string.Equals(originalName, name, StringComparison.Ordinal))
        {
            _variableHandler.Remove(originalName);
        }

        NewProperty();
    }

    [RelayCommand]
    private void DeleteProperty()
    {
        if (SelectedProperty is null)
        {
            return;
        }

        _variableHandler.Remove(SelectedProperty.Name);
        NewProperty();
    }

    partial void OnSelectedStructTypeChanged(string? value)
    {
        if (SelectedType == "Struct" && SelectedProperty is null &&
            !string.IsNullOrWhiteSpace(value))
        {
            ValueInput = _structTypeRegistry.SerializeValue(_structTypeRegistry.CreateDefaultValue(value));
        }
    }

    partial void OnSelectedArrayElementTypeChanged(string value)
    {
        if (SelectedType == "Array" && SelectedProperty is null)
        {
            ValueInput = "[]";
        }
    }

    partial void OnSelectedTypeChanged(string value)
    {
        if (value == "Struct" && string.IsNullOrWhiteSpace(SelectedStructType))
        {
            SelectedStructType = StructTypeNames.FirstOrDefault();
        }
        else if (value != "Struct")
        {
            SelectedStructType = null;
        }

        if (value == "Array")
        {
            SelectedArrayElementType = ArrayElementTypes.FirstOrDefault() ?? "Integer";
            ValueInput = "[]";
        }
    }

    partial void OnSelectedStructTypeDefinitionChanged(StructTypeDefinition? value)
    {
        OnPropertyChanged(nameof(IsEditingStructType));
        if (value is null)
        {
            return;
        }

        StructTypeName = value.Name;
        StructFields.Clear();
        foreach (var field in value.Fields)
        {
            StructFields.Add(new StructFieldViewModel(field.Name, field.TypeName));
        }
    }

    partial void OnSelectedPropertyChanged(NameValue? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        ErrorMessage = string.Empty;

        if (value is null)
        {
            return;
        }

        PropertyName = value.Name;
        SelectedType = value.Type;
        if (value.Value is StructValue structure)
        {
            SelectedStructType = structure.TypeName;
            ValueInput = _structTypeRegistry.SerializeValue(structure);
        }
        else
        {
            ValueInput = value.Value is System.Drawing.Color color ? ColorValue.Format(color) :
                value.Value is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty
                    : value.Value?.ToString() ?? string.Empty;
            SelectedStructType = null;
        }

        if (value.Value is ArrayValue arrayValue)
        {
            SelectedArrayElementType = arrayValue.ElementTypeName;
            ValueInput = _structTypeRegistry.SerializeTypedValue($"{arrayValue.ElementTypeName}[]", arrayValue);
        }
        else
        {
            SelectedArrayElementType = "Integer";
        }

        BooleanValue = value.Value is bool booleanValue && booleanValue;
    }

    [RelayCommand]
    private void NewStructType()
    {
        SelectedStructTypeDefinition = null;
        StructFields.Clear();
        var names = StructTypes.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        var number = 1;
        var name = "Struct";
        while (names.Contains(name))
        {
            name = $"Struct{number++}";
        }

        StructTypeName = name;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void AddStructField()
    {
        var names = StructFields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        var number = StructFields.Count + 1;
        var name = $"Field{number}";
        while (names.Contains(name))
        {
            name = $"Field{++number}";
        }

        StructFields.Add(new StructFieldViewModel(name, "Integer"));
    }

    [RelayCommand]
    private void RemoveStructField(StructFieldViewModel? field)
    {
        if (field is not null)
        {
            StructFields.Remove(field);
        }
    }

    [RelayCommand]
    private void SaveStructType()
    {
        try
        {
            var definition = new StructTypeDefinition(
                StructTypeName.Trim(),
                StructFields.Select(field => new StructFieldDefinition(field.Name.Trim(), field.TypeName)).ToArray());
            _structTypeRegistry.SaveDefinition(definition, SelectedStructTypeDefinition?.Name);
            SelectedStructTypeDefinition = StructTypes.FirstOrDefault(type => type.Name == definition.Name);
            ErrorMessage = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }
    }

    [RelayCommand]
    private void DeleteStructType()
    {
        if (SelectedStructTypeDefinition is null)
        {
            return;
        }

        try
        {
            _structTypeRegistry.RemoveDefinition(SelectedStructTypeDefinition.Name);
            SelectedStructTypeDefinition = null;
            StructTypeName = string.Empty;
            StructFields.Clear();
            ErrorMessage = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void RefreshStructTypes()
    {
        var selectedName = SelectedStructTypeDefinition?.Name;
        StructTypes.Clear();
        foreach (var definition in _structTypeRegistry.GetDefinitions())
        {
            StructTypes.Add(definition);
        }

        StructTypeNames.Clear();
        StructFieldTypes.Clear();
        ArrayElementTypes.Clear();
        foreach (var primitiveType in new[] { "Integer", "Double", "Boolean", "String", "DateTime", "TimeSpan", "Color" })
        {
            StructFieldTypes.Add(primitiveType);
            ArrayElementTypes.Add(primitiveType);
            ArrayElementTypes.Add($"{primitiveType}[]");
        }

        foreach (var definition in StructTypes)
        {
            StructTypeNames.Add(definition.Name);
            StructFieldTypes.Add(definition.Name);
            StructFieldTypes.Add($"{definition.Name}[]");
            ArrayElementTypes.Add(definition.Name);
            ArrayElementTypes.Add($"{definition.Name}[]");
        }

        if (selectedName is not null)
        {
            SelectedStructTypeDefinition = StructTypes.FirstOrDefault(type => type.Name == selectedName);
        }
    }

    private void OnStructTypesChanged(object? sender, EventArgs eventArgs)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            RefreshStructTypes();
            return;
        }

        dispatcher.BeginInvoke(new Action(RefreshStructTypes));
    }

    private bool TryCreateValue(out object value)
    {
        switch (SelectedType)
        {
            case "Integer":
                if (int.TryParse(ValueInput, NumberStyles.Integer, CultureInfo.CurrentCulture, out var integer))
                {
                    value = integer;
                    return true;
                }

                ErrorMessage = "Enter a valid integer.";
                break;
            case "Double":
                if (double.TryParse(ValueInput, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var number)
                    && double.IsFinite(number))
                {
                    value = number;
                    return true;
                }

                ErrorMessage = "Enter a valid finite number.";
                break;
            case "Boolean":
                value = BooleanValue;
                return true;
            case "String":
                value = ValueInput;
                return true;
            case "DateTime":
                if (DateTime.TryParse(ValueInput, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out var dateTime))
                {
                    value = dateTime;
                    return true;
                }
                ErrorMessage = "Enter a valid date and time (for example, 2026-11-23T17:49:32).";
                break;
            case "TimeSpan":
                if (TimeSpan.TryParse(ValueInput, CultureInfo.CurrentCulture, out var timeSpan))
                {
                    value = timeSpan;
                    return true;
                }
                ErrorMessage = "Enter a valid time span (for example, 01:30:00).";
                break;
            case "Color":
                if (ColorValue.TryParse(ValueInput, out var color))
                {
                    value = color;
                    return true;
                }
                ErrorMessage = "Enter a color name or hex value (for example, Red or #80FF0000).";
                break;
            case "Struct" when !string.IsNullOrWhiteSpace(SelectedStructType):
                try
                {
                    value = _structTypeRegistry.ParseValue(SelectedStructType, ValueInput);
                    return true;
                }
                catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
                {
                    ErrorMessage = $"Enter a valid JSON value for '{SelectedStructType}': {exception.Message}";
                    break;
                }
            case "Array" when !string.IsNullOrWhiteSpace(SelectedArrayElementType):
                try
                {
                    value = _structTypeRegistry.ParseTypedValue($"{SelectedArrayElementType}[]", ValueInput);
                    return true;
                }
                catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
                {
                    ErrorMessage = $"Enter a valid JSON array for '{SelectedArrayElementType}[]': {exception.Message}";
                    break;
                }
            default:
                ErrorMessage = "Select a supported property type.";
                break;
        }

        value = string.Empty;
        return false;
    }

    private void LoadProperties()
    {
        foreach (var name in _variableHandler.GetVariableNames())
        {
            if (_variableHandler.TryGet<int>(name, out var integer))
            {
                Properties.Add(new NameValue { Name = name, Type = "Integer", Value = integer });
            }
            else if (_variableHandler.TryGet<double>(name, out var number))
            {
                Properties.Add(new NameValue { Name = name, Type = "Double", Value = number });
            }
            else if (_variableHandler.TryGet<bool>(name, out var boolean))
            {
                Properties.Add(new NameValue { Name = name, Type = "Boolean", Value = boolean });
            }
            else if (_variableHandler.TryGet<string>(name, out var text))
            {
                Properties.Add(new NameValue { Name = name, Type = "String", Value = text });
            }
            else if (_variableHandler.TryGet<DateTime>(name, out var dateTime))
            {
                Properties.Add(new NameValue { Name = name, Type = "DateTime", Value = dateTime });
            }
            else if (_variableHandler.TryGet<TimeSpan>(name, out var timeSpan))
            {
                Properties.Add(new NameValue { Name = name, Type = "TimeSpan", Value = timeSpan });
            }
            else if (_variableHandler.TryGet<System.Drawing.Color>(name, out var color))
            {
                Properties.Add(new NameValue { Name = name, Type = "Color", Value = color });
            }
            else if (_variableHandler.TryGet<StructValue>(name, out var structure) && structure is not null)
            {
                Properties.Add(new NameValue { Name = name, Type = "Struct", Value = structure });
            }
            else if (_variableHandler.TryGet<ArrayValue>(name, out var array) && array is not null)
            {
                Properties.Add(new NameValue { Name = name, Type = "Array", Value = array });
            }
        }
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyVariableChange(change);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => ApplyVariableChange(change)));
    }

    private void ApplyVariableChange(VariableChanged change)
    {
        var property = Properties.FirstOrDefault(item => string.Equals(item.Name, change.Name, StringComparison.Ordinal));
        if (change.NewValue is null || !TryGetType(change.NewValue, out var type))
        {
            if (property is not null)
            {
                Properties.Remove(property);
                if (ReferenceEquals(SelectedProperty, property))
                {
                    NewProperty();
                }
            }

            return;
        }

        if (property is null)
        {
            Properties.Add(new NameValue { Name = change.Name, Type = type, Value = change.NewValue });
            return;
        }

        property.Type = type;
        property.Value = change.NewValue;
    }

    private static bool TryGetType(object value, out string type)
    {
        type = value switch
        {
            int => "Integer",
            double => "Double",
            bool => "Boolean",
            string => "String",
            DateTime => "DateTime",
            TimeSpan => "TimeSpan",
            System.Drawing.Color => "Color",
            StructValue => "Struct",
            ArrayValue => "Array",
            _ => string.Empty
        };

        return type.Length > 0;
    }

    /// <inheritdoc />
    public override string Kind => PropertyManagerCreator.jsonkey;

    /// <inheritdoc />
    public override int MaxChildren => 0;

    /// <inheritdoc />
    public override string ChildRule => "No Children here";

    /// <inheritdoc />
    public override bool CanAcceptChildren => false;

    /// <summary>Serializes the editor's expansion state.</summary>
    /// <returns>The document item settings.</returns>
    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new PropertyManagerDocumentSettings(IsExpanded));

    /// <summary>Restores the editor's expansion state.</summary>
    /// <param name="data">The serialized document item settings.</param>
    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<PropertyManagerDocumentSettings>()
            ?? throw new InvalidDataException("Property manager data is invalid.");
        IsExpanded = settings.IsExpanded;
    }

    /// <inheritdoc />
    public override TreeItemNode Copy()
    {
        var copy = new PropertyManagerViewModel(_variableHandler, _structTypeRegistry)
        {
            Name = Name,
            Description = Description,
            IsExpanded = IsExpanded
        };

        return copy;
    }

    private sealed record PropertyManagerDocumentSettings(bool IsExpanded = true);
}
