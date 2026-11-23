using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Interfaces;
using System.Windows.Media;

namespace Inputs.UI;

/// <summary>Registers the property-bound input controls and their document templates.</summary>
public sealed class AddInRegistrator : IAddInRegistrator
{
    /// <summary>Registers keyed tree-item options and nodes for each supported input kind.</summary>
    /// <param name="services">The service collection to configure.</param>
    public void RegisterAddIn(IServiceCollection services)
    {
        foreach (var kind in Enum.GetValues<InputKind>())
        {
            var key = InputViewModel.Key(kind);
            services.AddKeyedTransient<TreeItemOption>(key, (provider, _) =>
                new TreeItemOption(key, InputViewModel.Title(kind), "Inputs", "Property-bound input control", TreeItemOption.CreateDiamondImage(Colors.SteelBlue), provider));
            services.AddKeyedTransient<TreeItemNode>(key, (provider, _) =>
                new InputViewModel(provider.GetRequiredService<IVariableHandler>(), kind));
        }
    }

    /// <summary>Registers the input editor view for input tree items.</summary>
    /// <param name="serviceProvider">The built service provider supplied by the add-in host.</param>
    /// <param name="dataTemplateManager">The manager that receives the view template registration.</param>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager) =>
        dataTemplateManager.RegisterDataTemplate<InputViewModel, InputView>();
}
