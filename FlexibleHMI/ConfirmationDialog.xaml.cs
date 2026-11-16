using System.Windows;

namespace FlexibleHMI;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string message, string title)
    {
        InitializeComponent();
        DataContext = new ConfirmationDialogViewModel(message, title);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

internal sealed class ConfirmationDialogViewModel(string message, string dialogTitle)
{
    public string Message { get; } = message;

    public string DialogTitle { get; } = dialogTitle;
}
