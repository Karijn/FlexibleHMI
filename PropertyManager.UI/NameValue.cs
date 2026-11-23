using CommunityToolkit.Mvvm.ComponentModel;
using PropertyManager.Interfaces;

namespace PropertyManager.UI;

public partial class NameValue : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private object? _value;

    public object? DisplayValue => Value is System.Drawing.Color color ? ColorValue.Format(color) : Value;
}
