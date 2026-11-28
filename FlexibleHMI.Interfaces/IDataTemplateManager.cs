using System.Windows;


namespace FlexibleHMI.Interfaces;

/// <summary>Registers WPF views for view-model types.</summary>
public interface IDataTemplateManager
{
    /// <summary>Registers a view for a view-model type.</summary>
    /// <param name="viewModelType">The view-model type.</param>
    /// <param name="viewType">The view type.</param>
    void RegisterDataTemplate(Type viewModelType, Type viewType);

    /// <summary>Registers a view for a view-model type.</summary>
    /// <typeparam name="TViewModel">The view-model type.</typeparam>
    /// <typeparam name="TView">The WPF view type.</typeparam>
    void RegisterDataTemplate<TViewModel, TView>() where TView : FrameworkElement;

    /// <summary>
    /// Registers a template for the specified key and view type.
    /// </summary>
    /// <typeparam name="TView">The type of view associated with the template.</typeparam>
    /// <param name="key">The key used to identify the template.</param>
    void RegisterKeyedTemplate<TView>(string key) where TView : FrameworkElement;

    /// <summary>
    /// Registers a view type as a template associated with the specified key.
    /// </summary>
    /// <param name="key">The key used to identify the template.</param>
    /// <param name="viewType">The view type to register for the key.</param>
    void RegisterKeyedTemplate(string key, Type viewType);
}
