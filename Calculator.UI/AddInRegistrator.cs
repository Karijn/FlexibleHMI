using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Calculator.UI;

public class AddInRegistrator : IAddInRegistrator
{
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddKeyedTransient<TreeItemOption, CalculatorCreator>(CalculatorCreator.jsonkey);
        services.AddKeyedTransient<TreeItemNode, CalculatorViewModel>(CalculatorCreator.jsonkey);

        //services.AddTransient<FunctionGeneratorView>();
    }

    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<CalculatorViewModel, CalculatorView>();
    }
}
