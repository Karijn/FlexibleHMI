namespace FlexibleHMI.Services;

/// <summary>Reads and persists user preferences for the application window and documents.</summary>
public interface IUserSettingsService
{
    /// <summary>Gets the last saved window width.</summary>
    double WindowWidth { get; }

    /// <summary>Gets the last saved window height.</summary>
    double WindowHeight { get; }

    /// <summary>Gets the selected application theme.</summary>
    AppTheme Theme { get; }

    /// <summary>Gets whether the last-used document is loaded at startup.</summary>
    bool AutoLoadLastDocument { get; }

    /// <summary>Gets the path of the last-used document, if available.</summary>
    string? LastDocumentPath { get; }

    /// <summary>Gets whether toolbar buttons display text.</summary>
    bool ShowToolbarText { get; }

    /// <summary>Gets whether document changes are saved automatically.</summary>
    bool AutoSave { get; }

    /// <summary>Gets whether unsaved changes are saved when the application closes.</summary>
    bool AutoSaveOnClose { get; }

    /// <summary>Gets or sets whether the main content area is displayed full screen.</summary>
    bool IsMainContentFullScreen { get; set; }

    /// <summary>Gets or sets whether the left panel is expanded.</summary>
    bool IsLeftPanelExpanded { get; set; }

    /// <summary>Gets or sets the left panel's width in device-independent units.</summary>
    double LeftPanelWidth { get; set; }

    /// <summary>Gets or sets whether the right panel is expanded.</summary>
    bool IsRightPanelExpanded { get; set; }

    /// <summary>Gets or sets the right panel's width in device-independent units.</summary>
    double RightPanelWidth { get; set; }

    /// <summary>Gets the recently used document paths, newest first.</summary>
    IReadOnlyList<string> RecentFiles { get; }

    /// <summary>Adds a document path to the recent-file list.</summary>
    /// <param name="filePath">The path of the document to add.</param>
    void AddRecentFile(string filePath);

    /// <summary>Persists the window dimensions.</summary>
    /// <param name="width">The window width.</param>
    /// <param name="height">The window height.</param>
    void SaveWindowSize(double width, double height);

    /// <summary>Persists the selected theme.</summary>
    /// <param name="theme">The theme to save.</param>
    void SaveTheme(AppTheme theme);

    /// <summary>Persists whether to load the last-used document at startup.</summary>
    /// <param name="enabled">Whether automatic loading is enabled.</param>
    void SaveAutoLoadLastDocument(bool enabled);

    /// <summary>Persists the path of the last-used document.</summary>
    /// <param name="filePath">The document path to save.</param>
    void SaveLastDocumentPath(string filePath);

    /// <summary>Persists whether toolbar buttons display text.</summary>
    /// <param name="show">Whether toolbar text is displayed.</param>
    void SaveShowToolbarText(bool show);

    /// <summary>Persists whether automatic document saving is enabled.</summary>
    /// <param name="enabled">Whether automatic saving is enabled.</param>
    void SaveAutoSave(bool enabled);

    /// <summary>Persists whether unsaved changes are saved on application close.</summary>
    /// <param name="enabled">Whether saving on close is enabled.</param>
    void SaveAutoSaveOnClose(bool enabled);
}
