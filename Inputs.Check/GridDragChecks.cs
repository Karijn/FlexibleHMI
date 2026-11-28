using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Containers.UI;
using FlexibleHMI.Interfaces;
using FlexibleHMI.Services;
using FlexibleHMI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

internal static class GridDragChecks
{
    public class Fake<T> : DispatchProxy where T : class
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "get_RecentFiles") return Array.Empty<string>();
            if (method.Name == "GetProperties") return Array.Empty<TreeDocumentProperty>();
            if (method.Name == "Confirm") return true;
            if (method.ReturnType == typeof(void)) return null;
            return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
        }
    }

    private sealed class Node(string kind) : TreeItemNode
    {
        public override string Kind => kind;
        public override int MaxChildren => -1;
        public override string ChildRule => "Children";
        public override TreeItemNode Copy() => Copy(new Node(kind));
    }

    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object? Get(object item, string name) => item.GetType().GetProperty(name)!.GetValue(item);
    private static IList Cells(TreeItemNode grid) => (IList)Get(grid, "Cells")!;
    private static object Cell(TreeItemNode grid, TreeItemNode node) => Cells(grid).Cast<object>().Single(cell => ReferenceEquals(Get(cell, "Node"), node));
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    public static void Run()
    {
        var services = new ServiceCollection();
        var containers = new Containers.UI.AddinRegistrator();
        containers.RegisterAddIn(services);
        var kinds = new[] { "propertymanager", "calculation", "functiongenerator", "communicationManager" };
        foreach (var kind in kinds)
            services.AddKeyedTransient<TreeItemNode>(kind, (_, key) => new Node((string)key!));
        using var provider = services.BuildServiceProvider();
        var creators = kinds.ToDictionary(kind => kind, kind => new TreeItemOption(kind, kind, "Managers", kind, TreeItemOption.CreateDiamondImage(Colors.Red), provider));
        var vm = new MainWindowViewModel(DispatchProxy.Create<IConfirmationService, Fake<IConfirmationService>>(),
            DispatchProxy.Create<IThemeService, Fake<IThemeService>>(), DispatchProxy.Create<ITreeDocumentService, Fake<ITreeDocumentService>>(),
            DispatchProxy.Create<ITreeDocumentProperties, Fake<ITreeDocumentProperties>>(), DispatchProxy.Create<IUserSettingsService, Fake<IUserSettingsService>>(), creators);
        var grid = provider.GetRequiredKeyedService<TreeItemNode>("grid-container");
        var other = provider.GetRequiredKeyedService<TreeItemNode>("grid-container");
        vm.RootItems.Add(grid);
        vm.RootItems.Add(other);
        var child = new Node("child") { Name = "child" };
        vm.RootItems.Add(child);
        var payload = new TreeDragData(child, null);
        Check(vm.DropOnGrid(payload, grid, 0, 0, false) && ReferenceEquals(child.Parent, grid) && !vm.RootItems.Contains(child), "move root into grid");
        var view = new GridContainerView { DataContext = grid, Width = 400, Height = 300 };
        var window = new Window { Content = view, Width = 450, Height = 350, ShowInTaskbar = false };
        window.AddHandler(GridDropRequest.Event, new EventHandler<GridDropRequest>((_, request) =>
        {
            request.Succeeded = vm.DropOnGrid(request.Data, request.Target, request.Row, request.Column, request.Copy);
            request.Handled = true;
        }));
        window.Show();
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        Check(Descendants(view).OfType<Border>().Any(border => Equals(border.ToolTip, "Drag to move; hold Ctrl to copy")), "visible grid child drag handle");
        var cell = Cell(grid, child);
        cell.GetType().GetProperty("RowSpan")!.SetValue(cell, 2);
        var root = Descendants(view).OfType<Grid>().First();
        var panel = Descendants(view).OfType<GridContainerPanel>().Single();
        var position = panel.TranslatePoint(new Point(panel.ActualWidth * 0.75, panel.ActualHeight * 0.25), root);
        var data = new DataObject();
        data.SetData(typeof(TreeDragData), payload);
        var constructor = typeof(DragEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var drop = (DragEventArgs)constructor.Invoke([data, DragDropKeyStates.LeftMouseButton, DragDropEffects.Move | DragDropEffects.Copy, root, position]);
        drop.RoutedEvent = DragDrop.DropEvent;
        root.RaiseEvent(drop);
        Check(drop.Effects == DragDropEffects.Move && (int)Get(cell, "Column")! == 1 && (int)Get(cell, "RowSpan")! == 2, "WPF drop hit-tests grid and preserves span");
        var copyDrop = (DragEventArgs)constructor.Invoke([data, DragDropKeyStates.LeftMouseButton | DragDropKeyStates.ControlKey, DragDropEffects.Move | DragDropEffects.Copy, root, position]);
        copyDrop.RoutedEvent = DragDrop.DropEvent;
        root.RaiseEvent(copyDrop);
        Check(copyDrop.Effects == DragDropEffects.Copy && grid.Children.Count == 2, "Ctrl-drop copies grid child onto occupied cell");
        CheckUnequalDrop(vm, provider);
        window.Close();
        var copy = grid.Children.Single(item => !ReferenceEquals(item, child));
        Check(ReferenceEquals(copy.Parent, grid) && (int)Get(Cell(grid, copy), "RowSpan")! == 2, "copy retains spans and ownership");
        Check(vm.DropOnGrid(payload, grid, 0, 0, false), "move child to free cell");
        Check(vm.DropOnGrid(payload, grid, 1, 1, false), "move onto spanned occupied cell swaps children");
        Check((int)Get(Cell(grid, child), "Row")! == 1 && (int)Get(Cell(grid, child), "Column")! == 1 && (int)Get(Cell(grid, child), "RowSpan")! == 1, "moving span fits destination");
        Check((int)Get(Cell(grid, copy), "Row")! == 0 && (int)Get(Cell(grid, copy), "Column")! == 0 && (int)Get(Cell(grid, copy), "RowSpan")! == 2, "occupant moves to previous origin with span");
        Check(vm.DropOnGrid(payload, grid, 0, 1, false), "move back to free cell");
        cell.GetType().GetProperty("RowSpan")!.SetValue(cell, 2);
        var external = new Node("external");
        vm.RootItems.Add(external);
        Check(!vm.DropOnGrid(new TreeDragData(external, null), grid, 0, 0, false) && vm.RootItems.Contains(external), "occupied external move rejected without mutation");
        Check(vm.DropOnGrid(payload, grid, 0, 0, true), "copy may overlap");
        var overlap = grid.Children.Single(item => !ReferenceEquals(item, copy) && !ReferenceEquals(item, child));
        Check(!vm.DropOnGrid(payload, grid, 0, 0, false) && (int)Get(Cell(grid, child), "Column")! == 1, "ambiguous occupied move rejected");
        Check(vm.DeleteItem(overlap), "remove overlapping copy");
        Check(!vm.DropOnGrid(payload, grid, 4, 0, false) && ReferenceEquals(child.Parent, grid), "invalid cell rejected");
        Check(!vm.CanDrop(new TreeDragData(grid, null), child, DropPosition.Child, false), "ancestor cannot move into descendant");
        Check(vm.Drop(new TreeDragData(child, null), copy, DropPosition.After, false), "reorder grid child in tree");
        Check((int)Get(Cell(grid, child), "Column")! == 1 && (int)Get(Cell(grid, child), "RowSpan")! == 2, "tree reordering preserves placement");
        vm.SelectedTreeItem = child;
        vm.CopyItemCommand.Execute(null);
        vm.SelectedTreeItem = grid;
        vm.PasteItemCommand.Execute(null);
        Check(grid.Children.Count == 3 && grid.Children.All(item => ReferenceEquals(item.Parent, grid)), "tree copy and paste a grid child");
        var pasted = grid.Children.Single(item => !ReferenceEquals(item, copy) && !ReferenceEquals(item, child));
        Check(vm.DeleteItem(pasted), "delete pasted child");
        Check(vm.DropOnGrid(payload, other, 1, 0, false) && ReferenceEquals(child.Parent, other) && (int)Get(Cell(other, child), "RowSpan")! == 1, "move between grids fits spans");
        Check(vm.Drop(new TreeDragData(child, null), null, DropPosition.Child, false) && child.Parent is null && vm.RootItems.Contains(child), "move child back to tree root");
        Check(vm.DeleteItem(copy) && copy.Parent is null && grid.Children.Count == 0, "delete child through host");
        Check(!vm.DeleteItem(copy), "deleted child cannot be deleted twice");
    }

    private static void CheckUnequalDrop(MainWindowViewModel vm, IServiceProvider provider)
    {
        var grid = provider.GetRequiredKeyedService<TreeItemNode>("grid-container");
        grid.GetType().GetProperty("EqualColumnWidths")!.SetValue(grid, false);
        grid.GetType().GetProperty("EqualRowHeights")!.SetValue(grid, false);
        vm.RootItems.Add(grid);
        var first = new Node("first") { Name = "first" };
        var second = new Node("second") { Name = "second" };
        vm.RootItems.Add(first);
        vm.RootItems.Add(second);
        Check(vm.DropOnGrid(new TreeDragData(first, null), grid, 0, 0, false) && vm.DropOnGrid(new TreeDragData(second, null), grid, 1, 1, false), "prepare unequal grid children");
        var view = new GridContainerView { DataContext = grid, Width = 400, Height = 300 };
        var window = new Window { Content = view, Width = 450, Height = 350, ShowInTaskbar = false };
        window.AddHandler(GridDropRequest.Event, new EventHandler<GridDropRequest>((_, request) =>
        {
            request.Succeeded = vm.DropOnGrid(request.Data, request.Target, request.Row, request.Column, request.Copy);
            request.Handled = true;
        }));
        window.Show();
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        var panel = Descendants(view).OfType<GridContainerPanel>().Single();
        var presenters = Descendants(panel).OfType<ContentPresenter>().Where(item => Cells(grid).Contains(item.DataContext)).ToArray();
        var firstPresenter = presenters.Single(item => ReferenceEquals(item.DataContext, Cell(grid, first)));
        var secondPresenter = presenters.Single(item => ReferenceEquals(item.DataContext, Cell(grid, second)));
        firstPresenter.Width = 50;
        firstPresenter.Height = 30;
        secondPresenter.Width = 200;
        secondPresenter.Height = 120;
        window.UpdateLayout();
        var x = panel.ColumnDefinitions[0].ActualWidth + 1;
        var y = panel.RowDefinitions[0].ActualHeight + 1;
        Check(x < panel.ActualWidth / 2 && y < panel.ActualHeight / 2, "unequal tracks expose a nonuniform target");
        var root = Descendants(view).OfType<Grid>().First();
        var data = new DataObject();
        data.SetData(typeof(TreeDragData), new TreeDragData(first, null));
        var constructor = typeof(DragEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var position = panel.TranslatePoint(new Point(x, y), root);
        var drop = (DragEventArgs)constructor.Invoke([data, DragDropKeyStates.LeftMouseButton, DragDropEffects.Move, root, position]);
        drop.RoutedEvent = DragDrop.DropEvent;
        root.RaiseEvent(drop);
        Check(drop.Effects == DragDropEffects.Move && (int)Get(Cell(grid, first), "Row")! == 1 && (int)Get(Cell(grid, first), "Column")! == 1 && (int)Get(Cell(grid, second), "Row")! == 0 && (int)Get(Cell(grid, second), "Column")! == 0, "unequal-track drop swaps the correct children");
        window.UpdateLayout();
        position = panel.TranslatePoint(new Point(panel.ActualWidth - 1, panel.ActualHeight - 1), root);
        var blankDrop = (DragEventArgs)constructor.Invoke([data, DragDropKeyStates.LeftMouseButton, DragDropEffects.Move, root, position]);
        blankDrop.RoutedEvent = DragDrop.DropEvent;
        root.RaiseEvent(blankDrop);
        Check(blankDrop.Effects == DragDropEffects.None && (int)Get(Cell(grid, first), "Row")! == 1 && (int)Get(Cell(grid, first), "Column")! == 1, "drop in unused space is rejected without moving the child");
        window.Close();
    }
}
