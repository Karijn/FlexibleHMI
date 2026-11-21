using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace PropertyManager.UI;

public partial class PropertyManagerViewModel : TreeItemNode
{
    private readonly IVariableHandler _variableHandler;

    public PropertyManagerViewModel(IVariableHandler variableHandler)
    {
        ArgumentNullException.ThrowIfNull(variableHandler);

        Name = PropertyManagerCreator.defaultname;
        Description = PropertyManagerCreator.defaultdescription;
        Image = PropertyManagerCreator.defaultimage();
        _variableHandler = variableHandler;
        _variableHandler.VariableChanged += OnVariableChanged;
        LoadProperties();
    }

    public ObservableCollection<NameValue> Properties { get; } = [];

    public ObservableCollection<string> PropertyTypes { get; } = ["Integer", "Double", "Boolean"];

    [ObservableProperty]
    private NameValue? _selectedProperty;

    [ObservableProperty]
    private string _propertyName = string.Empty;

    [ObservableProperty]
    private string _selectedType = "Integer";

    [ObservableProperty]
    private string _valueInput = string.Empty;

    [ObservableProperty]
    private bool _booleanValue;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool IsEditing => SelectedProperty is not null;

    [RelayCommand]
    private void NewProperty()
    {
        SelectedProperty = null;
        PropertyName = string.Empty;
        SelectedType = "Integer";
        ValueInput = string.Empty;
        BooleanValue = false;
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
        ValueInput = value.Value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty
            : value.Value?.ToString() ?? string.Empty;
        BooleanValue = value.Value is bool booleanValue && booleanValue;
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
            _ => string.Empty
        };

        return type.Length > 0;
    }

    public override string Kind => PropertyManagerCreator.defaultkind;

    public override int MaxChildren => 0;

    public override string ChildRule => "No Children here";

    public override bool CanAcceptChildren => false;

    public override TreeItemNode Copy()
    {
        var copy = new PropertyManagerViewModel(_variableHandler)
        {
            Name = Name,
            Description = Description
        };

        return copy;
    }
}
