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
using ParameterRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand<object?>;

namespace FlexibleHMI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfirmationService _confirmationService;
    private readonly IThemeService _themeService;
    private readonly ITreeDocumentService _treeDocumentService;
    private readonly ITreeDocumentProperties _treeDocumentProperties;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IDictionary<string, TreeItemOption> _creators;
    private TreeItemOption? _selectedItemOption;
    private TreeItemNode? _selectedTreeItem;
    private TreeItemNode? _clipboardItem;
    private bool _clipboardIsCopy;
    private string _dropStatus = "Ready.";
    private string? _currentFilePath;
    private string _statusMessage = "Ready.";
    private bool _isDirty;
    private int _nextItemNumber = 1;

    public double RightPanelWidth
    {
        get => _userSettingsService.RightPanelWidth;
        set
        {
            _userSettingsService.RightPanelWidth = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private string _statusText;

    public double LeftPanelWidth
    {
        get => _userSettingsService.LeftPanelWidth;
        set
        {
            _userSettingsService.LeftPanelWidth = value;
            OnPropertyChanged();
        }
    }


    public bool IsLeftExpanded
    {
        get => _userSettingsService.IsLeftPanelExpanded;
        set
        {
            _userSettingsService.IsLeftPanelExpanded = value;
            OnPropertyChanged();
        }
    }

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
        RecentFiles = new ObservableCollection<string>(_userSettingsService.RecentFiles);

        //var creators = _serviceProvider.GetRequiredService<IDictionary<string, TreeItemOption>>();


        AvailableItems = new ObservableCollection<TreeItemOption>(creators.Values);
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
        AboutCommand = new RelayCommand(() => AboutRequested?.Invoke());
    }

    public event Action<TreeItemNode>? TreeItemRevealRequested;

    public event Action? AboutRequested;

    public string WindowTitle => IsDirty ? "*Flexible HMI" : "Flexible HMI";

    public ObservableCollection<TreeItemOption> AvailableItems { get; }

    public ObservableCollection<TreeItemNode> RootItems { get; } = new();

    public ObservableCollection<string> RecentFiles { get; }

    public ICommand NewDocumentCommand { get; }

    public ICommand OpenRecentFileCommand { get; }

    public ICommand AddItemCommand { get; }

    public ICommand AddBeforeItemCommand { get; }

    public ICommand AddAfterItemCommand { get; }

    public ICommand CutItemCommand { get; }

    public ICommand CopyItemCommand { get; }

    public ICommand PasteItemCommand { get; }

    public ICommand PasteBeforeItemCommand { get; }

    public ICommand PasteAfterItemCommand { get; }

    public ICommand DeleteItemCommand { get; }

    public ICommand MoveUpItemCommand { get; }

    public ICommand MoveDownItemCommand { get; }

    public ICommand MoveToParentCommand { get; }

    public ICommand MoveToChildCommand { get; }

    public ICommand MoveToFirstChildCommand { get; }

    public ICommand MoveToLastChildCommand { get; }

    public ICommand ExpandSelectedNodeCommand { get; }

    public ICommand CollapseSelectedNodeCommand { get; }

    public ICommand ExpandAllNodesCommand { get; }

    public ICommand CollapseAllNodesCommand { get; }

    public ICommand UseLightThemeCommand { get; }

    public ICommand UseDarkThemeCommand { get; }

    public ICommand OpenDocumentCommand { get; }

    public ICommand SaveDocumentCommand { get; }

    public ICommand SaveDocumentAsCommand { get; }

    public ICommand AboutCommand { get; }

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

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (_isDirty == value)
            {
                return;
            }

            _isDirty = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

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

    public bool IsLightTheme => _themeService.CurrentTheme == AppTheme.Light;

    public bool IsDarkTheme => _themeService.CurrentTheme == AppTheme.Dark;

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
        if ((RootItems.Count > 0 || _treeDocumentProperties.GetProperties().Count > 0) && !_confirmationService.Confirm(
                "Discard the current tree and properties and create a new document? This cannot be undone.",
                "New document"))
        {
            return;
        }

        _treeDocumentProperties.LoadProperties([]);
        RootItems.Clear();
        SelectedTreeItem = null;
        _clipboardItem = null;
        _currentFilePath = null;
        OnPropertyChanged(nameof(CurrentFilePath));
        IsDirty = false;
        StatusMessage = "New document.";
        DropStatus = StatusMessage;
        RaisePasteCommandsCanExecuteChanged();
    }

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
        try
        {
            var document = filePath is null
                ? _treeDocumentService.Load()
                : _treeDocumentService.Load(filePath);
            if (document is null)
            {
                return;
            }

            if ((RootItems.Count > 0 || _treeDocumentProperties.GetProperties().Count > 0) && !_confirmationService.Confirm(
                    "Replace the current tree and properties with the opened document? This cannot be undone.",
                    "Open document"))
            {
                return;
            }

            _treeDocumentProperties.LoadProperties(document.Properties);
            RootItems.Clear();
            foreach (var item in document.Items)
            {
                RootItems.Add(item);
            }

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
    }

    private void OnDocumentPropertiesChanged(object? sender, EventArgs e)
    {
        IsDirty = true;
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

    private bool SaveDocument(bool saveAs)
    {
        try
        {
            var path = _treeDocumentService.Save(RootItems, _currentFilePath, saveAs);
            if (path is null)
            {
                return false;
            }

            CurrentFilePath = path;
            IsDirty = false;
            AddRecentFile(path);
            SaveLastDocumentPath(path);
            StatusMessage = $"Saved {Path.GetFileName(path)}.";
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

    public bool ConfirmClose()
    {
        if (!IsDirty)
        {
            return true;
        }

        return _confirmationService.ConfirmSaveChanges() switch
        {
            UnsavedChangesDecision.Save => SaveDocument(false),
            UnsavedChangesDecision.Discard => true,
            _ => false
        };
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
        SelectedTreeItem is not null && GetSiblings(SelectedTreeItem).IndexOf(SelectedTreeItem) > 0;

    private bool CanMoveSelectedDown()
    {
        if (SelectedTreeItem is null)
        {
            return false;
        }

        var siblings = GetSiblings(SelectedTreeItem);
        var index = siblings.IndexOf(SelectedTreeItem);
        return index >= 0 && index < siblings.Count - 1;
    }

    private bool CanMoveSelectedToFirstChild() =>
        SelectedTreeItem is { } item && GetSiblings(item).IndexOf(item) > 0;

    private bool CanMoveSelectedToLastChild()
    {
        if (SelectedTreeItem is not { } item)
        {
            return false;
        }

        var siblings = GetSiblings(item);
        var index = siblings.IndexOf(item);
        return index >= 0 && index < siblings.Count - 1;
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
        var destinationIndex = first ? 0 : siblings.Count - 1;
        if (currentIndex < 0 || currentIndex == destinationIndex)
        {
            return;
        }

        siblings.RemoveAt(currentIndex);
        siblings.Insert(destinationIndex, item);
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
        var index = siblings.IndexOf(item);
        return index > 0 && siblings[index - 1].CanAcceptChildren;
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
        var destinationIndex = index + offset;
        if (index < 0 || destinationIndex < 0 || destinationIndex >= siblings.Count)
        {
            return;
        }

        siblings.RemoveAt(index);
        siblings.Insert(destinationIndex, item);
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
        var index = siblings.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        var newParent = siblings[index - 1];
        if (!newParent.CanAcceptChildren)
        {
            return;
        }

        siblings.RemoveAt(index);
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

    public bool DeleteItem(TreeItemNode item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!IsInTree(item) || !_confirmationService.Confirm(
                $"Delete '{item.Name}' and all of its descendants? This cannot be undone.",
                "Confirm deletion"))
        {
            return false;
        }

        var selectedItemIsBeingDeleted = SelectedTreeItem is { } selected &&
                                         (ReferenceEquals(selected, item) || IsAncestorOf(item, selected));
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

    public bool CanDrop(TreeDragData data, TreeItemNode? target, DropPosition position, bool copy)
    {
        var sourceNode = data.Node;
        if (sourceNode is null && data.Option is null)
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

    public bool Drop(TreeDragData data, TreeItemNode? target, DropPosition position, bool copy)
    {
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
                if (ReferenceEquals(sourceItems, destinationItems) && sourceIndex < insertionIndex)
                {
                    insertionIndex--;
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

        IsDirty = true;
        if (data.Option is not null || (data.Node is not null && !copy))
        {
            MakeCurrentAndReveal(item);
        }

        DropStatus = data.Option is not null
            ? $"Added {item.Name}."
            : copy ? "Tree copied." : "Tree moved.";
        RaiseAddCommandsCanExecuteChanged();
        return true;
    }

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
