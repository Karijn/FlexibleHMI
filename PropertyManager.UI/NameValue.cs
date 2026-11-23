using CommunityToolkit.Mvvm.ComponentModel;
using PropertyManager.Interfaces;

namespace PropertyManager.UI;

/// <summary>Represents a property row displayed by the Property Manager.</summary>
public partial class NameValue : ObservableObject
{
    /// <summary>Gets or sets the property name.</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Gets or sets the displayed property type.</summary>
    [ObservableProperty]
    private string _type = string.Empty;

    /// <summary>Gets or sets the property value.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private object? _value;

    /// <summary>Gets the display value, formatting colors as ARGB text.</summary>
    public object? DisplayValue => Value is System.Drawing.Color color ? ColorValue.Format(color) : Value;
}
