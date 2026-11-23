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
}
