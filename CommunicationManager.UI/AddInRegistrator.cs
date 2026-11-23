using CommunicationManager.Interfaces;
using CommunicationManagerRuntime = global::CommunicationManager.CommunicationManager;
using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CommunicationManager.UI;

/// <summary>Registers Communication Manager services and its document-tree UI integration.</summary>
public sealed class AddInRegistrator : IAddInRegistrator
{
    /// <summary>Registers the serial runtime services, history store, and keyed tree-item factories.</summary>
    /// <param name="services">The service collection to configure.</param>
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddSingleton<DirectMessageHistoryStore>();
        services.AddTransient<ISerialPortDevice, SerialPortDevice>();
        services.AddTransient<CommunicationManagerRuntime>();
        services.AddTransient<Func<CommunicationManagerRuntime>>(
            provider => () => provider.GetRequiredService<CommunicationManagerRuntime>());
        services.AddKeyedTransient<TreeItemOption, CommunicationManagerCreator>(CommunicationManagerCreator.DefaultKind);
        services.AddKeyedTransient<TreeItemNode, CommunicationManagerViewModel>(CommunicationManagerCreator.DefaultKind);
    }

    /// <summary>Registers the view template used to display a Communication Manager tree item.</summary>
    /// <param name="serviceProvider">The built service provider supplied by the add-in host.</param>
    /// <param name="dataTemplateManager">The manager that receives the view template registration.</param>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<CommunicationManagerViewModel, CommunicationManagerView>();
    }
}
