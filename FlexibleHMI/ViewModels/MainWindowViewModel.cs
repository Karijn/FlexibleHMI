using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using FlexibleHMI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ParameterRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand<object?>;

namespace FlexibleHMI.ViewModels;

/// <summary>Provides document editing state and commands consumed by the main application window.</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfirmationService _confirmationService;
    private readonly IThemeService _themeService;
    private readonly ITreeDocumentService _treeDocumentService;
    private readonly ITreeDocumentProperties _treeDocumentProperties;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IDictionary<string, TreeItemOption> _creators;
    private static readonly string[] RequiredManagerKinds = ["propertymanager", "calculation", "functiongenerator", "communicationManager"];
    private TreeItemOption? _selectedItemOption;
    private TreeItemNode? _selectedTreeItem;
    private TreeItemNode? _clipboardItem;
    private bool _clipboardIsCopy;
    private string _dropStatus = "Ready.";
    private string? _currentFilePath;
    private string _statusMessage = "Ready.";
    private bool _isDirty;
    private string[] _savedManagerData = [];
    private readonly HashSet<IDocumentDataChangeNotifier> _documentDataNotifiers = [];
    private readonly DispatcherTimer _autoSaveTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _nextItemNumber = 1;

    /// <summary>Gets or sets the persisted width of the right panel.</summary>
    public double RightPanelWidth
    {
        get => _userSettingsService.RightPanelWidth;
        set
        {
            _userSettingsService.RightPanelWidth = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the status text displayed by the main-window panel controls.</summary>
    [ObservableProperty]
    private string _statusText;

    /// <summary>Gets or sets the persisted width of the left panel.</summary>
    public double LeftPanelWidth
    {
        get => _userSettingsService.LeftPanelWidth;
        set
        {
            _userSettingsService.LeftPanelWidth = value;
            OnPropertyChanged();
        }
    }


    /// <summary>Gets or sets whether the left panel is expanded.</summary>
    public bool IsLeftExpanded
    {
        get => _userSettingsService.IsLeftPanelExpanded;
        set
        {
            _userSettingsService.IsLeftPanelExpanded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets whether the right panel is expanded.</summary>
    public bool IsRightExpanded
    {
        get => _userSettingsService.IsRightPanelExpanded;
        set
        {
            _userSettingsService.IsRightPanelExpanded = value;
            OnPropertyChanged();
        }
    }

    private bool _isMainContentFullScreen;

    /// <summary>Gets or sets whether the main content is full screen; setting it true collapses both side panels.</summary>
    public bool IsMainContentFullScreen
    {
        get => _isMainContentFullScreen;
        set
        {
            SetProperty(ref _isMainContentFullScreen, value);
            //OnIsLeftExpandedChanged(value);
            if (value)
            {
                IsLeftExpanded = false;
                IsRightExpanded = false;
            }
        }
    }


    /// <summary>Creates the main-window view model and initializes document-manager items and UI commands.</summary>
    /// <param name="confirmationService">Displays confirmation and unsaved-change dialogs.</param>
    /// <param name="themeService">Applies and reports the active theme.</param>
    /// <param name="treeDocumentService">Loads and saves tree documents.</param>
    /// <param name="treeDocumentProperties">Provides document-level properties and change notifications.</param>
    /// <param name="userSettingsService">Reads and persists user preferences.</param>
    /// <param name="creators">The registered tree-item creators keyed by item kind.</param>
    public MainWindowViewModel(
        IConfirmationService confirmationService,
        IThemeService themeService,
        ITreeDocumentService treeDocumentService,
        ITreeDocumentProperties treeDocumentProperties,
        IUserSettingsService userSettingsService,
        IDictionary<string, TreeItemOption> creators)
    {
        _confirmationService = confirmationService;
        _themeService = themeService;
        _treeDocumentService = treeDocumentService;
        _treeDocumentProperties = treeDocumentProperties;
        _treeDocumentProperties.Changed += OnDocumentPropertiesChanged;
        _userSettingsService = userSettingsService;
        _creators = creators;
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;
        RecentFiles = new ObservableCollection<string>(_userSettingsService.RecentFiles);

        //var creators = _serviceProvider.GetRequiredService<IDictionary<string, TreeItemOption>>();


        AvailableItems = new ObservableCollection<TreeItemOption>(creators.Values.Where(option => !IsRequiredManager(option.Kind)));
        //AvailableItems = new ObservableCollection<TreeItemOption>
        //{
        //    new("Leaf", "Leaf item", "Test Nodes", "Cannot contain children", TreeItemOption.CreateDiamondImage(Colors.Red), () => new LeafItemNode()),
        //    new("SingleChild", "Single-child item", "Test Nodes", "Can contain at most one child", TreeItemOption.CreateDiamondImage(Colors.Blue), () => new SingleChildItemNode()),
        //    new("Collection", "Collection item", "Test Nodes", "Can contain any number of children", TreeItemOption.CreateDiamondImage(Colors.Silver), () => new CollectionItemNode()),
        //    new("Grid", "Grid item", "Test Nodes", "Can contain any number of children", TreeItemOption.CreateDiamondImage(Colors.Gold), () => new GridItemNode()),
        //    new("vu meter", "Vu Meter", "Numeric Controls", "No Children here", TreeItemOption.CreateDiamondImage(Colors.DarkBlue), () => new VuMeterNode()),
        //};

        AddItemCommand = new RelayCommand(() => AddSelectedItem(DropPosition.Child), () => CanAddItemAt(DropPosition.Child));
        AddBeforeItemCommand = new RelayCommand(() => AddSelectedItem(DropPosition.Before), () => CanAddItemAt(DropPosition.Before));
        AddAfterItemCommand = new RelayCommand(() => AddSelectedItem(DropPosition.After), () => CanAddItemAt(DropPosition.After));
        CutItemCommand = new RelayCommand(CutSelectedItem, () => SelectedTreeItem is not null);
        CopyItemCommand = new RelayCommand(CopySelectedItem, () => SelectedTreeItem is not null);
        PasteItemCommand = new RelayCommand(() => PasteItem(DropPosition.Child), () => CanPasteItem(DropPosition.Child));
        PasteBeforeItemCommand = new RelayCommand(() => PasteItem(DropPosition.Before), () => CanPasteItem(DropPosition.Before));
        PasteAfterItemCommand = new RelayCommand(() => PasteItem(DropPosition.After), () => CanPasteItem(DropPosition.After));
        DeleteItemCommand = new RelayCommand(DeleteSelectedItem, () => SelectedTreeItem is not null);
        MoveUpItemCommand = new RelayCommand(() => MoveSelectedSibling(-1), CanMoveSelectedUp);
        MoveDownItemCommand = new RelayCommand(() => MoveSelectedSibling(1), CanMoveSelectedDown);
        MoveToParentCommand = new RelayCommand(MoveSelectedToParent, CanMoveSelectedToParent);
        MoveToChildCommand = new RelayCommand(MoveSelectedToPreviousSibling, CanMoveSelectedToPreviousSibling);
        MoveToFirstChildCommand = new RelayCommand(() => MoveSelectedToSiblingEdge(first: true), CanMoveSelectedToFirstChild);
        MoveToLastChildCommand = new RelayCommand(() => MoveSelectedToSiblingEdge(first: false), CanMoveSelectedToLastChild);
        ExpandSelectedNodeCommand = new RelayCommand(ExpandSelectedNode, () => SelectedTreeItem is not null);
        CollapseSelectedNodeCommand = new RelayCommand(CollapseSelectedNode, () => SelectedTreeItem is not null);
        ExpandAllNodesCommand = new RelayCommand(ExpandAllNodes, () => true);
        CollapseAllNodesCommand = new RelayCommand(CollapseAllNodes, () => true);
        UseLightThemeCommand = new RelayCommand(() => SetTheme(AppTheme.Light), () => true);
        UseDarkThemeCommand = new RelayCommand(() => SetTheme(AppTheme.Dark), () => true);
        NewDocumentCommand = new RelayCommand(NewDocument, () => true);
        OpenDocumentCommand = new RelayCommand(OpenDocument, () => true);
        OpenRecentFileCommand = new ParameterRelayCommand(OpenRecentFile, parameter => parameter is string);
        SaveDocumentCommand = new RelayCommand(() => SaveDocument(false), () => true);
        SaveDocumentAsCommand = new RelayCommand(() => SaveDocument(true), () => true);
        StartAllFunctionGeneratorsCommand = new RelayCommand(StartAllFunctionGenerators);
        StopAllFunctionGeneratorsCommand = new RelayCommand(StopAllFunctionGenerators);
        AboutCommand = new RelayCommand(() => AboutRequested?.Invoke());
        EnsureRequiredManagers();
        CaptureManagerData();
    }

    /// <summary>Raised when the UI should scroll to and select the specified tree item.</summary>
    public event Action<TreeItemNode>? TreeItemRevealRequested;

    /// <summary>Raised when the user requests the About window.</summary>
    public event Action? AboutRequested;

    /// <summary>Gets the window title, prefixed with an asterisk while the document has unsaved changes.</summary>
    public string WindowTitle => IsDirty ? "*Flexible HMI" : "Flexible HMI";

    /// <summary>Gets the registered item types available for insertion, excluding required document managers.</summary>
    public ObservableCollection<TreeItemOption> AvailableItems { get; }

    /// <summary>Gets the root-level tree items displayed and edited by the main window.</summary>
    public ObservableCollection<TreeItemNode> RootItems { get; } = new();

    /// <summary>Gets the recent document paths displayed in the main window.</summary>
    public ObservableCollection<string> RecentFiles { get; }

    /// <summary>Gets the command that replaces the current document with a new one.</summary>
    public ICommand NewDocumentCommand { get; }

    /// <summary>Gets the command that opens a recent document path supplied as its command parameter.</summary>
    public ICommand OpenRecentFileCommand { get; }

    /// <summary>Gets the command that adds the selected item type under the selected node, or at the root.</summary>
    public ICommand AddItemCommand { get; }

    /// <summary>Gets the command that inserts the selected item type before the selected node.</summary>
    public ICommand AddBeforeItemCommand { get; }

    /// <summary>Gets the command that inserts the selected item type after the selected node.</summary>
    public ICommand AddAfterItemCommand { get; }

    /// <summary>Gets the command that places the selected node on the move clipboard.</summary>
    public ICommand CutItemCommand { get; }

    /// <summary>Gets the command that places a copy of the selected node on the clipboard.</summary>
    public ICommand CopyItemCommand { get; }

    /// <summary>Gets the command that pastes the clipboard node under the selected node, or at the root.</summary>
    public ICommand PasteItemCommand { get; }

    /// <summary>Gets the command that pastes the clipboard node before the selected node.</summary>
    public ICommand PasteBeforeItemCommand { get; }

    /// <summary>Gets the command that pastes the clipboard node after the selected node.</summary>
    public ICommand PasteAfterItemCommand { get; }

    /// <summary>Gets the command that deletes the selected node and its descendants after confirmation.</summary>
    public ICommand DeleteItemCommand { get; }

    /// <summary>Gets the command that moves the selected node up among its siblings.</summary>
    public ICommand MoveUpItemCommand { get; }

    /// <summary>Gets the command that moves the selected node down among its siblings.</summary>
    public ICommand MoveDownItemCommand { get; }

    /// <summary>Gets the command that moves the selected node out one level in the tree.</summary>
    public ICommand MoveToParentCommand { get; }

    /// <summary>Gets the command that makes the selected node a child of its previous sibling.</summary>
    public ICommand MoveToChildCommand { get; }

    /// <summary>Gets the command that moves the selected node to the first child position among its siblings.</summary>
    public ICommand MoveToFirstChildCommand { get; }

    /// <summary>Gets the command that moves the selected node to the last child position among its siblings.</summary>
    public ICommand MoveToLastChildCommand { get; }

    /// <summary>Gets the command that expands the selected tree node.</summary>
    public ICommand ExpandSelectedNodeCommand { get; }

    /// <summary>Gets the command that collapses the selected tree node.</summary>
    public ICommand CollapseSelectedNodeCommand { get; }

    /// <summary>Gets the command that expands every node in the tree.</summary>
    public ICommand ExpandAllNodesCommand { get; }

    /// <summary>Gets the command that collapses every node in the tree.</summary>
    public ICommand CollapseAllNodesCommand { get; }

    /// <summary>Gets the command that applies the light theme.</summary>
    public ICommand UseLightThemeCommand { get; }

    /// <summary>Gets the command that applies the dark theme.</summary>
    public ICommand UseDarkThemeCommand { get; }

    /// <summary>Gets the command that prompts for and opens a document.</summary>
    public ICommand OpenDocumentCommand { get; }

    /// <summary>Gets the command that saves the current document to its current path.</summary>
    public ICommand SaveDocumentCommand { get; }

    /// <summary>Gets the command that prompts for a destination and saves the document there.</summary>
    public ICommand SaveDocumentAsCommand { get; }

    /// <summary>Gets the command that raises <see cref="AboutRequested"/>.</summary>
    public ICommand AboutCommand { get; }

    /// <summary>Gets the command that starts all tree items implementing the runtime controller contract.</summary>
    public ICommand StartAllFunctionGeneratorsCommand { get; }

    /// <summary>Gets the command that stops all tree items implementing the runtime controller contract.</summary>
    public ICommand StopAllFunctionGeneratorsCommand { get; }

    /// <summary>Gets the current document path, or a label indicating that the document has not been saved.</summary>
    public string CurrentFilePath
    {
        get => _currentFilePath ?? "Unsaved document";
        private set
        {
            if (_currentFilePath == value)
            {
                return;
            }

            _currentFilePath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets whether document content, manager data, or document properties have changed since the last save.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (_isDirty == value)
            {
                if (value)
                {
                    ScheduleAutoSave();
                }

                return;
            }

            _isDirty = value;
            if (value)
            {
                ScheduleAutoSave();
            }
            else
            {
                _autoSaveTimer.Stop();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    /// <summary>Gets the current application status message shown by the main window.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets whether the light theme is currently active.</summary>
    public bool IsLightTheme => _themeService.CurrentTheme == AppTheme.Light;

    /// <summary>Gets whether the dark theme is currently active.</summary>
    public bool IsDarkTheme => _themeService.CurrentTheme == AppTheme.Dark;

    /// <summary>Gets or sets whether toolbar buttons display their text labels.</summary>
    public bool ShowToolbarText
    {
        get => _userSettingsService.ShowToolbarText;
        set
        {
            if (_userSettingsService.ShowToolbarText == value)
            {
                return;
            }

            try
            {
                _userSettingsService.SaveShowToolbarText(value);
                OnPropertyChanged();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Could not save toolbar setting: {exception.Message}";
            }
        }
    }

    /// <summary>Gets or sets whether dirty documents with a file path are periodically auto-saved.</summary>
    public bool AutoSave
    {
        get => _userSettingsService.AutoSave;
        set
        {
            if (_userSettingsService.AutoSave == value)
            {
                return;
            }

            try
            {
                _userSettingsService.SaveAutoSave(value);
                OnPropertyChanged();
                if (value && IsDirty)
                {
                    ScheduleAutoSave();
                }
                else
                {
                    _autoSaveTimer.Stop();
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Could not save auto-save setting: {exception.Message}";
                OnPropertyChanged();
                if (AutoSave && IsDirty)
                {
                    ScheduleAutoSave();
                }
                else
                {
                    _autoSaveTimer.Stop();
                }
            }
        }
    }

    /// <summary>Gets or sets whether closing a dirty document attempts to save it automatically.</summary>
    public bool AutoSaveOnClose
    {
        get => _userSettingsService.AutoSaveOnClose;
        set
        {
            if (_userSettingsService.AutoSaveOnClose == value)
            {
                return;
            }

            try
            {
                _userSettingsService.SaveAutoSaveOnClose(value);
                OnPropertyChanged();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Could not save auto-save-on-close setting: {exception.Message}";
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets or sets whether the last-used document is loaded when the application starts.</summary>
    public bool AutoLoadLastDocument
    {
        get => _userSettingsService.AutoLoadLastDocument;
        set
        {
            if (_userSettingsService.AutoLoadLastDocument == value)
            {
                return;
            }

            try
            {
                _userSettingsService.SaveAutoLoadLastDocument(value);
                OnPropertyChanged();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Could not save startup setting: {exception.Message}";
            }
        }
    }

    /// <summary>Gets the drag-and-drop guidance or result displayed by the main window.</summary>
    public string DropStatus
    {
        get => _dropStatus;
        private set
        {
            if (_dropStatus == value)
            {
                return;
            }

            _dropStatus = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the selected item type from the available-items list.</summary>
    public TreeItemOption? SelectedItemOption
    {
        get => _selectedItemOption;
        set
        {
            if (ReferenceEquals(_selectedItemOption, value))
            {
                return;
            }

            _selectedItemOption = value;
            OnPropertyChanged();
            RaiseAddCommandsCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets the selected tree node and updates command availability when it changes.</summary>
    public TreeItemNode? SelectedTreeItem
    {
        get => _selectedTreeItem;
        set
        {
            if (ReferenceEquals(_selectedTreeItem, value))
            {
                return;
            }

            _selectedTreeItem?.SetCurrent(false);
            _selectedTreeItem = value;
            _selectedTreeItem?.SetCurrent(true);
            OnPropertyChanged();
            OnPropertyChanged(nameof(AddTargetDescription));
            RaiseAddCommandsCanExecuteChanged();
            ((RelayCommand)CutItemCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CopyItemCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PasteItemCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PasteBeforeItemCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PasteAfterItemCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteItemCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExpandSelectedNodeCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CollapseSelectedNodeCommand).RaiseCanExecuteChanged();
            RaiseMoveCommandsCanExecuteChanged();
        }
    }

    /// <summary>Gets a description of where a newly added item will be placed.</summary>
    public string AddTargetDescription => SelectedTreeItem is null
        ? "New items will be added to the root."
        : $"Selected parent: {SelectedTreeItem.Name} ({SelectedTreeItem.ChildRule}).";

    private void SetTheme(AppTheme theme)
    {
        _themeService.ApplyTheme(theme);
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));

        try
        {
            _userSettingsService.SaveTheme(theme);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save theme setting: {exception.Message}";
        }
    }

    private void NewDocument()
    {
        if ((IsDirty || HasManagerChanges() || RootItems.Any(item => !IsRequiredManager(item.Kind)) || _treeDocumentProperties.GetProperties().Count > 0) && !_confirmationService.Confirm(
                "Discard the current tree and properties and create a new document? This cannot be undone.",
                "New document"))
        {
            return;
        }

        if (!StopDocumentRuntimeActivity())
        {
            return;
        }

        foreach (var item in RootItems)
        {
            UnsubscribeDocumentDataChanges(item);
            DetachDocumentItem(item);
        }

        _treeDocumentProperties.LoadProperties([]);
        RootItems.Clear();
        EnsureRequiredManagers();
        CaptureManagerData();
        SelectedTreeItem = null;
        _clipboardItem = null;
        _currentFilePath = null;
        OnPropertyChanged(nameof(CurrentFilePath));
        IsDirty = false;
        StatusMessage = "New document.";
        DropStatus = StatusMessage;
        RaisePasteCommandsCanExecuteChanged();
    }

    /// <summary>Loads the last-used document at startup when the corresponding preference and path are available.</summary>
    public void LoadLastDocumentAtStartup()
    {
        if (!_userSettingsService.AutoLoadLastDocument ||
            string.IsNullOrWhiteSpace(_userSettingsService.LastDocumentPath))
        {
            return;
        }

        if (!File.Exists(_userSettingsService.LastDocumentPath))
        {
            StatusMessage = "The last document could not be found.";
            DropStatus = StatusMessage;
            return;
        }

        OpenDocument(_userSettingsService.LastDocumentPath);
    }

    private void OpenDocument() => OpenDocument(null);

    private void OpenRecentFile(object? parameter)
    {
        if (parameter is string filePath)
        {
            OpenDocument(filePath);
        }
    }

    private void OpenDocument(string? filePath)
    {
        LoadedTreeDocument? document = null;
        var documentInstalled = false;
        try
        {
            document = filePath is null
                ? _treeDocumentService.Load()
                : _treeDocumentService.Load(filePath);
            if (document is null)
            {
                return;
            }

            if ((IsDirty || HasManagerChanges() || RootItems.Any(item => !IsRequiredManager(item.Kind)) || _treeDocumentProperties.GetProperties().Count > 0) && !_confirmationService.Confirm(
                    "Replace the current tree and properties with the opened document? This cannot be undone.",
                    "Open document"))
            {
                return;
            }

            if (!StopDocumentRuntimeActivity())
            {
                return;
            }

            foreach (var item in RootItems)
            {
                UnsubscribeDocumentDataChanges(item);
                DetachDocumentItem(item);
            }

            try
            {
                _treeDocumentProperties.LoadProperties(document.Properties);
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException)
            {
                foreach (var item in EnumerateTreeItems(RootItems))
                {
                    if (item is IDocumentItemDetached detached)
                    {
                        detached.OnDocumentItemReattached();
                    }
                }

                foreach (var item in RootItems)
                {
                    SubscribeDocumentDataChanges(item);
                }

                throw;
            }

            RootItems.Clear();
            foreach (var item in document.Items)
            {
                RootItems.Add(item);
                SubscribeDocumentDataChanges(item);
            }

            documentInstalled = true;
            EnsureRequiredManagers();
            CaptureManagerData();
            SelectedTreeItem = null;
            _clipboardItem = null;
            CurrentFilePath = document.FilePath;
            IsDirty = false;
            AddRecentFile(document.FilePath);
            SaveLastDocumentPath(document.FilePath);
            StatusMessage = $"Loaded {Path.GetFileName(document.FilePath)}.";
            DropStatus = StatusMessage;
            RaisePasteCommandsCanExecuteChanged();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusMessage = $"Could not open document: {exception.Message}";
            DropStatus = StatusMessage;
        }
        finally
        {
            if (document is not null && !documentInstalled)
            {
                foreach (var item in EnumerateTreeItems(document.Items))
                {
                    if (item is IDocumentItemDetached detached)
                    {
                        detached.OnDocumentItemDetached();
                    }
                }
            }
        }
    }

    private static bool IsRequiredManager(string kind) => RequiredManagerKinds.Contains(kind, StringComparer.Ordinal);

    private void EnsureRequiredManagers()
    {
        foreach (var kind in RequiredManagerKinds)
        {
            if (RootItems.Any(item => item.Kind == kind))
            {
                continue;
            }

            if (!_creators.TryGetValue(kind, out var creator))
            {
                throw new InvalidOperationException($"Required document manager '{kind}' is not available. Check the installed add-ins.");
            }

            var manager = creator.Create() ?? throw new InvalidOperationException($"Could not create required document manager '{kind}'.");
            RootItems.Add(manager);
            SubscribeDocumentDataChanges(manager);
        }
    }

    private string[] GetManagerData() => EnumerateTreeItems(RootItems)
        .Where(item => IsRequiredManager(item.Kind) && item is ITreeItemDocumentData)
        .Select(item => ((ITreeItemDocumentData)item).SaveDocumentData().GetRawText())
        .ToArray();

    private void CaptureManagerData() => _savedManagerData = GetManagerData();

    private bool HasManagerChanges() => !_savedManagerData.SequenceEqual(GetManagerData(), StringComparer.Ordinal);

    private void OnDocumentPropertiesChanged(object? sender, EventArgs e)
    {
        IsDirty = true;
    }

    private void ScheduleAutoSave()
    {
        if (!AutoSave || string.IsNullOrWhiteSpace(_currentFilePath))
        {
            _autoSaveTimer.Stop();
            return;
        }

        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void OnAutoSaveTimerTick(object? sender, EventArgs eventArgs)
    {
        _autoSaveTimer.Stop();
        if (AutoSave && IsDirty && !string.IsNullOrWhiteSpace(_currentFilePath))
        {
            SaveDocument(false, isAutoSave: true);
        }
    }

    private void AddRecentFile(string filePath)
    {
        _userSettingsService.AddRecentFile(filePath);
        RecentFiles.Clear();
        foreach (var recentFile in _userSettingsService.RecentFiles)
        {
            RecentFiles.Add(recentFile);
        }
    }

    private void SaveLastDocumentPath(string filePath)
    {
        try
        {
            _userSettingsService.SaveLastDocumentPath(filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save last document setting: {exception.Message}";
        }
    }

    private bool SaveDocument(bool saveAs, bool isAutoSave = false) =>
        TrySaveDocument(saveAs, isAutoSave, out _);

    private bool TrySaveDocument(bool saveAs, bool isAutoSave, out bool saveCanceled)
    {
        saveCanceled = false;
        try
        {
            var path = _treeDocumentService.Save(RootItems, _currentFilePath, saveAs);
            if (path is null)
            {
                saveCanceled = true;
                return false;
            }

            CurrentFilePath = path;
            CaptureManagerData();
            IsDirty = false;
            AddRecentFile(path);
            SaveLastDocumentPath(path);
            StatusMessage = isAutoSave
                ? $"Auto-saved {Path.GetFileName(path)}."
                : $"Saved {Path.GetFileName(path)}.";
            DropStatus = StatusMessage;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusMessage = $"Could not save document: {exception.Message}";
            DropStatus = StatusMessage;
            return false;
        }
    }

    private void StartAllFunctionGenerators()
    {
        foreach (var controller in EnumerateTreeItems(RootItems).OfType<IRuntimeStartStopController>())
        {
            controller.Start();
        }
    }

    private void StopAllFunctionGenerators()
    {
        foreach (var controller in EnumerateTreeItems(RootItems).OfType<IRuntimeStartStopController>())
        {
            controller.Stop();
        }
    }

    /// <summary>Attempts to stop runtime activity for every node in the current document.</summary>
    /// <returns><see langword="true"/> if all nodes stopped successfully; otherwise <see langword="false"/> and the status message reports failures.</returns>
    public bool StopDocumentRuntimeActivity()
    {
        var errors = new List<Exception>();
        foreach (var item in EnumerateTreeItems(RootItems))
        {
            try
            {
                item.StopRuntimeActivity();
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count == 0)
        {
            return true;
        }

        StatusMessage = $"Could not stop document activity: {string.Join("; ", errors.Select(error => error.Message))}";
        DropStatus = StatusMessage;
        return false;
    }

    private static IEnumerable<TreeItemNode> EnumerateTreeItems(IEnumerable<TreeItemNode> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in EnumerateTreeItems(item.Children))
            {
                yield return child;
            }
        }
    }

    /// <summary>Resolves unsaved changes according to the close preferences and confirmation dialog.</summary>
    /// <returns><see langword="true"/> if the window may close; otherwise <see langword="false"/>.</returns>
    public bool ConfirmClose()
    {
        _autoSaveTimer.Stop();

        if (!IsDirty && !HasManagerChanges())
        {
            return true;
        }

        if (AutoSaveOnClose)
        {
            if (TrySaveDocument(false, isAutoSave: false, out var saveCanceled))
            {
                return true;
            }

            if (saveCanceled)
            {
                return true;
            }

            ScheduleAutoSave();
            return false;
        }

        var decision = _confirmationService.ConfirmSaveChanges();
        if (decision == UnsavedChangesDecision.Save)
        {
            if (TrySaveDocument(false, isAutoSave: false, out var saveCanceled))
            {
                return true;
            }

            if (saveCanceled)
            {
                return false;
            }

            ScheduleAutoSave();
            return false;
        }

        if (decision == UnsavedChangesDecision.Discard)
        {
            return true;
        }

        ScheduleAutoSave();
        return false;
    }

    private bool CanAddItemAt(DropPosition position) =>
        SelectedItemOption is not null &&
        CanDrop(new TreeDragData(null, SelectedItemOption), SelectedTreeItem, position, false);

    private void AddSelectedItem(DropPosition position)
    {
        if (SelectedItemOption is null || !CanAddItemAt(position))
        {
            return;
        }

        Drop(new TreeDragData(null, SelectedItemOption), SelectedTreeItem, position, false);
        RaiseAddCommandsCanExecuteChanged();
    }

    private void RaiseAddCommandsCanExecuteChanged()
    {
        ((RelayCommand)AddItemCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddBeforeItemCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddAfterItemCommand).RaiseCanExecuteChanged();
    }

    private void CutSelectedItem()
    {
        _clipboardItem = SelectedTreeItem;
        _clipboardIsCopy = false;
        RaisePasteCommandsCanExecuteChanged();
        DropStatus = $"Cut {SelectedTreeItem!.Name}. Select a destination and paste to move it.";
    }

    private void CopySelectedItem()
    {
        _clipboardItem = SelectedTreeItem!.Copy();
        _clipboardIsCopy = true;
        RaisePasteCommandsCanExecuteChanged();
        DropStatus = $"Copied {SelectedTreeItem.Name}. Select a destination and paste.";
    }

    private bool CanPasteItem(DropPosition position) =>
        _clipboardItem is not null &&
        CanDrop(
            new TreeDragData(_clipboardItem, null),
            SelectedTreeItem,
            position,
            _clipboardIsCopy);

    private void PasteItem(DropPosition position)
    {
        if (_clipboardItem is null || !CanPasteItem(position))
        {
            return;
        }

        var item = _clipboardItem;
        var isCopy = _clipboardIsCopy;
        if (Drop(new TreeDragData(item, null), SelectedTreeItem, position, isCopy) && !isCopy)
        {
            _clipboardItem = null;
        }

        RaisePasteCommandsCanExecuteChanged();
    }

    private void RaisePasteCommandsCanExecuteChanged()
    {
        ((RelayCommand)PasteItemCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PasteBeforeItemCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PasteAfterItemCommand).RaiseCanExecuteChanged();
    }

    private bool CanMoveSelectedUp() =>
        SelectedTreeItem is { } item && FindVisibleSiblingIndex(GetSiblings(item), item, -1) >= 0;

    private bool CanMoveSelectedDown() =>
        SelectedTreeItem is { } item && FindVisibleSiblingIndex(GetSiblings(item), item, 1) >= 0;

    private bool CanMoveSelectedToFirstChild() => CanMoveSelectedUp();

    private bool CanMoveSelectedToLastChild() => CanMoveSelectedDown();

    private int FindVisibleSiblingIndex(IList<TreeItemNode> siblings, TreeItemNode item, int direction)
    {
        var index = siblings.IndexOf(item);
        if (index < 0)
        {
            return -1;
        }

        for (index += direction; index >= 0 && index < siblings.Count; index += direction)
        {
            if (!ReferenceEquals(siblings, RootItems) || !IsRequiredManager(siblings[index].Kind))
            {
                return index;
            }
        }

        return -1;
    }

    private void MoveSelectedToSiblingEdge(bool first)
    {
        var item = SelectedTreeItem;
        if (item is null)
        {
            return;
        }

        var siblings = GetSiblings(item);
        var currentIndex = siblings.IndexOf(item);
        var direction = first ? -1 : 1;
        var destinationIndex = FindVisibleSiblingIndex(siblings, item, direction);
        if (currentIndex < 0 || destinationIndex < 0)
        {
            return;
        }

        for (var index = destinationIndex + direction; index >= 0 && index < siblings.Count; index += direction)
        {
            if (!ReferenceEquals(siblings, RootItems) || !IsRequiredManager(siblings[index].Kind))
            {
                destinationIndex = index;
            }
        }

        ((ObservableCollection<TreeItemNode>)siblings).Move(currentIndex, destinationIndex);
        IsDirty = true;
        MakeCurrentAndReveal(item);
        DropStatus = $"Moved {item.Name} to the {(first ? "first" : "last")} child position.";
        RaiseMoveCommandsCanExecuteChanged();
    }

    private void ExpandSelectedNode()
    {
        if (SelectedTreeItem is { } item)
        {
            item.IsExpanded = true;
        }
    }

    private void CollapseSelectedNode()
    {
        if (SelectedTreeItem is { } item)
        {
            item.IsExpanded = false;
        }
    }

    private void ExpandAllNodes()
    {
        foreach (var item in EnumerateTreeItems())
        {
            item.IsExpanded = true;
        }
    }

    private void CollapseAllNodes()
    {
        foreach (var item in EnumerateTreeItems())
        {
            item.IsExpanded = false;
        }
    }

    private IEnumerable<TreeItemNode> EnumerateTreeItems()
    {
        var pending = new Stack<TreeItemNode>(RootItems.Reverse());
        while (pending.Count > 0)
        {
            var item = pending.Pop();
            yield return item;
            for (var index = item.Children.Count - 1; index >= 0; index--)
            {
                pending.Push(item.Children[index]);
            }
        }
    }

    private bool CanMoveSelectedToParent()
    {
        var item = SelectedTreeItem;
        if (item?.Parent is not { } parent)
        {
            return false;
        }

        var destinationParent = parent.Parent;
        return destinationParent is null ||
               destinationParent.CanAcceptChildCount(destinationParent.Children.Count + 1);
    }

    private bool CanMoveSelectedToPreviousSibling()
    {
        var item = SelectedTreeItem;
        if (item is null)
        {
            return false;
        }

        var siblings = GetSiblings(item);
        var index = FindVisibleSiblingIndex(siblings, item, -1);
        return index >= 0 && siblings[index].CanAcceptChildren;
    }

    private void MoveSelectedSibling(int offset)
    {
        var item = SelectedTreeItem;
        if (item is null)
        {
            return;
        }

        var siblings = GetSiblings(item);
        var index = siblings.IndexOf(item);
        var destinationIndex = FindVisibleSiblingIndex(siblings, item, offset);
        if (index < 0 || destinationIndex < 0)
        {
            return;
        }

        ((ObservableCollection<TreeItemNode>)siblings).Move(index, destinationIndex);
        IsDirty = true;
        MakeCurrentAndReveal(item);
        DropStatus = $"Moved {item.Name} {(offset < 0 ? "up" : "down")}.";
        RaiseMoveCommandsCanExecuteChanged();
    }

    private void MoveSelectedToParent()
    {
        var item = SelectedTreeItem;
        if (item?.Parent is not { } parent)
        {
            return;
        }

        var grandparent = parent.Parent;
        IList<TreeItemNode> destination = grandparent is null ? RootItems : grandparent.Children;
        var parentIndex = destination.IndexOf(parent);
        if (parentIndex < 0 || (grandparent is not null &&
            !grandparent.CanAcceptChildCount(destination.Count + 1)))
        {
            return;
        }

        parent.Children.Remove(item);
        if (grandparent is null)
        {
            RootItems.Insert(parentIndex + 1, item);
        }
        else
        {
            grandparent.Children.Insert(parentIndex + 1, item);
        }

        IsDirty = true;
        MakeCurrentAndReveal(item);
        DropStatus = $"Moved {item.Name} out one level.";
        RaiseMoveCommandsCanExecuteChanged();
    }

    private void MoveSelectedToPreviousSibling()
    {
        var item = SelectedTreeItem;
        if (item is null)
        {
            return;
        }

        var siblings = GetSiblings(item);
        var index = FindVisibleSiblingIndex(siblings, item, -1);
        if (index < 0)
        {
            return;
        }

        var newParent = siblings[index];
        if (!newParent.CanAcceptChildren)
        {
            return;
        }

        siblings.Remove(item);
        newParent.Children.Insert(newParent.Children.Count, item);
        IsDirty = true;
        MakeCurrentAndReveal(item);
        DropStatus = $"Moved {item.Name} under {newParent.Name}.";
        RaiseMoveCommandsCanExecuteChanged();
    }

    private IList<TreeItemNode> GetSiblings(TreeItemNode item) =>
        item.Parent is null ? RootItems : item.Parent.Children;

    private void RaiseMoveCommandsCanExecuteChanged()
    {
        ((RelayCommand)MoveUpItemCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MoveDownItemCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MoveToParentCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MoveToChildCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MoveToFirstChildCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MoveToLastChildCommand).RaiseCanExecuteChanged();
    }

    private void DeleteSelectedItem()
    {
        if (SelectedTreeItem is { } item)
        {
            DeleteItem(item);
        }
    }

    /// <summary>Deletes a node and its descendants after confirming with the user.</summary>
    /// <param name="item">The node to delete.</param>
    /// <returns><see langword="true"/> if the node was deleted; otherwise <see langword="false"/>.</returns>
    public bool DeleteItem(TreeItemNode item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsRequiredManager(item.Kind) || !IsInTree(item) || !_confirmationService.Confirm(
                $"Delete '{item.Name}' and all of its descendants? This cannot be undone.",
                "Confirm deletion"))
        {
            return false;
        }

        var selectedItemIsBeingDeleted = SelectedTreeItem is { } selected &&
                                         (ReferenceEquals(selected, item) || IsAncestorOf(item, selected));
        UnsubscribeDocumentDataChanges(item);
        DetachDocumentItem(item);
        if (item.Parent is not null)
        {
            item.Parent.Children.Remove(item);
        }
        else
        {
            RootItems.Remove(item);
        }

        if (selectedItemIsBeingDeleted)
        {
            SelectedTreeItem = null;
        }

        IsDirty = true;
        DropStatus = $"Deleted {item.Name} and its descendants.";
        return true;
    }

    /// <summary>Determines whether a drag payload can be inserted or moved to the proposed tree location.</summary>
    /// <param name="data">The dragged node or available-item option.</param>
    /// <param name="target">The target node, or <see langword="null"/> to target the root.</param>
    /// <param name="position">The insertion position relative to the target.</param>
    /// <param name="copy"><see langword="true"/> to copy a dragged node instead of moving it.</param>
    /// <returns><see langword="true"/> if the proposed drop satisfies tree and item constraints.</returns>
    public bool CanDrop(TreeDragData data, TreeItemNode? target, DropPosition position, bool copy)
    {
        var sourceNode = data.Node;
        if (sourceNode is null && data.Option is null)
        {
            return false;
        }

        if ((sourceNode is not null && IsRequiredManager(sourceNode.Kind)) ||
            (data.Option is not null && IsRequiredManager(data.Option.Kind)) ||
            (target is not null && IsRequiredManager(target.Kind)))
        {
            return false;
        }

        if (target is null && position != DropPosition.Child)
        {
            return false;
        }

        var destinationParent = target is null || position == DropPosition.Child ? target : target.Parent;
        IList<TreeItemNode> destinationItems = destinationParent is null
            ? RootItems
            : destinationParent.Children;
        var insertionIndex = target is null
            ? destinationItems.Count
            : position switch
            {
                DropPosition.Child => destinationItems.Count,
                DropPosition.Before => destinationItems.IndexOf(target),
                DropPosition.After => destinationItems.IndexOf(target) + 1,
                _ => -1
            };

        if (insertionIndex < 0)
        {
            return false;
        }

        if (sourceNode is not null)
        {
            if ((target is not null && ReferenceEquals(sourceNode, target)) ||
                IsAncestorOf(sourceNode, destinationParent))
            {
                return false;
            }

            IList<TreeItemNode> sourceItems = sourceNode.Parent is null
                ? RootItems
                : sourceNode.Parent.Children;
            var sourceIndex = sourceItems.IndexOf(sourceNode);
            if (sourceIndex < 0 && (!copy || IsInTree(sourceNode)))
            {
                return false;
            }

            if (sourceIndex >= 0 && !copy && ReferenceEquals(sourceItems, destinationItems))
            {
                if (sourceIndex < insertionIndex)
                {
                    insertionIndex--;
                }

                if (sourceIndex == insertionIndex)
                {
                    return false;
                }
            }
        }

        var childCount = destinationItems.Count;
        if (sourceNode is not null && !copy &&
            ReferenceEquals(sourceNode.Parent, destinationParent))
        {
            childCount--;
        }

        if (destinationParent is not null &&
            !destinationParent.CanAcceptChildCount(childCount + 1))
        {
            return false;
        }

        return true;
    }

    /// <summary>Moves or copies an item onto a grid cell using document tree operations.</summary>
    public bool DropOnGrid(TreeDragData data, TreeItemNode target, int row, int column, bool copy)
    {
        if (target is not IGridDropTarget grid || !IsInTree(target) || !grid.ContainsCell(row, column))
        {
            SetDropStatus("Drop is not allowed at this position.");
            return false;
        }

        var sourceSpan = data.Node is { Parent: IGridDropTarget source } node ? source.GetChildSpan(node) : ((int, int)?)null;
        var sameGrid = !copy && data.Node is { } moving && ReferenceEquals(moving.Parent, target) && IsInTree(moving);
        if (!copy)
        {
            var occupants = grid.GetChildrenAt(row, column, sameGrid ? data.Node : null);
            if (occupants.Count > 1 || (occupants.Count == 1 && !sameGrid))
            {
                SetDropStatus("Cannot move onto an occupied cell unless swapping one child in the same grid.");
                return false;
            }
            if (occupants.Count == 1)
            {
                grid.SwapChildren(data.Node!, occupants[0], row, column);
                SetDropStatus($"Swapped {data.Node!.Name} and {occupants[0].Name}.");
                return true;
            }
        }

        TreeItemNode? item;
        if (sameGrid)
            item = data.Node;
        else if (!Drop(data, target, DropPosition.Child, copy, out item))
        {
            SetDropStatus("Drop is not allowed at this position.");
            return false;
        }

        grid.PositionChild(item!, row, column, sourceSpan);
        SetDropStatus($"{(copy ? "Copied" : "Moved")} {item!.Name} to row {row}, column {column}.");
        return true;
    }

    /// <summary>Applies a valid tree drag-and-drop operation.</summary>
    /// <param name="data">The dragged node or available-item option.</param>
    /// <param name="target">The target node, or <see langword="null"/> to target the root.</param>
    /// <param name="position">The insertion position relative to the target.</param>
    /// <param name="copy"><see langword="true"/> to copy a dragged node instead of moving it.</param>
    /// <returns><see langword="true"/> if the item was inserted or moved; otherwise <see langword="false"/>.</returns>
    public bool Drop(TreeDragData data, TreeItemNode? target, DropPosition position, bool copy) =>
        Drop(data, target, position, copy, out _);

    /// <summary>Applies a tree drop and returns the inserted or moved item.</summary>
    public bool Drop(TreeDragData data, TreeItemNode? target, DropPosition position, bool copy, out TreeItemNode? droppedItem)
    {
        droppedItem = null;
        if (!CanDrop(data, target, position, copy))
        {
            return false;
        }

        var destinationParent = target is null || position == DropPosition.Child ? target : target.Parent;
        IList<TreeItemNode> destinationItems = destinationParent is null
            ? RootItems
            : destinationParent.Children;
        var insertionIndex = target is null
            ? destinationItems.Count
            : position switch
            {
                DropPosition.Child => destinationItems.Count,
                DropPosition.Before => destinationItems.IndexOf(target),
                DropPosition.After => destinationItems.IndexOf(target) + 1,
                _ => throw new ArgumentOutOfRangeException(nameof(position))
            };

        TreeItemNode item;
        if (data.Node is not null)
        {
            item = copy ? data.Node.Copy() : data.Node;
            if (!copy)
            {
                IList<TreeItemNode> sourceItems = item.Parent is null
                    ? RootItems
                    : item.Parent.Children;
                var sourceIndex = sourceItems.IndexOf(item);
                if (ReferenceEquals(sourceItems, destinationItems))
                {
                    if (sourceIndex < insertionIndex) insertionIndex--;
                    ((ObservableCollection<TreeItemNode>)sourceItems).Move(sourceIndex, insertionIndex);
                    IsDirty = true;
                    MakeCurrentAndReveal(item);
                    DropStatus = "Tree moved.";
                    RaiseAddCommandsCanExecuteChanged();
                    droppedItem = item;
                    return true;
                }

                if (item.Parent is not null)
                {
                    item.Parent.Children.Remove(item);
                }
                else
                {
                    RootItems.Remove(item);
                }
            }
        }
        else
        {
            item = CreateItem(data.Option!);
        }

        if (destinationParent is null)
        {
            RootItems.Insert(insertionIndex, item);
        }
        else
        {
            destinationParent.Children.Insert(insertionIndex, item);
        }

        SubscribeDocumentDataChanges(item);
        IsDirty = true;
        if (data.Option is not null || (data.Node is not null && !copy))
        {
            MakeCurrentAndReveal(item);
        }

        DropStatus = data.Option is not null
            ? $"Added {item.Name}."
            : copy ? "Tree copied." : "Tree moved.";
        RaiseAddCommandsCanExecuteChanged();
        droppedItem = item;
        return true;
    }

    private void SubscribeDocumentDataChanges(TreeItemNode item)
    {
        if (item is IDocumentItemDetached detached)
        {
            detached.OnDocumentItemReattached();
        }

        if (item is IDocumentDataChangeNotifier notifier && _documentDataNotifiers.Add(notifier))
        {
            notifier.DocumentDataChanged += OnDocumentDataChanged;
        }

        foreach (var child in item.Children)
        {
            SubscribeDocumentDataChanges(child);
        }
    }

    private void UnsubscribeDocumentDataChanges(TreeItemNode item)
    {
        foreach (var child in item.Children)
        {
            UnsubscribeDocumentDataChanges(child);
        }

        if (item is IDocumentDataChangeNotifier notifier && _documentDataNotifiers.Remove(notifier))
        {
            notifier.DocumentDataChanged -= OnDocumentDataChanged;
        }
    }

    private void OnDocumentDataChanged(object? sender, EventArgs eventArgs) => IsDirty = true;

    private static void DetachDocumentItem(TreeItemNode item)
    {
        foreach (var child in item.Children)
        {
            DetachDocumentItem(child);
        }

        if (item is IDocumentItemDetached detached)
        {
            detached.OnDocumentItemDetached();
        }
    }

    /// <summary>Updates the drag-and-drop status text shown by the main window.</summary>
    /// <param name="status">The status text to display.</param>
    public void SetDropStatus(string status) => DropStatus = status;

    private void MakeCurrentAndReveal(TreeItemNode item)
    {
        SelectedTreeItem = item;
        TreeItemRevealRequested?.Invoke(item);
    }

    private TreeItemNode CreateItem(TreeItemOption option)
    {
        var name = $"{option.Name} {_nextItemNumber++}";
        TreeItemNode node;
        node = _creators[option.Kind].Create();

        node.Name = option.Name;
        return node;
    }

    private bool IsInTree(TreeItemNode item)
    {
        while (item.Parent is not null)
        {
            item = item.Parent;
        }

        return RootItems.Contains(item);
    }

    private static bool IsAncestorOf(TreeItemNode possibleAncestor, TreeItemNode? item)
    {
        for (var current = item; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, possibleAncestor))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Sets the application status message and the drag-and-drop status text to the same value.</summary>
    /// <param name="message">The status message to display.</param>
    public void ReportStatus(string message)
    {
        StatusMessage = message;
        DropStatus = message;
    }

}

internal static class RelayCommandExtensions
{
    public static void RaiseCanExecuteChanged(this RelayCommand command) => command.NotifyCanExecuteChanged();

    public static void RaiseCanExecuteChanged<T>(this RelayCommand<T> command) => command.NotifyCanExecuteChanged();
}
