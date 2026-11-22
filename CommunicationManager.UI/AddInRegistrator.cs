using CommunicationManager.Interfaces;
using CommunicationManagerRuntime = global::CommunicationManager.CommunicationManager;
using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CommunicationManager.UI;

public sealed class AddInRegistrator : IAddInRegistrator
{
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

    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<CommunicationManagerViewModel, CommunicationManagerView>();
    }
}
