using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunicationManager.UI;

/// <summary>Represents one editable mapping between a document property and a serial protocol name.</summary>
public partial class CommunicationMappingViewModel : ObservableObject
{
    /// <summary>Creates a mapping with the specified property and external names.</summary>
    /// <param name="propertyName">The document property name.</param>
    /// <param name="externalName">The serial protocol name.</param>
    public CommunicationMappingViewModel(string propertyName, string externalName)
    {
        PropertyName = propertyName;
        ExternalName = externalName;
    }

    /// <summary>Gets or sets the mapped document property name.</summary>
    [ObservableProperty]
    private string _propertyName = string.Empty;

    /// <summary>Gets or sets the name used for this property in serial messages.</summary>
    [ObservableProperty]
    private string _externalName = string.Empty;

    /// <summary>Gets or sets whether changes to the property are sent to the device.</summary>
    [ObservableProperty]
    private bool _sendUpdates;
}
