using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunicationManager.UI;

public partial class CommunicationMappingViewModel : ObservableObject
{
    public CommunicationMappingViewModel(string propertyName, string externalName)
    {
        PropertyName = propertyName;
        ExternalName = externalName;
    }

    [ObservableProperty]
    private string _propertyName = string.Empty;

    [ObservableProperty]
    private string _externalName = string.Empty;

    [ObservableProperty]
    private bool _sendUpdates;
}
