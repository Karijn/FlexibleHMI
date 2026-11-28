using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Containers.UI;
using FlexibleHMI.Interfaces;
using Inputs.UI;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager;
using PropertyManager.Interfaces;

internal static class ContainerChecks
{
    private sealed class TestNode : TreeItemNode
    {
        public TestNode(string name) => Name = name;
        public override string Kind => "test-node";
        public override int MaxChildren => 0;
        public override string ChildRule => "None";
        public override TreeItemNode Copy() => Copy(new TestNode(Name));
    }

    static object? Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o);
    static void Set(object o, string name, object value)
    {
        try { o.GetType().GetProperty(name)!.SetValue(o, value); }
        catch (TargetInvocationException exception) { throw exception.InnerException!; }
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject o)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
        {
            var child = VisualTreeHelper.GetChild(o, i);
            yield return child;
            foreach (var item in Descendants(child)) yield return item;
        }
    }
    static List<object> Cells(object model) => ((System.Collections.IEnumerable)Get(model, "Cells")!).Cast<object>().ToList();
    static int[] Place(object cell) => new[] { "Row", "Column", "RowSpan", "ColumnSpan" }.Select(name => (int)Get(cell, name)!).ToArray();
    static string Layout(object model) => string.Join(" ", Cells(model).Select(cell => string.Join(",", Place(cell))));
    static bool Throws(Action action) { try { action(); return false; } catch (InvalidOperationException) { return true; } }

    public static void CheckGrid()
    {
        CheckTrackSizing();
        var services = new ServiceCollection();
        var resources = new ResourceDictionary { ["DefaultDetailsTemplate"] = new DataTemplate() };
        services.AddSingleton(resources);
        var registrator = new Containers.UI.AddinRegistrator();
        registrator.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        registrator.InitializeAddIn(provider, new DataTemplateManager(resources));

        var model = provider.GetRequiredKeyedService<TreeItemNode>("grid-container");
        Check(provider.GetRequiredKeyedService<TreeItemOption>("grid-container").Group == "Containers", "grid container creation registered");
        var registrations = provider.GetServices<ViewTemplateRegistration>().Where(item => item.ViewModelType == model.GetType()).ToArray();
        Check(registrations.Length == 2 && registrations.Any(item => item.View == ViewKind.MainView && item.Template.LoadContent() is GridContainerDetails), "grid details registration");
        Check(Application.Current.Resources.Contains(new DataTemplateKey(model.GetType())), "grid implicit main template registration");
        Check(model.MaxChildren == -1 && model.CanAcceptChildren, "grid accepts any number of children");

        Check((int)Get(model, "RowCount")! == 2 && (int)Get(model, "ColumnCount")! == 2, "default 2x2 grid");
        int dirty = 0;
        ((IDocumentDataChangeNotifier)model).DocumentDataChanged += (_, _) => dirty++;
        foreach (var name in new[] { "A", "B", "C", "D", "E" }) model.Children.Add(new TestNode(name));
        Check(model.Children.All(child => ReferenceEquals(child.Parent, model)), "grid children have parents");
        Check(Throws(() => model.Children.Add(model)), "tree rejects cycles");
        Check(Throws(() => model.Children.Add(model.Children[0])), "tree rejects duplicate ownership");
        Check(Layout(model) == "0,0,1,1 0,1,1,1 1,0,1,1 1,1,1,1 2,0,1,1", "children fill free cells and grow rows when full");
        Check((int)Get(model, "RowCount")! == 3, "grid grew by one row");

        var view = new GridContainerView { DataContext = model };
        var details = new GridContainerDetails { DataContext = model };
        var panel = new StackPanel();
        panel.Children.Add(details);
        panel.Children.Add(view);
        var window = new Window { Width = 600, Height = 800, Content = panel, ShowInTaskbar = false };
        window.Show();
        Pump();
        CheckComboBoxLayout(panel, provider);
        CheckLiveEqualRows(panel);

        var grid = Descendants(view).OfType<GridContainerPanel>().Single();
        Check(grid.RowDefinitions.Count == 3 && grid.ColumnDefinitions.Count == 2, "view grid has configured rows and columns");
        var root = Descendants(view).OfType<Grid>().First();
        var width = (TextBox)details.FindName("WidthEditor");
        var height = (TextBox)details.FindName("HeightEditor");
        var grabber = (CheckBox)details.FindName("ShowGrabberEditor");
        var equalColumns = (CheckBox)details.FindName("EqualColumnWidthsEditor");
        var equalRows = (CheckBox)details.FindName("EqualRowHeightsEditor");
        Check(double.IsNaN(root.Width) && double.IsNaN(root.Height) && grabber.IsChecked == true, "automatic sizing and visible grabbers default");
        Check(equalColumns.IsChecked == true && equalRows.IsChecked == true && grid.EqualColumnWidths && grid.EqualRowHeights, "equal track sizing defaults on");
        dirty = 0;
        equalColumns.IsChecked = false;
        Pump();
        Check(!(bool)Get(model, "EqualColumnWidths")! && !grid.EqualColumnWidths && grid.EqualRowHeights && dirty == 1, "column sizing editor independently updates view and marks document dirty");
        Set(model, "EqualRowHeights", false);
        Pump();
        Check(equalRows.IsChecked == false && !grid.EqualRowHeights && dirty == 2, "row sizing model updates editor and view");
        equalRows.IsChecked = true;
        Pump();
        Check(grid.EqualRowHeights && dirty == 3, "row sizing can be restored independently");
        width.Text = "45";
        width.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        height.Text = "120";
        height.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        grabber.IsChecked = false;
        Pump();
        Check(root.Width == 45 && root.ActualWidth == 45 && root.Height == 120 && root.ActualHeight == 120, "fixed dimensions apply even below automatic minimum");
        var handles = Descendants(view).OfType<Border>().Where(item => Equals(item.ToolTip, "Drag to move; hold Ctrl to copy")).ToArray();
        Check(handles.Length == 5 && handles.All(item => item.Visibility == Visibility.Collapsed), "grabbers hidden");
        grabber.IsChecked = true;
        width.Text = "0";
        width.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Pump();
        Check(double.IsNaN(root.Width) && root.MinWidth == 80 && handles.All(item => item.Visibility == Visibility.Visible), "automatic width and grabbers restored");
        grabber.IsChecked = false;
        width.Text = "45";
        width.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        width.Text = "-1";
        width.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(Validation.GetHasError(width) && (double)Get(model, "Width")! == 45, "invalid width rejected");
        width.Text = "45";
        width.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        var showGrid = (CheckBox)details.FindName("ShowGridEditor");
        Check(showGrid.IsChecked == false && !grid.ShowGridLines && !(bool)Get(model, "ShowGrid")!, "grid lines are hidden by default");
        var visualCount = VisualTreeHelper.GetChildrenCount(grid);
        dirty = 0;
        showGrid.IsChecked = true;
        Pump();
        Check((bool)Get(model, "ShowGrid")! && grid.ShowGridLines && dirty == 1, "show grid editor updates view and marks document dirty");
        Check(VisualTreeHelper.GetChildrenCount(grid) == visualCount + 1, "grid line renderer is added when enabled");
        Set(model, "ShowGrid", false);
        Pump();
        Check(showGrid.IsChecked == false && !grid.ShowGridLines && dirty == 2, "model updates hide grid lines and update editor");
        Check(VisualTreeHelper.GetChildrenCount(grid) == visualCount, "grid line renderer is removed when disabled");
        showGrid.IsChecked = true;
        Pump();
        ContentPresenter Presenter(int index) => Descendants(grid).OfType<ContentPresenter>().First(item => ReferenceEquals(item.DataContext, Cells(model)[index]));
        Check(Grid.GetRow(Presenter(4)) == 2 && Grid.GetColumn(Presenter(1)) == 1, "views are placed at row and column");

        var first = Cells(model)[0];
        dirty = 0;
        Set(first, "RowSpan", 2);
        Set(first, "ColumnSpan", 2);
        Pump();
        Check(Grid.GetRowSpan(Presenter(0)) == 2 && Grid.GetColumnSpan(Presenter(0)) == 2 && dirty == 2, "spans apply to view and dirty the document");
        Set(first, "Column", 1);
        Check(Place(first).SequenceEqual([0, 1, 2, 1]), "moving a control fits its span to the grid");
        Set(first, "Row", 1);
        Check(Place(first).SequenceEqual([1, 1, 2, 1]), "moving a row keeps a span that still fits");
        Set(first, "Row", 2);
        Pump();
        Check(Place(first).SequenceEqual([2, 1, 1, 1]) && Grid.GetRow(Presenter(0)) == 2, "moving to the last row shrinks the row span");

        Check(Throws(() => Set(first, "Row", 3)) && Throws(() => Set(first, "Column", -1)) && Throws(() => Set(first, "RowSpan", 0)) && Throws(() => Set(first, "ColumnSpan", 2)), "invalid placements are rejected");
        Check(Place(first).SequenceEqual([2, 1, 1, 1]), "rejected placement keeps previous value");
        Check(Throws(() => Set(model, "RowCount", 2)) && Throws(() => Set(model, "ColumnCount", 0)) && Throws(() => Set(model, "RowCount", 101)), "invalid grid sizes are rejected");

        var rowEditor = Descendants(details).OfType<TextBox>().Where(item => item.Name == "RowEditor").First();
        rowEditor.Text = "9";
        rowEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(Validation.GetHasError(rowEditor), "details editor reports invalid row");
        rowEditor.Text = "0";
        rowEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(!Validation.GetHasError(rowEditor) && (int)Get(Cells(model)[0], "Row")! == 0, "details editor updates row");
        Check(Descendants(details).OfType<TextBox>().Count(item => item.Name == "RowSpanEditor") == 5, "details list every control");

        Set(model, "ColumnCount", 4);
        Set(model, "RowCount", 4);
        Pump();
        Check(grid.RowDefinitions.Count == 4 && grid.ColumnDefinitions.Count == 4, "view follows grid size");
        Check(grid.ColumnDefinitions.All(definition => definition.Width.IsAuto) && grid.EqualRowHeights, "new tracks preserve independent sizing modes");

        var saved = ((ITreeItemDocumentData)model).SaveDocumentData();
        Check(saved.GetProperty("ShowGrid").GetBoolean() && saved.GetProperty("Width").GetDouble() == 45 && saved.GetProperty("Height").GetDouble() == 120 && !saved.GetProperty("ShowGrabber").GetBoolean(), "grid visibility and dimensions are saved");
        Check(!saved.GetProperty("EqualColumnWidths").GetBoolean() && saved.GetProperty("EqualRowHeights").GetBoolean(), "independent sizing modes are saved");
        var copy = ((TreeItemNode)model).Copy();
        Check(copy.Children.Count == 5 && Layout(copy) == Layout(model) && (int)Get(copy, "RowCount")! == 4 && (bool)Get(copy, "ShowGrid")! && (double)Get(copy, "Width")! == 45 && !(bool)Get(copy, "ShowGrabber")!, "copy keeps size, placements and grid visibility");
        Check(!(bool)Get(copy, "EqualColumnWidths")! && (bool)Get(copy, "EqualRowHeights")!, "copy keeps independent sizing modes");

        var loaded = provider.GetRequiredKeyedService<TreeItemNode>("grid-container");
        ((ITreeItemDocumentData)loaded).LoadDocumentData(saved);
        foreach (var name in new[] { "A", "B", "C", "D", "E" }) loaded.Children.Add(new TestNode(name));
        Check(Layout(loaded) == Layout(model), "placements restore when children attach after load");
        Check((bool)Get(loaded, "ShowGrid")! && ((ITreeItemDocumentData)loaded).SaveDocumentData().GetRawText() == saved.GetRawText(), "grid visibility and data round trip");
        using var legacy = JsonDocument.Parse("{\"RowCount\":2,\"ColumnCount\":2,\"Cells\":[]}");
        var legacyModel = provider.GetRequiredKeyedService<TreeItemNode>("grid-container");
        ((ITreeItemDocumentData)legacyModel).LoadDocumentData(legacy.RootElement);
        Check(!(bool)Get(legacyModel, "ShowGrid")! && (bool)Get(legacyModel, "ShowGrabber")! && (double)Get(legacyModel, "Width")! == 0 && (double)Get(legacyModel, "Height")! == 0, "older documents retain automatic size and visible grabbers");
        Check((bool)Get(legacyModel, "EqualColumnWidths")! && (bool)Get(legacyModel, "EqualRowHeights")!, "older documents retain equal track sizing");
        Check(!(bool)Get(loaded, "EqualColumnWidths")! && (bool)Get(loaded, "EqualRowHeights")!, "independent sizing modes load");

        var bad = JsonSerializer.SerializeToElement(new { RowCount = 2, ColumnCount = 2, Cells = new[] { new { Row = 5, Column = 0, RowSpan = 1, ColumnSpan = 1 } } });
        Check(provider.GetRequiredKeyedService<TreeItemNode>("grid-container") is ITreeItemDocumentData target && ThrowsInvalid(() => target.LoadDocumentData(bad)), "out-of-range saved cell is rejected");
        var badSize = JsonSerializer.SerializeToElement(new { RowCount = 2, ColumnCount = 2, Cells = Array.Empty<object>(), Width = -1 });
        Check(ThrowsInvalid(() => ((ITreeItemDocumentData)legacyModel).LoadDocumentData(badSize)), "negative saved width is rejected");

        var originalPosition = Place(Cells(model)[0]);
        model.Children.Move(0, 4);
        Check(Cells(model).Select(cell => ((TreeItemNode)Get(cell, "Node")!).Name).SequenceEqual(["B", "C", "D", "E", "A"]), "cells follow child order");
        Check(Place(Cells(model)[4]).SequenceEqual(originalPosition), "reordering preserves placement");
        var removed = model.Children[0];
        model.Children.RemoveAt(0);
        Check(removed.Parent is null, "removal clears parent");
        Pump();
        Check(Cells(model).Count == 4 && Descendants(grid).OfType<ContentPresenter>().Count(item => item.DataContext is not null && Cells(model).Contains(item.DataContext)) == 4, "removing a child removes its cell and view");
        window.Close();
    }

    private static void CheckTrackSizing()
    {
        var grid = new GridContainerPanel { RowCount = 3, ColumnCount = 3 };
        var first = new Border { Width = 40, Height = 30 };
        var second = new Border { Width = 120, Height = 80 };
        Grid.SetRow(second, 1);
        Grid.SetColumn(second, 1);
        grid.Children.Add(first);
        grid.Children.Add(second);
        void LayoutGrid(double width, double height)
        {
            grid.Measure(new Size(width, height));
            grid.Arrange(new Rect(0, 0, width, height));
            grid.UpdateLayout();
        }
        static bool Near(double actual, double expected) => Math.Abs(actual - expected) < 0.01;

        grid.Measure(new Size(300, double.PositiveInfinity));
        grid.Arrange(new Rect(new Point(), grid.DesiredSize));
        Check(grid.RowDefinitions.All(item => Near(item.ActualHeight, 80)), "equal rows match tallest child under unbounded height");
        LayoutGrid(300, 240);
        Check(grid.ColumnDefinitions.All(item => Near(item.ActualWidth, 100)) && grid.RowDefinitions.All(item => Near(item.ActualHeight, 80)), "equal tracks divide fixed space");
        second.MinHeight = 150;
        LayoutGrid(300, 90);
        Check(grid.RowDefinitions.All(item => Near(item.ActualHeight, 30)), "fixed height is divided equally despite oversized child");
        second.MinHeight = 0;
        Grid.SetRowSpan(second, 2);
        second.Height = 200;
        grid.Measure(new Size(300, double.PositiveInfinity));
        grid.Arrange(new Rect(new Point(), grid.DesiredSize));
        Check(grid.RowDefinitions.All(item => Near(item.ActualHeight, 100)), "spanning child contributes proportionally to equal automatic rows");
        second.Height = 80;
        Grid.SetRowSpan(second, 1);
        grid.EqualColumnWidths = false;
        LayoutGrid(300, 240);
        Check(Near(grid.ColumnDefinitions[0].ActualWidth, 40) && Near(grid.ColumnDefinitions[1].ActualWidth, 120) && Near(grid.ColumnDefinitions[2].ActualWidth, 0) && grid.RowDefinitions.All(item => Near(item.ActualHeight, 80)), "content widths and equal heights are independent");
        grid.EqualRowHeights = false;
        LayoutGrid(300, 240);
        Check(Near(grid.RowDefinitions[0].ActualHeight, 30) && Near(grid.RowDefinitions[1].ActualHeight, 80) && Near(grid.RowDefinitions[2].ActualHeight, 0), "content heights follow children");
        Check(grid.TryGetCellAt(new Point(40, 30), out var row, out var column) && row == 1 && column == 1, "track boundaries target the following visible cell");
        Check(grid.TryGetCellAt(new Point(160, 110), out row, out column) && row == 1 && column == 1, "last visible track includes its end boundary");
        Check(!grid.TryGetCellAt(new Point(200, 50), out _, out _) && !grid.TryGetCellAt(new Point(10, 150), out _, out _), "unused space does not target a cell");
        grid.EqualColumnWidths = true;
        LayoutGrid(300, 240);
        Check(grid.ColumnDefinitions.All(item => Near(item.ActualWidth, 100)) && Near(grid.RowDefinitions[0].ActualHeight, 30), "equal widths and content heights are independent");
        grid.EqualColumnWidths = false;
        var span = new Border { Width = 200, Height = 50 };
        Grid.SetColumnSpan(span, 2);
        Grid.SetRow(span, 2);
        grid.Children.Add(span);
        LayoutGrid(300, 240);
        Check(grid.ColumnDefinitions[0].ActualWidth + grid.ColumnDefinitions[1].ActualWidth >= 200 && Near(grid.RowDefinitions[2].ActualHeight, 50), "spanning content contributes to automatic tracks");
        grid.Children.Clear();
        LayoutGrid(300, 240);
        Check(!grid.TryGetCellAt(new Point(0, 0), out _, out _), "zero-sized tracks are not drop targets");
        var nested = new GridContainerPanel { EqualColumnWidths = false, EqualRowHeights = false };
        nested.Children.Add(new Border { Width = 70, Height = 45 });
        Grid.SetRow(nested, 2);
        Grid.SetColumn(nested, 2);
        grid.Children.Add(nested);
        grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        grid.Arrange(new Rect(new Point(), grid.DesiredSize));
        grid.UpdateLayout();
        Check(Near(grid.DesiredSize.Width, 70) && Near(grid.DesiredSize.Height, 45) && Near(nested.ColumnDefinitions[0].ActualWidth, 70), "nested content-sized grid measures with automatic dimensions");
        Check(grid.TryGetCellAt(new Point(0, 0), out row, out column) && row == 2 && column == 2, "hit testing skips preceding zero-sized tracks");
    }

    private static void CheckLiveEqualRows(Panel host)
    {
        var grid = new GridContainerPanel { RowCount = 3 };
        var child = new Border { Height = 80 };
        Grid.SetRow(child, 1);
        grid.Children.Add(child);
        host.Children.Add(grid);
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 80) < 0.01), "live automatic grid gives empty rows the tallest row height");
        child.Height = 120;
        host.UpdateLayout();
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 120) < 0.01), "equal automatic rows respond to child growth");
        child.Height = 40;
        host.UpdateLayout();
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 40) < 0.01), "equal automatic rows respond to child shrinkage");
        grid.Height = 90;
        child.MinHeight = 120;
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 30) < 0.01), "live fixed grid divides height equally");
        Check(LayoutInformation.GetLayoutClip(child) is not null, "oversized child is clipped to its fixed-height row");
        Check(grid.IsMeasureValid && grid.IsArrangeValid, "equal-row layout settles after resizing");
        grid.Height = 300;
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 100) < 0.01), "equal rows respond to fixed container height changes");
        grid.EqualRowHeights = false;
        Pump();
        Check(grid.RowDefinitions[1].ActualHeight > grid.RowDefinitions[0].ActualHeight, "turning off equal heights restores content sizing");
        grid.EqualRowHeights = true;
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 100) < 0.01), "turning equal heights back on restores equal layout");
        grid.Height = double.NaN;
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight - 120) < 0.01), "switching back to automatic height fits the content equally");
        grid.Children.Clear();
        Pump();
        Check(grid.RowDefinitions.All(row => Math.Abs(row.ActualHeight) < 0.01), "removing the content reduces automatic equal rows");
        host.Children.Remove(grid);
    }

    private static void CheckComboBoxLayout(Panel host, IServiceProvider containerServices)
    {
        var variables = new VariableHandler();
        variables.Set("choice", "One");
        variables.Set("other", "One");
        var services = new ServiceCollection();
        services.AddSingleton<IVariableHandler>(variables);
        services.AddSingleton(new ResourceDictionary { ["DefaultDetailsTemplate"] = new DataTemplate() });
        var registrator = new Inputs.UI.AddInRegistrator();
        registrator.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        registrator.InitializeAddIn(provider, new DataTemplateManager(provider.GetRequiredService<ResourceDictionary>()));
        var input = provider.GetRequiredKeyedService<TreeItemNode>("input-combobox");
        ((IDocumentItemDetached)input).OnDocumentItemReattached();
        Set(input, "OptionsText", "One\nTwo");
        Set(input, "PropertyName", "choice");
        var gridModel = containerServices.GetRequiredKeyedService<TreeItemNode>("grid-container");
        Set(gridModel, "Height", 400d);
        gridModel.Children.Add(input);
        var view = new GridContainerView { DataContext = gridModel };
        var direct = new ComboBoxView { DataContext = input, Height = 200 };
        host.Children.Add(view);
        host.Children.Add(direct);
        var details = new InputDetails { DataContext = input, Width = 260 };
        var detailsWindow = new Window { Content = details, Width = 350, Height = 400, Left = 120, Top = 120, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        detailsWindow.Show();
        host.UpdateLayout();
        Pump();
        var selector = (ComboBox)details.FindName("PropertyEditor");
        selector.IsDropDownOpen = true;
        var popupFrame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (_, _) => { timer.Stop(); popupFrame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(popupFrame);
        var popup = (Popup)selector.Template.FindName("PART_Popup", selector);
        var selectorTopLeft = selector.PointToScreen(new Point());
        var selectorBottomRight = selector.PointToScreen(new Point(selector.ActualWidth, selector.ActualHeight));
        var popupTopLeft = popup.Child.PointToScreen(new Point());
        var popupBottomRight = popup.Child.PointToScreen(new Point(popup.Child.RenderSize.Width, popup.Child.RenderSize.Height));
        Check(popup.IsOpen && popupTopLeft.X < selectorBottomRight.X && popupBottomRight.X > selectorTopLeft.X &&
              (Math.Abs(popupTopLeft.Y - selectorBottomRight.Y) < 20 || Math.Abs(popupBottomRight.Y - selectorTopLeft.Y) < 20),
              $"property popup opens next to its selector, not at {popupTopLeft}");
        selector.SelectedItem = "other";
        selector.GetBindingExpression(ComboBox.TextProperty)!.UpdateSource();
        Pump();
        Check((string)Get(input, "PropertyName")! == "other", "property selector updates the selected property");
        selector.IsDropDownOpen = false;
        Pump();
        selector.IsDropDownOpen = true;
        Pump();
        Check(popup.IsOpen && popup.Child.PointToScreen(new Point()).X > 0, "property dropdown reopens away from the screen origin");
        selector.IsDropDownOpen = false;
        Set(input, "PropertyName", "choice");
        var gridControl = Descendants(view).OfType<ComboBox>().Single();
        var directControl = Descendants(direct).OfType<ComboBox>().Single();
        Check(gridControl.ActualHeight > 0 && Math.Abs(gridControl.ActualHeight - gridControl.DesiredSize.Height) < 0.1, "combo box stays at its natural height in a tall grid cell");
        Check(directControl.ActualHeight > 0 && Math.Abs(directControl.ActualHeight - directControl.DesiredSize.Height) < 0.1, "combo box stays at its natural height in a tall direct view");
        Check(gridControl.ActualWidth == 200 && gridControl.IsEnabled, "combo box retains width and availability");
        gridControl.SelectedItem = "Two";
        Pump();
        Check(variables.TryGet<string>("choice", out var value) && value == "Two", "combo box selection still updates its property");
        host.Children.Remove(view);
        host.Children.Remove(direct);
        detailsWindow.Close();
        ((IDocumentItemDetached)input).OnDocumentItemDetached();
    }

    static bool ThrowsInvalid(Action action) { try { action(); return false; } catch (System.IO.InvalidDataException) { return true; } }
}
