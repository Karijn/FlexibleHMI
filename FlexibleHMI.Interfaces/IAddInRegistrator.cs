using Microsoft.Extensions.DependencyInjection;


namespace FlexibleHMI.Interfaces;

public interface IAddInRegistrator
{
    void RegisterAddIn(IServiceCollection services);

    void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager);
}

public static class AddInRegistrationExtensions
{
    //public static IServiceCollection RegistterAddInCreatorAndViewModel(this IServiceCollection services, TreeItemOption option, TreeItemMode node)
    //{

    //}
}