using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;


namespace FlexibleHMI.Interfaces;

public enum ViewKind
{
    MainView,
    DetailsView
}

public sealed record ViewTemplateRegistration(Type ViewModelType, ViewKind View, DataTemplate Template);


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
    public static IServiceCollection AddMainAndDetail<TViewModel>(this IServiceCollection services, string main, string details) where TViewModel : ObservableObject
    {
        services.AddViewTemplate<TViewModel>(ViewKind.MainView, main);
        services.AddViewTemplate<TViewModel>(ViewKind.DetailsView, details);
        return services;
    }

    public static IServiceCollection AddViewTemplate<TViewModel>(
    this IServiceCollection services, ViewKind view, string resourceKey)
    where TViewModel : ObservableObject
    {
        var key = (typeof(TViewModel), view);
        services.AddKeyedSingleton<DataTemplate>(key, (provider, _) =>
        {
            var resources = provider.GetRequiredService<ResourceDictionary>();
            return resources[resourceKey] as DataTemplate
                ?? throw new InvalidOperationException($"Data template '{resourceKey}' is missing or invalid.");
        });
        services.AddSingleton(provider => new ViewTemplateRegistration(
            typeof(TViewModel), view, provider.GetRequiredKeyedService<DataTemplate>(key)));
        return services;
    }

    //public static IServiceCollection AddViewModelChoice<TViewModel>(
    //    this IServiceCollection services, string label)
    //    where TViewModel : ObservableObject
    //{
    //    //services.AddSingleton<TViewModel>();
    //    //services.AddSingleton(provider => new ViewModelChoice(label, provider.GetRequiredService<TViewModel>()));
    //    services.AddTransient<TViewModel>();
    //    services.AddSingleton(provider => new ViewModelChoice(label, provider.GetRequiredService<TViewModel>()));
    //    return services;
    //}

}

