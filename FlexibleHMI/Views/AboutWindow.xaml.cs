using System.Reflection;
using System.Windows;

namespace FlexibleHMI.Views;

/// <summary>Displays application version and about information.</summary>
public partial class AboutWindow : Window
{
    /// <summary>Initializes the about window and reads the application version.</summary>
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;

        VersionTextBlock.Text = version is null
            ? "Version unavailable"
            : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }
}
