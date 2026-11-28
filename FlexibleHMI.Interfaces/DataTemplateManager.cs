using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace FlexibleHMI.Interfaces;


public static class DataTemplateManagerExtensions
{
    /// <summary>Registers a data template manager in the service collection.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="resources">The resource dictionary to register templates in.</param>
    public static void AddDataTemplateManager(this IServiceCollection services, ResourceDictionary resources)
    {
        services.AddSingleton<IDataTemplateManager>(new DataTemplateManager(resources));
    }

    //private static void Register<T>(IServiceCollection services, string key, string title, Func<IVariableHandler, T> create) where T : InputViewModelBase
    //{
    //    services.AddKeyedTransient<TreeItemOption>(key, (provider, _) =>
    //        new TreeItemOption(key, title, "Inputs", "Property-bound input control", TreeItemOption.CreateDiamondImage(Colors.SteelBlue), provider));
    //    services.AddKeyedTransient<TreeItemNode>(key, (provider, _) => create(provider.GetRequiredService<IVariableHandler>()));
    //}

}

/// <summary>Registers WPF data templates in the current application's resources.</summary>
public class DataTemplateManager : IDataTemplateManager
{
    private readonly ResourceDictionary _resources;

    public DataTemplateManager(ResourceDictionary resources)
    {
        _resources = resources;
    }


    /// <inheritdoc />
    public void RegisterDataTemplate<TViewModel, TView>() where TView : FrameworkElement
    {
        RegisterDataTemplate(typeof(TViewModel), typeof(TView));
    }

    /// <inheritdoc />
    public void RegisterDataTemplate(Type viewModelType, Type viewType)
    {
        var template = CreateTemplate(viewModelType, viewType);
        var key = template.DataTemplateKey;
        Application.Current.Resources.Add(key, template);
    }

    /// <summary>
    /// Creates a data template for the specified view model and view types.
    /// </summary>
    /// <param name="viewModelType"></param>
    /// <param name="viewType"></param>
    /// <returns></returns>
    private DataTemplate CreateTemplate(Type viewModelType, Type viewType)
    {
        const string xamlTemplate = "<DataTemplate DataType=\"{{x:Type vm:{0}}}\"><v:{1} /></DataTemplate>";
        var xaml = String.Format(xamlTemplate, viewModelType.Name, viewType.Name, viewModelType.Namespace, viewType.Namespace);

        var context = new ParserContext();

        context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
        context.XamlTypeMapper.AddMappingProcessingInstruction("vm", viewModelType.Namespace, viewModelType.Assembly.FullName);
        context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);

        context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
        context.XmlnsDictionary.Add("vm", "vm");
        context.XmlnsDictionary.Add("v", "v");

        var template = (DataTemplate)XamlReader.Parse(xaml, context);
        return template;
    }

    //======================================================================
    public void RegisterKeyedTemplate<TView>(string key) where TView : FrameworkElement
    {
        RegisterKeyedTemplate(key, typeof(TView));
    }

    /// <inheritdoc />
    public void RegisterKeyedTemplate(string key, Type viewType)
    {
        var template = CreateKeyedTemplate(key, viewType);
        //var resourcekey = template.DataTemplateKey;
        _resources.Add(key, template);
    }

    private DataTemplate CreateKeyedTemplate(string key, Type viewType)
    {
        const string xamlTemplate = "<DataTemplate x:Key=\"{0}\"><v:{1}/></DataTemplate>";
        var xaml = String.Format(xamlTemplate, key, viewType.Name);

        var context = new ParserContext();
        context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
        context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);
        context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
        context.XmlnsDictionary.Add("v", "v");
        var template = (DataTemplate)XamlReader.Parse(xaml, context);
        return template;
    }

}
