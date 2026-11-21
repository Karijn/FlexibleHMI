using CommunityToolkit.Mvvm.ComponentModel;

namespace PropertyManager.UI;

public partial class NameValue : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private object? _value;
}
