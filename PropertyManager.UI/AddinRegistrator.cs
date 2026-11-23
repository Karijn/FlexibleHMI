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
    /// <param name="services">The collection receiving the Property Manager services.</param>
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddSingleton<IVariableHandler, VariableHandler>();
        services.AddSingleton<IStructTypeRegistry, StructTypeRegistry>();
        services.AddSingleton<ITreeDocumentProperties, VariableHandlerDocumentProperties>();
        services.AddKeyedTransient<TreeItemOption, PropertyManagerCreator>(PropertyManagerCreator.jsonkey);
        services.AddKeyedTransient<TreeItemNode, PropertyManagerViewModel>(PropertyManagerCreator.jsonkey);
    }

    /// <summary>
    /// Initializes the add-in with the provided service provider and data template manager.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="dataTemplateManager">The manager receiving the view template registration.</param>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<PropertyManagerViewModel, PropertyManagerView>();
    }
}
