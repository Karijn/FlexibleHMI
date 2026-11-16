using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FlexibleHMI;

public partial class ThemedFileDialog : Window
{
    private readonly FileDialogMode _mode;

    public ThemedFileDialog(FileDialogMode mode, string? initialFilePath = null)
    {
        InitializeComponent();
        _mode = mode;
        DataContext = this;
        Title = mode == FileDialogMode.Open ? "Open HMI document" : "Save HMI document";
        AcceptButton.Content = mode == FileDialogMode.Open ? "Open" : "Save";
        StatusTextBlock.Text = mode == FileDialogMode.Open
            ? "Select an HMI document to open."
            : "Choose a name for the HMI document.";

        var initialDirectory = Path.GetDirectoryName(initialFilePath ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            CurrentDirectory = initialDirectory;
            FileNameTextBox.Text = Path.GetFileName(initialFilePath);
        }
        else
        {
            var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            CurrentDirectory = Directory.Exists(documentsDirectory)
                ? documentsDirectory
                : Environment.CurrentDirectory;
        }

        LoadDirectory(CurrentDirectory);
    }

    public ObservableCollection<FileDialogEntry> Entries { get; } = new();

    public string SelectedPath { get; private set; } = string.Empty;

    private string CurrentDirectory { get; set; } = string.Empty;

    public static string? ShowOpen(string? initialFilePath = null) => Show(FileDialogMode.Open, initialFilePath);

    public static string? ShowSave(string? initialFilePath = null) => Show(FileDialogMode.Save, initialFilePath);

    private static string? Show(FileDialogMode mode, string? initialFilePath)
    {
        var dialog = new ThemedFileDialog(mode, initialFilePath);
        if (Application.Current?.MainWindow is { IsVisible: true } owner)
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
    }

    private void LoadDirectory(string directory)
    {
        try
        {
            var fullPath = Path.GetFullPath(directory);
            if (!Directory.Exists(fullPath))
            {
                StatusTextBlock.Text = $"Folder does not exist: {fullPath}";
                return;
            }

            var folders = Directory.GetDirectories(fullPath)
                .Select(path => new FileDialogEntry(GetEntryName(path), path, true));
            var files = Directory.GetFiles(fullPath, "*.hmi")
                .Select(path => new FileDialogEntry(GetEntryName(path), path, false));

            Entries.Clear();
            foreach (var entry in folders.Concat(files).OrderBy(entry => entry.IsDirectory ? 0 : 1)
                         .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                Entries.Add(entry);
            }

            CurrentDirectory = fullPath;
            FolderPathTextBox.Text = fullPath;
            EntriesListBox.SelectedItem = null;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            StatusTextBlock.Text = $"Could not list folder: {exception.Message}";
        }
    }

    private static string GetEntryName(string path)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return string.IsNullOrEmpty(name) ? path : name;
    }

    private void GoToFolder_Click(object sender, RoutedEventArgs e) => LoadDirectory(FolderPathTextBox.Text);

    private void FolderPathTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            LoadDirectory(FolderPathTextBox.Text);
            e.Handled = true;
        }
    }

    private void FileNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AcceptSelection();
            e.Handled = true;
        }
    }

    private void EntriesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesListBox.SelectedItem is not FileDialogEntry entry)
        {
            return;
        }

        if (entry.IsDirectory)
        {
            LoadDirectory(entry.Path);
            return;
        }

        FileNameTextBox.Text = entry.Name;
        if (_mode == FileDialogMode.Open)
        {
            AcceptSelection();
        }
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e) => AcceptSelection();

    private void AcceptSelection()
    {
        var fileName = FileNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or ".." ||
            !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal) ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            StatusTextBlock.Text = "Enter a valid file name without a folder path.";
            return;
        }

        var path = Path.Combine(CurrentDirectory, fileName);
        if (_mode == FileDialogMode.Open)
        {
            if (!string.Equals(Path.GetExtension(path), ".hmi", StringComparison.OrdinalIgnoreCase))
            {
                StatusTextBlock.Text = "Select an HMI document (*.hmi).";
                return;
            }

            if (!File.Exists(path))
            {
                StatusTextBlock.Text = "The selected file does not exist.";
                return;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path += ".hmi";
            }

            if (File.Exists(path))
            {
                var confirmation = new ConfirmationDialog(
                    $"'{Path.GetFileName(path)}' already exists. Do you want to replace it?",
                    "Confirm save")
                {
                    Owner = this
                };
                if (confirmation.ShowDialog() != true)
                {
                    return;
                }
            }
        }

        SelectedPath = path;
        DialogResult = true;
    }

    private void EntriesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (EntriesListBox.SelectedItem is FileDialogEntry { IsDirectory: false } entry)
        {
            FileNameTextBox.Text = entry.Name;
        }
    }
}

public sealed record FileDialogEntry(string Name, string Path, bool IsDirectory)
{
    public string DisplayName => IsDirectory ? $"[Folder] {Name}" : Name;
}

public enum FileDialogMode
{
    Open,
    Save
}
