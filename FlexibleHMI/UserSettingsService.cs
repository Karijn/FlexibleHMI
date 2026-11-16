using System.IO;
using System.Text.Json;

namespace FlexibleHMI;

public sealed class UserSettingsService : IUserSettingsService
{
    private const int MaximumRecentFiles = 10;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    private readonly UserSettings _settings;

    public UserSettingsService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlexibleHMI");
        _settingsPath = Path.Combine(directory, "settings.json");

        if (File.Exists(_settingsPath))
        {
            _settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_settingsPath))
                ?? new UserSettings();
        }
        else
        {
            _settings = new UserSettings();
        }

        _settings.RecentFiles ??= new List<string>();
        _settings.RecentFiles = _settings.RecentFiles
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumRecentFiles)
            .ToList();
    }

    public double WindowWidth => IsValidSize(_settings.WindowWidth) ? _settings.WindowWidth : 800;

    public double WindowHeight => IsValidSize(_settings.WindowHeight) ? _settings.WindowHeight : 450;

    public AppTheme Theme => Enum.IsDefined(_settings.Theme) ? _settings.Theme : AppTheme.Light;

    public bool AutoLoadLastDocument => _settings.AutoLoadLastDocument;

    public string? LastDocumentPath => _settings.LastDocumentPath;

    public bool ShowToolbarText => _settings.ShowToolbarText;

    public IReadOnlyList<string> RecentFiles => _settings.RecentFiles;

    public void AddRecentFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _settings.RecentFiles.RemoveAll(path =>
            string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase));
        _settings.RecentFiles.Insert(0, Path.GetFullPath(filePath));
        if (_settings.RecentFiles.Count > MaximumRecentFiles)
        {
            _settings.RecentFiles.RemoveRange(MaximumRecentFiles, _settings.RecentFiles.Count - MaximumRecentFiles);
        }

        Save();
    }

    public void SaveWindowSize(double width, double height)
    {
        if (!IsValidSize(width) || !IsValidSize(height))
        {
            return;
        }

        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        Save();
    }

    public void SaveTheme(AppTheme theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unsupported theme.");
        }

        _settings.Theme = theme;
        Save();
    }

    public void SaveAutoLoadLastDocument(bool enabled)
    {
        _settings.AutoLoadLastDocument = enabled;
        Save();
    }

    public void SaveLastDocumentPath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _settings.LastDocumentPath = Path.GetFullPath(filePath);
        Save();
    }

    public void SaveShowToolbarText(bool show)
    {
        _settings.ShowToolbarText = show;
        Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_settings, SerializerOptions));
        File.Move(temporaryPath, _settingsPath, true);
    }

    private static bool IsValidSize(double size) =>
        double.IsFinite(size) && size >= 300 && size <= 10000;

    private sealed class UserSettings
    {
        public UserSettings()
        {
        }

        public double WindowWidth { get; set; } = 800;

        public double WindowHeight { get; set; } = 450;

        public AppTheme Theme { get; set; } = AppTheme.Light;

        public bool AutoLoadLastDocument { get; set; }

        public string? LastDocumentPath { get; set; }

        public bool ShowToolbarText { get; set; } = true;

        public List<string>? RecentFiles { get; set; } = new();
    }
}
