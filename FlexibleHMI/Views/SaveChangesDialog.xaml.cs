using System.Windows;

namespace FlexibleHMI.Views;

/// <summary>Prompts whether to save, discard, or cancel closing a document with unsaved changes.</summary>
public partial class SaveChangesDialog : Window
{
    /// <summary>Initializes the save-changes dialog.</summary>
    public SaveChangesDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Discard_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = null;
}
