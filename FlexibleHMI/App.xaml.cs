using FlexibleHMI.Controls;
using FlexibleHMI.Interfaces;
using FlexibleHMI.Services;
using FlexibleHMI.ViewModels;
using FlexibleHMI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Media;

namespace FlexibleHMI;

public partial class App : Application
{
    public static ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AllowResolvingKeyedServicesAsDictionary();

        ConfigureServices(services);

        var addinsDirectory = Path.Combine(AppContext.BaseDirectory, "Addins");
        var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
        AddAssemblies(AppContext.BaseDirectory, assemblies);
        AddAssemblies(addinsDirectory, assemblies);

        //services.AddTransient<VUMeterControl>();
        //services.AddTransient<VuMeterNode>();


        var addIns = RegisterAddinServices(services, assemblies);



        _serviceProvider = services.BuildServiceProvider();

        var dataTemplateManager = _serviceProvider.GetRequiredService<IDataTemplateManager>();
        InitializeAddIns(addIns, _serviceProvider, dataTemplateManager);

        //dataTemplateManager.RegisterDataTemplate<VuMeterNode, VUMeterControl>();

        var creators = _serviceProvider.GetRequiredService<IDictionary<string, TreeItemOption>>();

        var settings = _serviceProvider.GetRequiredService<IUserSettingsService>();
        _serviceProvider.GetRequiredService<IThemeService>().ApplyTheme(settings.Theme);
        MainWindowViewModel viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        viewModel.IsMainContentFullScreen = settings.IsMainContentFullScreen;

        viewModel.IsLeftExpanded = settings.IsLeftPanelExpanded;
        viewModel.LeftPanelWidth = settings.LeftPanelWidth;

        viewModel.IsRightExpanded = settings.IsRightPanelExpanded;
        viewModel.RightPanelWidth = settings.RightPanelWidth;

        _serviceProvider.GetRequiredService<MainWindow>().Show();
        viewModel.LoadLastDocumentAtStartup();
    }

    /// <summary>
    /// Configures the services for dependency injection, including the main window, view models, and data template manager.
    /// </summary>
    /// <param name="services"></param>
    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfirmationService, MessageBoxConfirmationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ITreeDocumentProperties, EmptyTreeDocumentProperties>();
        services.AddSingleton<ITreeDocumentService, TreeDocumentService>();
        services.AddSingleton<IUserSettingsService, UserSettingsService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        services.AddSingleton<IDataTemplateManager, DataTemplateManager>();

        //services.AddTransient<LeafItemNode>();
        //services.AddKeyedTransient<TreeItemOption, LeafCreator>("leaf");
        //services.AddKeyedTransient<TreeItemNode, LeafItemNode>("leaf");

        //services.AddKeyedTransient<TreeItemOption, SingleChildCreator>("singlechild");
        //services.AddKeyedTransient<TreeItemNode, SingleChildItemNode>("singlechild");

        //services.AddKeyedTransient<TreeItemOption, CollectionCreator>("collection");
        //services.AddKeyedTransient<TreeItemNode, CollectionItemNode>("collection");

        //services.AddKeyedTransient<TreeItemOption, GridCreator>("grid");
        //services.AddKeyedTransient<TreeItemNode, GridItemNode>("grid");

        //services.AddKeyedTransient<TreeItemOption, VuMeterCreator>("vu meter");
        //services.AddKeyedTransient<TreeItemNode, VuMeterNode>("vu meter");

        //services.AddTransient<Func<TreeItemNode>>(provider =>
        //    () => provider.GetRequiredKeyedService<TreeItemNode>("leaf"));

        return services;
    }

    /// <summary>
    /// Initializes add-ins by calling their InitializeAddIn method and registering data templates for specific view models and controls.
    /// </summary>
    /// <param name="addIns"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="dataTemplateManager"></param>
    private void InitializeAddIns(IReadOnlyList<IAddInRegistrator> addIns, ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        foreach (var addin in addIns)
        {
            addin.InitializeAddIn(serviceProvider, dataTemplateManager);
        }
    }

    /// <summary>
    /// Adds assemblies from the specified directory to the provided list of assemblies.
    /// </summary>
    /// <param name="addinsDirectory"></param>
    /// <param name="assemblies"></param>
    private static void AddAssemblies(string addinsDirectory, List<Assembly> assemblies)
    {
        if (Directory.Exists(addinsDirectory))
        {
            foreach (var addinAssemblyPath in Directory.EnumerateFiles(addinsDirectory, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(addinAssemblyPath));
                    assemblies.Add(assembly);
                }
                catch
                {
                    // eat exceptions for now, maybe log them later
                }
            }
        }
    }

    /// <summary>
    /// Registers add-in services by discovering and initializing IAddInRegistrator implementations from the provided assemblies.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IReadOnlyList<IAddInRegistrator> RegisterAddinServices(IServiceCollection services, IReadOnlyList<Assembly> assemblies)
    {
        //var discoveredAddins = new List<IAddInRegistrator>();
        //var addinsDirectory = Path.Combine(AppContext.BaseDirectory, "Addins");

        List<IAddInRegistrator> addinInitialisators = new List<IAddInRegistrator>();
        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var addinType in GetAddinTypes(assembly))
            {
                if (Activator.CreateInstance(addinType) is IAddInRegistrator addin)
                {
                    addinInitialisators.Add(addin);
                    addin.RegisterAddIn(services);
                }
            }
        }

        return addinInitialisators;
    }

    /// <summary>
    /// Gets the types that implement IAddInRegistrator from the specified assembly, filtering out abstract classes and interfaces, and ensuring they have a parameterless constructor.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    private static IEnumerable<Type> GetAddinTypes(Assembly assembly)
    {
        try
        {
            return assembly
                .GetTypes()
                .Where(type => typeof(IAddInRegistrator).IsAssignableFrom(type)
                    && type is { IsAbstract: false, IsInterface: false }
                    && type.GetConstructor(Type.EmptyTypes) is not null);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)
                .Cast<Type>()
                .Where(type => typeof(IAddInRegistrator).IsAssignableFrom(type)
                    && type is { IsAbstract: false, IsInterface: false }
                    && type.GetConstructor(Type.EmptyTypes) is not null);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is not null)
        {
            try
            {
                var viewModel = _serviceProvider.GetService<MainWindowViewModel>();
                if (viewModel is not null && !viewModel.StopDocumentRuntimeActivity())
                {
                    System.Diagnostics.Trace.TraceError(viewModel.StatusMessage);
                }
            }
            finally
            {
                _serviceProvider.Dispose();
            }
        }

        base.OnExit(e);
    }
}


public class LeafCreator : TreeItemOption
{
    //private readonly IServiceProvider _serviceProvider;

    //private ServiceProvider _sp;

    public LeafCreator(IServiceProvider serviceProvider) :
        base("leaf", "Leaf item", "Test Nodes", "Cannot contain children", TreeItemOption.CreateDiamondImage(Colors.Red), serviceProvider)
    {
        //this._serviceProvider = serviceProvider;
    }
}

public class SingleChildCreator : TreeItemOption
{
    public SingleChildCreator(IServiceProvider serviceProvider) 
        : base("singlechild", "Single-child item", "Test Nodes", "Can contain at most one child", TreeItemOption.CreateDiamondImage(Colors.Blue), 
            serviceProvider)
    {
    }
}

public class CollectionCreator : TreeItemOption
{
    public CollectionCreator(IServiceProvider serviceProvider) 
        : base("collection", "Collection item", "Test Nodes", "Can contain any number of children", TreeItemOption.CreateDiamondImage(Colors.Silver),
             serviceProvider)
    {
    }
}

public class GridCreator : TreeItemOption
{
    public GridCreator(IServiceProvider serviceProvider) 
        : base("grid", "Grid item", "Test Nodes", "Can contain any number of children", TreeItemOption.CreateDiamondImage(Colors.Gold), 
            serviceProvider)
    { }
}

//public class VuMeterCreator : TreeItemOption
//{
//    public VuMeterCreator(IServiceProvider serviceProvider) 
//        : base("vu meter", "Vu Meter", "Numeric Controls", "No Children here", TreeItemOption.CreateDiamondImage(Colors.DarkBlue), 
//        serviceProvider)
//    { }
//}
