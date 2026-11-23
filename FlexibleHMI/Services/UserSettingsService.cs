using System.IO;
using System.Text.Json;

namespace FlexibleHMI.Services;

/// <summary>Stores application preferences in the current user's local application data.</summary>
public sealed class UserSettingsService : IUserSettingsService
{
    private const int MaximumRecentFiles = 10;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    private readonly UserSettings _settings;

    /// <summary>Loads saved preferences and removes unavailable or duplicate recent-file entries.</summary>
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

    /// <inheritdoc/>
    public double WindowWidth => IsValidSize(_settings.WindowWidth) ? _settings.WindowWidth : 800;

    /// <inheritdoc/>
    public double WindowHeight => IsValidSize(_settings.WindowHeight) ? _settings.WindowHeight : 450;

    /// <inheritdoc/>
    public AppTheme Theme => Enum.IsDefined(_settings.Theme) ? _settings.Theme : AppTheme.Light;

    /// <inheritdoc/>
    public bool AutoLoadLastDocument => _settings.AutoLoadLastDocument;

    /// <inheritdoc/>
    public string? LastDocumentPath => _settings.LastDocumentPath;

    /// <inheritdoc/>
    public bool ShowToolbarText => _settings.ShowToolbarText;

    /// <inheritdoc/>
    public bool AutoSave => _settings.AutoSave;

    /// <inheritdoc/>
    public bool AutoSaveOnClose => _settings.AutoSaveOnClose;

    /// <inheritdoc/>
    public IReadOnlyList<string> RecentFiles => _settings.RecentFiles;


    /// <inheritdoc/>
    public bool IsMainContentFullScreen { get => _settings.IsMainContentFullScreen; set => _settings.IsMainContentFullScreen = value; }
    /// <inheritdoc/>
    public bool IsLeftPanelExpanded { get => _settings.IsLeftPanelExpanded; set => _settings.IsLeftPanelExpanded = value; }
    /// <inheritdoc/>
    public double LeftPanelWidth { get => _settings.LeftPanelWidth; set => _settings.LeftPanelWidth = value; }
    /// <inheritdoc/>
    public bool IsRightPanelExpanded { get => _settings.IsRightPanelExpanded; set => _settings.IsRightPanelExpanded = value; }
    /// <inheritdoc/>
    public double RightPanelWidth { get => _settings.RightPanelWidth; set => _settings.RightPanelWidth = value; }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void SaveTheme(AppTheme theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unsupported theme.");
        }

        _settings.Theme = theme;
        Save();
    }

    /// <inheritdoc/>
    public void SaveAutoLoadLastDocument(bool enabled)
    {
        _settings.AutoLoadLastDocument = enabled;
        Save();
    }

    /// <inheritdoc/>
    public void SaveLastDocumentPath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _settings.LastDocumentPath = Path.GetFullPath(filePath);
        Save();
    }

    /// <inheritdoc/>
    public void SaveShowToolbarText(bool show)
    {
        _settings.ShowToolbarText = show;
        Save();
    }

    /// <inheritdoc/>
    public void SaveAutoSave(bool enabled)
    {
        _settings.AutoSave = enabled;
        Save();
    }

    /// <inheritdoc/>
    public void SaveAutoSaveOnClose(bool enabled)
    {
        _settings.AutoSaveOnClose = enabled;
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

        public bool AutoSave { get; set; }

        public bool AutoSaveOnClose { get; set; }

        public bool IsMainContentFullScreen { get; set; } = false;
        public bool IsLeftPanelExpanded { get; set; } = true;
        public double LeftPanelWidth { get; set ; } = 300;
        public bool IsRightPanelExpanded { get; set; } = true;
        public double RightPanelWidth { get; set; } = 300;


        public List<string>? RecentFiles { get; set; } = new();
    }
}
