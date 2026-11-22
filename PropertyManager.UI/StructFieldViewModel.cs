using CommunityToolkit.Mvvm.ComponentModel;

namespace PropertyManager.UI;

public partial class StructFieldViewModel(string name, string typeName) : ObservableObject
{
    [ObservableProperty]
    private string _name = name;

    [ObservableProperty]
    private string _typeName = typeName;
}
