using CommunityToolkit.Mvvm.ComponentModel;

namespace PropertyManager.UI;

/// <summary>Represents an editable struct field definition.</summary>
/// <param name="name">The initial field name.</param>
/// <param name="typeName">The initial field type name.</param>
public partial class StructFieldViewModel(string name, string typeName) : ObservableObject
{
    /// <summary>Gets or sets the field name.</summary>
    [ObservableProperty]
    private string _name = name;

    /// <summary>Gets or sets the field type name.</summary>
    [ObservableProperty]
    private string _typeName = typeName;
}
