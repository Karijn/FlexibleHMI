using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlexibleHMI;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfirmationService, MessageBoxConfirmationService>();
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<ITreeDocumentService, TreeDocumentService>();
                    services.AddSingleton<IUserSettingsService, UserSettingsService>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

        _host.Start();
        var settings = _host.Services.GetRequiredService<IUserSettingsService>();
        _host.Services.GetRequiredService<IThemeService>().ApplyTheme(settings.Theme);
        MainWindowViewModel viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

        viewModel.IsMainContentFullScreen = settings.IsMainContentFullScreen;

        //var leftPanel = _serviceProvider.GetRequiredService<LeftExpanderViewModel>();
        viewModel.IsLeftExpanded = settings.IsLeftPanelExpanded;
        viewModel.LeftPanelWidth = settings.LeftPanelWidth;

        //var rightPanel = _serviceProvider.GetRequiredService<RightExpanderViewModel>();
        viewModel.IsRightExpanded = settings.IsRightPanelExpanded;
        viewModel.RightPanelWidth = settings.RightPanelWidth;

        _host.Services.GetRequiredService<MainWindow>().Show();
        viewModel.LoadLastDocumentAtStartup();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
