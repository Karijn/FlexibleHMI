using System.Windows;


namespace FlexibleHMI.Interfaces;

public interface IDataTemplateManager
{
    void RegisterDataTemplate(Type viewModelType, Type viewType);
    void RegisterDataTemplate<TViewModel, TView>() where TView : FrameworkElement;
}
