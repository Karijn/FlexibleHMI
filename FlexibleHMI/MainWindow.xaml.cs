using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FlexibleHMI;

public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    private TreeDragData? _dragData;
    private AdornerLayer? _dropAdornerLayer;
    private DropTargetAdorner? _dropAdorner;
    private UIElement? _dropAdornedElement;
    private DropPosition _dropPosition;
    private readonly IUserSettingsService _userSettingsService;

    private MainWindowViewModel ViewModel { get => (MainWindowViewModel)DataContext; }

    public MainWindow(MainWindowViewModel viewModel, IUserSettingsService userSettingsService)
    {
        InitializeComponent();
        _userSettingsService = userSettingsService;
        Width = Math.Clamp(userSettingsService.WindowWidth, MinWidth, Math.Max(MinWidth, SystemParameters.WorkArea.Width));
        Height = Math.Clamp(userSettingsService.WindowHeight, MinHeight, Math.Max(MinHeight, SystemParameters.WorkArea.Height));
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.TreeItemRevealRequested += RevealTreeItem;
        viewModel.AboutRequested += ShowAboutDialog;
    }

    private void ShowAboutDialog()
    {
        var dialog = new AboutWindow { Owner = this };
        dialog.ShowDialog();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedTreeItem) &&
            sender is MainWindowViewModel { SelectedTreeItem: { } selectedItem })
        {
            Dispatcher.BeginInvoke(
                new Action(() => RevealTreeItem(selectedItem)),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void RevealTreeItem(TreeItemNode item)
    {
        var ancestors = new Stack<TreeItemNode>();
        for (var current = item; current is not null; current = current.Parent)
        {
            ancestors.Push(current);
        }

        ItemsControl itemsControl = ItemTree;
        TreeViewItem? container = null;
        while (ancestors.Count > 0)
        {
            var current = ancestors.Pop();
            container = itemsControl.ItemContainerGenerator.ContainerFromItem(current) as TreeViewItem;
            if (container is null)
            {
                return;
            }

            if (ancestors.Count > 0)
            {
                current.IsExpanded = true;
                container.UpdateLayout();
                itemsControl = container;
            }
        }

        container?.BringIntoView();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel closingViewModel && !closingViewModel.ConfirmClose())
        {
            e.Cancel = true;
            return;
        }

        var width = WindowState == WindowState.Normal ? ActualWidth : RestoreBounds.Width;
        var height = WindowState == WindowState.Normal ? ActualHeight : RestoreBounds.Height;

        try
        {
            _userSettingsService.SaveWindowSize(width, height);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            e.Cancel = true;
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ReportStatus($"Could not save window size: {exception.Message}");
            }
        }
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedTreeItem = e.NewValue as TreeItemNode;
        }
    }

    private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        _dragStartPoint = e.GetPosition(this);
        _dragData = null;

        if (sender is ListBox)
        {
            var item = FindAncestor<ListBoxItem>(source);
            if (item?.DataContext is TreeItemOption option)
            {
                _dragData = new TreeDragData(null, option);
            }
        }
        else
        {
            var item = FindAncestor<TreeViewItem>(source);
            if (item?.DataContext is TreeItemNode node)
            {
                _dragData = new TreeDragData(node, null);
            }
        }
    }

    private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragData is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject();
        data.SetData(typeof(TreeDragData), _dragData);
        var allowedEffects = _dragData.Option is not null
            ? DragDropEffects.Copy
            : DragDropEffects.Move | DragDropEffects.Copy;
        var effect = DragDrop.DoDragDrop((DependencyObject)sender, data, allowedEffects);

        ClearDropIndicator();
        if (effect == DragDropEffects.None && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetDropStatus("Drag canceled or dropped in an invalid location.");
        }

        _dragData = null;
    }

    private void ItemTree_DragOver(object sender, DragEventArgs e)
    {
        var data = GetDragData(e.Data);
        if (data is null || DataContext is not MainWindowViewModel viewModel)
        {
            e.Effects = DragDropEffects.None;
            ClearDropIndicator();
            e.Handled = true;
            return;
        }

        var targetContainer = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
        var target = targetContainer?.DataContext as TreeItemNode;
        var header = targetContainer is null ? null : GetHeaderElement(targetContainer);
        var position = header is null
            ? DropPosition.Child
            : GetDropPosition(e.GetPosition(header).Y, header.ActualHeight);
        var copy = data.Option is not null || Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

        if (!viewModel.CanDrop(data, target, position, copy))
        {
            e.Effects = DragDropEffects.None;
            viewModel.SetDropStatus("Drop is not allowed at this position.");
            ClearDropIndicator();
            e.Handled = true;
            return;
        }

        e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
        var action = copy ? "Copy" : "Move";
        var placement = target is null
            ? "to the end of the root"
            : position switch
            {
                DropPosition.Before => $"before {target.Name}",
                DropPosition.After => $"after {target.Name}",
                _ => $"as a child of {target.Name}"
            };
        viewModel.SetDropStatus($"{action} {placement}.");
        ShowDropIndicator(header ?? ItemTree, position);
        e.Handled = true;
    }

    private void ItemTree_DragLeave(object sender, DragEventArgs e)
    {
        if (!ItemTree.IsMouseOver)
        {
            ClearDropIndicator();
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SetDropStatus("Drag an item onto the tree to add it.");
            }
        }
    }

    private void ItemTree_Drop(object sender, DragEventArgs e)
    {
        var data = GetDragData(e.Data);
        if (data is not null && DataContext is MainWindowViewModel viewModel)
        {
            var targetContainer = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
            var target = targetContainer?.DataContext as TreeItemNode;
            var header = targetContainer is null ? null : GetHeaderElement(targetContainer);
            var position = header is null
                ? DropPosition.Child
                : GetDropPosition(e.GetPosition(header).Y, header.ActualHeight);
            var copy = data.Option is not null || Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (viewModel.Drop(data, target, position, copy))
            {
                e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                viewModel.SetDropStatus("Drop is not allowed at this position.");
            }
        }

        ClearDropIndicator();
        e.Handled = true;
    }

    private void DeleteBin_DragOver(object sender, DragEventArgs e)
    {
        var data = GetDragData(e.Data);
        if (data?.Node is null || Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ||
            DataContext is not MainWindowViewModel viewModel)
        {
            e.Effects = DragDropEffects.None;
            DeleteBin.Background = (Brush)FindResource("DeleteBinBrush");
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        DeleteBin.Background = (Brush)FindResource("DeleteBinHighlightBrush");
        viewModel.SetDropStatus($"Drop to delete {data.Node.Name} and all its descendants.");
        e.Handled = true;
    }

    private void DeleteBin_DragLeave(object sender, DragEventArgs e)
    {
        DeleteBin.Background = (Brush)FindResource("DeleteBinBrush");
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetDropStatus("Drag an item onto the tree to add it.");
        }
    }

    private void DeleteBin_Drop(object sender, DragEventArgs e)
    {
        var data = GetDragData(e.Data);
        if (data?.Node is not null && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            DataContext is MainWindowViewModel viewModel && viewModel.DeleteItem(data.Node))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        DeleteBin.Background = (Brush)FindResource("DeleteBinBrush");
        e.Handled = true;
    }

    private void ShowDropIndicator(UIElement element, DropPosition position)
    {
        if (ReferenceEquals(_dropAdornedElement, element) && _dropPosition == position)
        {
            return;
        }

        ClearDropIndicator();
        var layer = AdornerLayer.GetAdornerLayer(element);
        if (layer is null)
        {
            return;
        }

        _dropAdornedElement = element;
        _dropPosition = position;
        _dropAdornerLayer = layer;
        _dropAdorner = new DropTargetAdorner(element, position);
        layer.Add(_dropAdorner);
    }

    private void ClearDropIndicator()
    {
        if (_dropAdorner is not null)
        {
            _dropAdornerLayer?.Remove(_dropAdorner);
        }

        _dropAdorner = null;
        _dropAdornerLayer = null;
        _dropAdornedElement = null;
    }

    private static TreeDragData? GetDragData(IDataObject data) =>
        data.GetDataPresent(typeof(TreeDragData))
            ? data.GetData(typeof(TreeDragData)) as TreeDragData
            : null;

    private static DropPosition GetDropPosition(double y, double height)
    {
        if (y < height * 0.25)
        {
            return DropPosition.Before;
        }

        return y > height * 0.75 ? DropPosition.After : DropPosition.Child;
    }

    private static FrameworkElement? GetHeaderElement(TreeViewItem item) =>
        item.Template.FindName("PART_Header", item) as FrameworkElement;

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = current switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(current),
                FrameworkContentElement contentElement => contentElement.Parent,
                _ => LogicalTreeHelper.GetParent(current)
            };
        }

        return null;
    }

    private void LeftPanel_Expanded(object sender, RoutedEventArgs e)
    {
        LeftPanelColumn.Width = new GridLength(ViewModel.LeftPanelWidth);
        ViewModel.StatusText = "Left panel opened";
    }
    
    private void LeftPanelSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ViewModel.LeftPanelWidth = LeftPanelColumn.ActualWidth;
        ViewModel.StatusText = $"Left panel: {Math.Round(ViewModel.LeftPanelWidth):0} px";
    }

    private void LeftPanelSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        ViewModel.LeftPanelWidth = LeftPanelColumn.ActualWidth;
        ViewModel.StatusText = $"Left panel: {Math.Round(ViewModel.LeftPanelWidth):0} px";
    }

    private void LeftPanel_Expander_MouseLeave(object sender, MouseEventArgs e)
    {

    }

    private void LeftPanel_Collapsed(object sender, RoutedEventArgs e)
    {
        ViewModel.LeftPanelWidth = LeftPanelColumn.ActualWidth;
        LeftPanelColumn.Width = GridLength.Auto;
        ViewModel.StatusText = "Left panel closed";
    }

    private void LeftPanel_MouseEnter(object sender, MouseEventArgs e)
    {
        ViewModel.StatusText = ViewModel.IsLeftExpanded
            ? "Close left panel"
            : "Open left panel";

    }

    private void RightPanelSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ViewModel.RightPanelWidth = RightPanelColumn.ActualWidth;
        ViewModel.StatusText = $"Right panel: {Math.Round(ViewModel.RightPanelWidth):0} px";
    }

    private void RightPanelSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        ViewModel.RightPanelWidth = RightPanelColumn.ActualWidth;
        ViewModel.StatusText = $"Right panel: {Math.Round(ViewModel.RightPanelWidth):0} px";
    }

    private void RightPanel_Collapsed(object sender, RoutedEventArgs e)
    {
        ViewModel.RightPanelWidth = RightPanelColumn.ActualWidth;
        RightPanelColumn.Width = GridLength.Auto;
        ViewModel.StatusText = "Right panel closed";
    }

    private void RightPanel_Expanded(object sender, RoutedEventArgs e)
    {
        RightPanelColumn.Width = new GridLength(ViewModel.RightPanelWidth);
        ViewModel.StatusText = "Right panel opened";
    }

    private void RightPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ViewModel.StatusText = ViewModel.IsRightExpanded
            ? "Close right panel"
            : "Open right panel";
    }


    private void RightPanel_Expander_MouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel.StatusText = "Ready";
    }
}
