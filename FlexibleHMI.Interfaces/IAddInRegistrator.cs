using Microsoft.Extensions.DependencyInjection;


namespace FlexibleHMI.Interfaces;

/// <summary>Registers and initializes an application add-in.</summary>
public interface IAddInRegistrator
{
    /// <summary>Registers the add-in's services before the service provider is built.</summary>
    /// <param name="services">The application service collection.</param>
    void RegisterAddIn(IServiceCollection services);

    /// <summary>Initializes the add-in after services are available.</summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="dataTemplateManager">The manager used to register view templates.</param>
    void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager);
}

/// <summary>Contains add-in registration extensions.</summary>
public static class AddInRegistrationExtensions
{
    //public static IServiceCollection RegistterAddInCreatorAndViewModel(this IServiceCollection services, TreeItemOption option, TreeItemMode node)
    //{

    //}
}