using FlexibleHMI.ViewModels;
using System.Windows;

namespace FlexibleHMI.Views;

/// <summary>Displays a yes-or-no confirmation dialog.</summary>
public partial class ConfirmationDialog : Window
{
    /// <summary>Initializes the dialog with the specified message and title.</summary>
    /// <param name="message">The confirmation message shown to the user.</param>
    /// <param name="title">The dialog title.</param>
    public ConfirmationDialog(string message, string title)
    {
        InitializeComponent();
        DataContext = new ConfirmationDialogViewModel(message, title);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
