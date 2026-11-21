using FlexibleHMI.Views;
using System.Windows;

namespace FlexibleHMI.Services;

public sealed class MessageBoxConfirmationService : IConfirmationService
{
    public bool Confirm(string message, string title)
    {
        var dialog = new ConfirmationDialog(message, title)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

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
