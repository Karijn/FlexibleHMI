using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace VUMeter.UI;

/// <summary>
/// Represents the add-in registrator for external add-ins.
/// </summary>
public class AddinRegistrator : IAddInRegistrator
{
    //internal static IAbstractFactory<SerialPortsView>? SerialPortsViewFactory { get; private set; }

    /// <summary>
    /// Registers the add-in services with the provided service collection.
    /// </summary>
    /// <param name="services">The dependency-injection service collection used to register the add-in.</param>
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddKeyedTransient<TreeItemOption, VUMeterCreator>(VUMeterCreator.jsonkey);
        services.AddKeyedTransient<TreeItemNode, VUMeterViewModel>(VUMeterCreator.jsonkey);
        services.AddKeyedTransient<TreeItemOption, XYControlCreator>(XYControlCreator.KindName);
        services.AddKeyedTransient<TreeItemNode, XYControlViewModel>(XYControlCreator.KindName);
        services.AddKeyedTransient<TreeItemOption, OscilloscopeCreator>(OscilloscopeCreator.KindName);
        services.AddKeyedTransient<TreeItemNode, OscilloscopeViewModel>(OscilloscopeCreator.KindName);
    }

    /// <summary>
    /// Initializes the add-in with the provided service provider and data template manager.
    /// </summary>
    /// <param name="serviceProvider">The provider containing the registered add-in services.</param>
    /// <param name="dataTemplateManager">The host manager used to associate view models with their views.</param>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<VUMeterViewModel, VUMeterView>();
        dataTemplateManager.RegisterDataTemplate<XYControlViewModel, XYControlView>();
        dataTemplateManager.RegisterDataTemplate<OscilloscopeViewModel, OscilloscopeView>();
    }
}
