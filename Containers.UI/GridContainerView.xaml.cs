using FlexibleHMI.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Containers.UI;

/// <summary>Displays the controls of a grid container.</summary>
public partial class GridContainerView : UserControl
{
    private Point _dragStart;
    private TreeItemNode? _draggedNode;

    /// <summary>Creates the grid container view.</summary>
    public GridContainerView() => InitializeComponent();

    private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _draggedNode = (sender as FrameworkElement)?.DataContext is GridCellViewModel cell ? cell.Node : null;
    }

    private void Handle_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedNode is null || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var data = new DataObject();
        data.SetData(typeof(TreeDragData), new TreeDragData(_draggedNode, null));
        _draggedNode = null;
        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TreeDragData))) return;
        var data = (TreeDragData)e.Data.GetData(typeof(TreeDragData))!;
        e.Effects = data.Option is not null || e.KeyStates.HasFlag(DragDropKeyStates.ControlKey)
            ? DragDropEffects.Copy : DragDropEffects.Move;
        e.Handled = true;
    }

    private void Grid_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TreeDragData)) || DataContext is not GridContainerViewModel model) return;
        var panel = FindPanel(Cells);
        if (panel is null || !panel.TryGetCellAt(e.GetPosition(panel), out var row, out var column) || !model.ContainsCell(row, column))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }
        var data = (TreeDragData)e.Data.GetData(typeof(TreeDragData))!;
        var copy = data.Option is not null || e.KeyStates.HasFlag(DragDropKeyStates.ControlKey);
        var request = new GridDropRequest(data, model, row, column, copy);
        RaiseEvent(request);
        e.Effects = request.Succeeded ? (copy ? DragDropEffects.Copy : DragDropEffects.Move) : DragDropEffects.None;
        e.Handled = true;
    }

    private static GridContainerPanel? FindPanel(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is GridContainerPanel panel) return panel;
            if (FindPanel(child) is { } result) return result;
        }
        return null;
    }
}
