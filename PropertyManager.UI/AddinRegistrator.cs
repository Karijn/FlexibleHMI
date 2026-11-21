using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Interfaces;


namespace PropertyManager.UI;

/// <summary>
/// Represents the add-in registrator for external add-ins.
/// </summary>
public class AddinRegistrator : IAddInRegistrator
{
    //internal static IAbstractFactory<SerialPortsView>? SerialPortsViewFactory { get; private set; }

    /// <summary>
    /// Registers the add-in services with the provided service collection.
    /// </summary>
    /// <param name="services"></param>
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddSingleton<IVariableHandler, VariableHandler>();
        services.AddKeyedTransient<TreeItemOption, PropertyManagerCreator>(PropertyManagerCreator.defaultkind);
        services.AddKeyedTransient<TreeItemNode, PropertyManagerViewModel>(PropertyManagerCreator.defaultkind);
    }

    /// <summary>
    /// Initializes the add-in with the provided service provider and data template manager.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="dataTemplateManager"></param>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<PropertyManagerViewModel, PropertyManagerView>();
    }
}
