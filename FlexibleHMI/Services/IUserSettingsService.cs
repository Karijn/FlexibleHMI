namespace FlexibleHMI.Services;

public interface IUserSettingsService
{
    double WindowWidth { get; }

    double WindowHeight { get; }

    AppTheme Theme { get; }

    bool AutoLoadLastDocument { get; }

    string? LastDocumentPath { get; }

    bool ShowToolbarText { get; }

    bool IsMainContentFullScreen { get; set; }


    bool IsLeftPanelExpanded { get; set; }
    double LeftPanelWidth { get; set; }

    bool IsRightPanelExpanded { get; set; }
    double RightPanelWidth { get; set; }




    IReadOnlyList<string> RecentFiles { get; }

    void AddRecentFile(string filePath);

    void SaveWindowSize(double width, double height);

    void SaveTheme(AppTheme theme);

    void SaveAutoLoadLastDocument(bool enabled);

    void SaveLastDocumentPath(string filePath);

    void SaveShowToolbarText(bool show);
}
