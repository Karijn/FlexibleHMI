using FlexibleHMI.ViewModels;
using System.Windows;

namespace FlexibleHMI.Views;

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
