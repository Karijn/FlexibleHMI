using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Interfaces;
using System.Windows.Media;

namespace Inputs.UI;

public sealed class AddInRegistrator : IAddInRegistrator
{
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

    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager) =>
        dataTemplateManager.RegisterDataTemplate<InputViewModel, InputView>();
}
