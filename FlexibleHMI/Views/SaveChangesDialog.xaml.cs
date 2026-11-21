using System.Windows;

namespace FlexibleHMI.Views;

public partial class SaveChangesDialog : Window
{
    public SaveChangesDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Discard_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
