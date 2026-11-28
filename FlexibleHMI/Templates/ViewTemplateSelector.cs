using FlexibleHMI.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace FlexibleHMI.Templates;




public class ViewTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate _defaultTemplate;
    private readonly Dictionary<Type, DataTemplate> _templates;

    public ViewTemplateSelector(ViewKind view, IEnumerable<ViewTemplateRegistration> registrations, DataTemplate defaultTemplate)
    {
        ArgumentNullException.ThrowIfNull(defaultTemplate);
        _defaultTemplate = defaultTemplate;
        _templates = new Dictionary<Type, DataTemplate>();
        foreach (var registration in registrations.Where(registration => registration.View == view))
        {
            if (!_templates.TryAdd(registration.ViewModelType, registration.Template))
            {
                throw new InvalidOperationException($"A template for '{registration.ViewModelType.FullName}' and view '{view}' is already registered.");
            }
        }
    }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is null)
        {
            return base.SelectTemplate(item, container);
        }

        return _templates.TryGetValue(item.GetType(), out var template)
            ? template
            : _defaultTemplate;
    }
}
