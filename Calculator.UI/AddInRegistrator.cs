using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Calculator.UI;

/// <summary>Registers the calculator tree item and its WPF view with the host application.</summary>
public class AddInRegistrator : IAddInRegistrator
{
    /// <inheritdoc/>
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddKeyedTransient<TreeItemOption, CalculatorCreator>(CalculatorCreator.jsonkey);
        services.AddKeyedTransient<TreeItemNode, CalculatorViewModel>(CalculatorCreator.jsonkey);

        //services.AddTransient<FunctionGeneratorView>();
    }

    /// <inheritdoc/>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<CalculatorViewModel, CalculatorView>();
    }
}
