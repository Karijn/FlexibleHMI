using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionGenerator.UI;

public class AddInRegistrator : IAddInRegistrator
{
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddKeyedTransient<TreeItemOption, FunctionGeneratorCreator>(FunctionGeneratorCreator.jsonkey);
        services.AddKeyedTransient<TreeItemNode, FunctionGeneratorViewModel>(FunctionGeneratorCreator.jsonkey);
    }

    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<FunctionGeneratorViewModel, FunctionGeneratorView>();
    }
}
