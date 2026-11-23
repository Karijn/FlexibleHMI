using FlexibleHMI.Views;
using System.Windows;

namespace FlexibleHMI.Services;

/// <summary>Implements confirmations with the application's owned WPF dialogs.</summary>
public sealed class MessageBoxConfirmationService : IConfirmationService
{
    /// <inheritdoc/>
    public bool Confirm(string message, string title)
    {
        var dialog = new ConfirmationDialog(message, title)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

    /// <inheritdoc/>
    public UnsavedChangesDecision ConfirmSaveChanges()
    {
        var dialog = new SaveChangesDialog
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() switch
        {
            true => UnsavedChangesDecision.Save,
            false => UnsavedChangesDecision.Discard,
            _ => UnsavedChangesDecision.Cancel
        };
    }
}
