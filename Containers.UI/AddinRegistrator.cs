using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Containers.UI;

/// <summary>Represents the add-in registrator for the container controls.</summary>
public class AddinRegistrator : IAddInRegistrator
{
    /// <inheritdoc />
    public void RegisterAddIn(IServiceCollection services)
    {
        services.AddKeyedTransient<TreeItemOption, GridContainerCreator>(GridContainerCreator.KindName);
        services.AddKeyedTransient<TreeItemNode, GridContainerViewModel>(GridContainerCreator.KindName);
        services.AddMainAndDetail<GridContainerViewModel>("GridContainerDetails", "DefaultDetailsTemplate");
    }

    /// <inheritdoc />
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<GridContainerViewModel, GridContainerView>();
        dataTemplateManager.RegisterKeyedTemplate<GridContainerView>("GridContainerMain");
        dataTemplateManager.RegisterKeyedTemplate<GridContainerDetails>("GridContainerDetails");
    }
}
