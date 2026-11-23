using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionGenerator.UI;

/// <summary>Registers the function-generator item and its WPF view with the host application.</summary>
public class AddInRegistrator : IAddInRegistrator
{
    /// <inheritdoc/>
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddKeyedTransient<TreeItemOption, FunctionGeneratorCreator>(FunctionGeneratorCreator.jsonkey);
        services.AddKeyedTransient<TreeItemNode, FunctionGeneratorViewModel>(FunctionGeneratorCreator.jsonkey);
    }

    /// <inheritdoc/>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<FunctionGeneratorViewModel, FunctionGeneratorView>();
    }
}
