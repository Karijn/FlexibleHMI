using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ColorPicker;
using FlexibleHMI.Interfaces;
using Inputs.UI.Potmeter;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager;
using VUMeter.UI;

internal static class Program
{
    static object? Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetProperty(name)!.SetValue(o, value);
    static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
    static void Pump()
    {
        var f = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => f.Continue = false));
        Dispatcher.PushFrame(f);
    }
    static void PumpFor(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject o)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
        {
            var c = VisualTreeHelper.GetChild(o, i);
            yield return c;
            foreach (var child in Descendants(c))
                yield return child;
        }
    }
    static object New(string name, VariableHandler vars) => Activator.CreateInstance(typeof(XYPlot).Assembly.GetType("VUMeter.UI." + name, true)!, vars)!;
    static void CheckPotentiometerColors(Window window, VariableHandler vars)
    {
        var viewModelType = typeof(PotentiometerView).Assembly.GetType("Inputs.UI.Potmeter.PotentiometerViewModel", true)!;
        var model = Activator.CreateInstance(viewModelType, vars)!;
        ((IDocumentItemDetached)model).OnDocumentItemReattached();
        Set(model, "PropertyName", "left");
        var details = new PotentiometerDetails { DataContext = model };
        var view = new PotentiometerView { DataContext = model };
        var panel = new StackPanel();
        panel.Children.Add(details);
        panel.Children.Add(view);
        window.Content = panel;
        Pump();

        var picker = (PortableColorPicker)details.FindName("ColorPicker");
        var borderPicker = (PortableColorPicker)details.FindName("BorderColorPicker");
        var dial = Descendants(view).OfType<PotentiometerControl>().Single();
        Check(picker.SelectedColor == (Color)Get(model, "ForeColor")! && picker.SecondaryColor == (Color)Get(model, "BackColor")!, "picker initial colors");
        Check(borderPicker.SelectedColor == (Color)Get(model, "BorderColor")!, "border picker initial color");
        int dirty = 0;
        ((IDocumentDataChangeNotifier)model).DocumentDataChanged += (_, _) => dirty++;
        picker.SetCurrentValue(PortableColorPicker.SelectedColorProperty, Colors.Gold);
        picker.SetCurrentValue(PortableColorPicker.SecondaryColorProperty, Colors.Purple);
        borderPicker.SetCurrentValue(PortableColorPicker.SelectedColorProperty, Colors.Orange);
        Pump();
        Check((Color)Get(model, "ForeColor")! == Colors.Gold && (Color)Get(model, "BackColor")! == Colors.Purple && (Color)Get(model, "BorderColor")! == Colors.Orange && dirty == 3, "pickers update persisted colors");
        Check(dial.ForeColor == Colors.Gold && dial.BackColor == Colors.Purple && dial.BorderColor == Colors.Orange, "pickers update dial colors");
        var drawing = VisualTreeHelper.GetDrawing(dial);
        Check(drawing.Children.OfType<GeometryDrawing>().Any(item => item.Pen?.Brush is SolidColorBrush brush && brush.Color == Colors.Gold), "foreground redraws dial pointer");
        Check(drawing.Children.OfType<GeometryDrawing>().Any(item => item.Geometry is EllipseGeometry && item.Pen?.Brush is SolidColorBrush brush && brush.Color == Colors.Orange), "border redraws dial outline");
        Set(model, "ForeColor", Colors.Coral);
        Set(model, "BackColor", Colors.Teal);
        Set(model, "BorderColor", Colors.Navy);
        Pump();
        Check(picker.SelectedColor == Colors.Coral && picker.SecondaryColor == Colors.Teal && borderPicker.SelectedColor == Colors.Navy, "external color changes update pickers");
        var saved = ((ITreeItemDocumentData)model).SaveDocumentData();
        var copy = ((TreeItemNode)model).Copy();
        Check(((ITreeItemDocumentData)copy).SaveDocumentData().GetRawText() == saved.GetRawText(), "potentiometer colors survive save and copy");
        Set(model, "BorderColor", Colors.Red);
        ((ITreeItemDocumentData)model).LoadDocumentData(saved);
        Pump();
        Check((Color)Get(model, "BorderColor")! == Colors.Navy && borderPicker.SelectedColor == Colors.Navy && dial.BorderColor == Colors.Navy, "saved border color restores picker and dial");
        ((IDocumentItemDetached)model).OnDocumentItemDetached();
    }
    static void CheckLedBar(VariableHandler vars)
    {
        vars.Set("level", 0d);
        var services = new ServiceCollection();
        services.AddSingleton<PropertyManager.Interfaces.IVariableHandler>(vars);
        var resources = new ResourceDictionary();
        services.AddSingleton(resources);
        var registrator = new AddinRegistrator();
        registrator.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        registrator.InitializeAddIn(provider, new DataTemplateManager(resources));
        var model = provider.GetRequiredKeyedService<TreeItemNode>("ledbar-vumeter");
        Check(provider.GetRequiredKeyedService<TreeItemOption>("ledbar-vumeter") is not null, "LED bar creation registered");
        var registrations = provider.GetServices<ViewTemplateRegistration>().Where(item => item.ViewModelType == model.GetType()).ToArray();
        Check(registrations.Length == 2 && registrations.Any(item => item.View == ViewKind.MainView && item.Template.LoadContent() is LedBarView) && registrations.Any(item => item.View == ViewKind.DetailsView && item.Template.LoadContent() is LedBarDetails), "LED bar main and details registration");
        var life = (IDocumentItemDetached)model;
        life.OnDocumentItemReattached();
        Set(model, "PropertyName", "level");
        Set(model, "FadeMilliseconds", 0d);
        Check((int)Get(model, "SegmentCount")! == 10 && (int)Get(model, "LitSegments")! == 0, "LED bar defaults and minimum");
        int dirty = 0;
        ((IDocumentDataChangeNotifier)model).DocumentDataChanged += (_, _) => dirty++;
        vars.Set("level", 9.99);
        Check((int)Get(model, "LitSegments")! == 0, "below first threshold");
        vars.Set("level", 10d);
        Check((int)Get(model, "LitSegments")! == 1, "first threshold");
        vars.Set("level", 50d);
        Check((int)Get(model, "LitSegments")! == 5 && dirty == 0, "half scale and live updates do not dirty document");
        var view = new LedBarView { DataContext = model };
        var details = new LedBarDetails { DataContext = model };
        var window = new Window { Width = 400, Height = 400, Content = view, ShowInTaskbar = false };
        window.Show();
        Pump();
        var bar = Descendants(view).OfType<LedBarDisplay>().Single();
        var rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        Check(rectangles.Length == 10 && rectangles.Count(item => item.Brush is SolidColorBrush brush && brush.Color == Colors.LimeGreen) == 5, "LED bar draws five of ten rows");
        Check(rectangles.Take(5).All(item => item.Brush is SolidColorBrush brush && brush.Color == Colors.LimeGreen) && rectangles.Skip(5).All(item => item.Brush == Brushes.DarkGreen), "LED bar fills bottom up");
        vars.Set("level", 200d);
        Check((int)Get(model, "LitSegments")! == 10 && ((string)Get(model, "Status")!).Contains("clamped"), "high value clamps");
        vars.Set("level", -1);
        Check((int)Get(model, "LitSegments")! == 0, "integer value clamps low");
        Set(model, "SegmentCount", 2000);
        vars.Set("level", 25d);
        Check((int)Get(model, "LitSegments")! == 500 && dirty == 1, "large segment count and configuration dirty state");
        Pump();
        Check(VisualTreeHelper.GetDrawing(bar).Children.Count < 2000, "large counts aggregate at screen resolution");
        var saved = ((ITreeItemDocumentData)model).SaveDocumentData();
        var copy = model.Copy();
        Check(((ITreeItemDocumentData)copy).SaveDocumentData().GetRawText() == saved.GetRawText(), "LED bar copy persists settings");
        Set(model, "SegmentCount", 7);
        ((ITreeItemDocumentData)model).LoadDocumentData(saved);
        Check((int)Get(model, "SegmentCount")! == 2000 && (int)Get(model, "LitSegments")! == 500, "LED bar settings restore");
        Set(model, "Minimum", 100d);
        Check((int)Get(model, "LitSegments")! == 0 && ((string)Get(model, "Status")!).Contains("Minimum"), "invalid range is reported");
        Set(model, "Minimum", 0d);
        vars.Set("level", double.NaN);
        Check((int)Get(model, "LitSegments")! == 0 && ((string)Get(model, "Status")!).Contains("unavailable"), "non-finite property reported");
        vars.Remove("level");
        Check(((string)Get(model, "PropertyName")!) == "level" && ((string)Get(model, "Status")!).Contains("unavailable"), "missing property retained");
        vars.Set("level", 25d);
        life.OnDocumentItemDetached();
        vars.Set("level", 50d);
        Check((int)Get(model, "LitSegments")! == 500, "detached LED bar ignores updates");
        life.OnDocumentItemReattached();
        Check((int)Get(model, "LitSegments")! == 1000, "reattached LED bar refreshes");
        var editsBefore = dirty;
        Task.Run(() => vars.Set("level", 75d)).GetAwaiter().GetResult();
        Pump();
        Check((int)Get(model, "LitSegments")! == 1500 && dirty == editsBefore, "worker thread update delivered without dirtying document");
        Set(model, "SegmentCount", int.MaxValue);
        Pump();
        Check((int)Get(model, "LitSegments")! == (int)Math.Floor(0.75 * int.MaxValue) && VisualTreeHelper.GetDrawing(bar).Children.Count < 2000, "maximum segment count renders without allocating each segment");
        window.Content = details;
        Pump();
        var editor = (TextBox)details.FindName("SegmentCountEditor");
        editor.Text = "0";
        editor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(Validation.GetHasError(editor) && (int)Get(model, "SegmentCount")! == int.MaxValue, "invalid segment count is reported without changing configuration");
        editor.Text = "8";
        editor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(!Validation.GetHasError(editor) && (int)Get(model, "SegmentCount")! == 8 && (int)Get(model, "LitSegments")! == 6, "settings editor changes segment count");
        var selector = (ComboBox)details.FindName("PropertySelector");
        selector.Text = "right";
        selector.GetBindingExpression(ComboBox.TextProperty)!.UpdateSource();
        Check((string)Get(model, "PropertyName")! == "right" && (int)Get(model, "LitSegments")! == 2, "settings selector switches numeric property");
        var minimumEditor = (TextBox)details.FindName("MinimumEditor");
        var maximumEditor = (TextBox)details.FindName("MaximumEditor");
        minimumEditor.Text = "-50";
        minimumEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        maximumEditor.Text = "50";
        maximumEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check((double)Get(model, "Minimum")! == -50 && (double)Get(model, "Maximum")! == 50 && (int)Get(model, "LitSegments")! == 6, "configured range updates thresholds");
        vars.Set("right", 50d);
        Check((int)Get(model, "LitSegments")! == 8, "configured maximum lights all segments");
        vars.Set("right", "not numeric");
        Check((int)Get(model, "LitSegments")! == 0 && ((string)Get(model, "Status")!).Contains("unavailable"), "wrong property type is reported");
        vars.Set("right", 50d);
        var columnEditor = (TextBox)details.FindName("ColumnCountEditor");
        columnEditor.Text = "0";
        columnEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(Validation.GetHasError(columnEditor) && (int)Get(model, "ColumnCount")! == 1, "invalid column count is reported");
        columnEditor.Text = "3";
        columnEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(!Validation.GetHasError(columnEditor) && (int)Get(model, "ColumnCount")! == 3, "column editor updates count");
        Set(model, "SelectedColumn", 2);
        Set(model, "PropertyName", "right");
        Set(model, "SelectedColumn", 3);
        Set(model, "PropertyName", "right");
        Set(model, "SelectedColumn", 1);
        var basePicker = (PortableColorPicker)details.FindName("BaseColorPicker");
        basePicker.SetCurrentValue(PortableColorPicker.SelectedColorProperty, Colors.Blue);
        Check((Color)Get(model, "BaseColor")! == Colors.Blue, "base picker updates color");
        var stops = (IList)Get(model, "ColorStops")!;
        ((ICommand)Get(model, "AddColorStopCommand")!).Execute(null);
        var stop = stops[0]!;
        Pump();
        var heightEditor = Descendants(details).OfType<TextBox>().Single(item => item.Name == "HeightEditor");
        heightEditor.Text = "101";
        heightEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(Validation.GetHasError(heightEditor) && (double)Get(stop, "Height")! == 100d, "invalid stop height is reported");
        heightEditor.Text = "50";
        heightEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(!Validation.GetHasError(heightEditor) && (double)Get(stop, "Height")! == 50d, "stop height editor updates position");
        var stopPicker = Descendants(details).OfType<PortableColorPicker>().Single(item => ReferenceEquals(item.DataContext, stop));
        stopPicker.SetCurrentValue(PortableColorPicker.SelectedColorProperty, Colors.Red);
        Check((Color)Get(stop, "Color")! == Colors.Red, "stop picker updates color");
        var colorEditor = Descendants(details).OfType<TextBox>().Single(item => item.Name == "ColorEditor" && ReferenceEquals(item.DataContext, stop));
        colorEditor.Text = "#00FF00";
        colorEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(!Validation.GetHasError(colorEditor) && (Color)Get(stop, "Color")! == Color.FromRgb(0, 255, 0), "stop hex editor updates color");
        colorEditor.Text = "nonsense";
        colorEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check(Validation.GetHasError(colorEditor), "invalid stop color text is reported");
        colorEditor.Text = "Red";
        colorEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check((Color)Get(stop, "Color")! == Colors.Red && !Validation.GetHasError(colorEditor), "stop color name restores red");

        window.Content = view;
        Pump();
        rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        Check(rectangles.Length == 24, "three columns with eight segments each");
        Check(Enumerable.Range(0, 3).All(column => rectangles.Skip(column * 8).Take(3).All(item => item.Brush is SolidColorBrush brush && brush.Color == Colors.Blue) && rectangles.Skip(column * 8 + 3).Take(5).All(item => item.Brush is SolidColorBrush brush && brush.Color == Colors.Red)), "stepped colors apply at threshold in every column");
        Set(model, "UseGradient", true);
        Pump();
        rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        var firstColor = ((SolidColorBrush)rectangles[0].Brush).Color;
        Check(firstColor == Color.FromRgb(64, 0, 191) && Enumerable.Range(0, 3).All(column => ((SolidColorBrush)rectangles[column * 8].Brush).Color == firstColor), "gradient interpolates consistently across columns");
        ((ICommand)Get(model, "AddColorStopCommand")!).Execute(null);
        var secondStop = stops[1]!;
        Set(secondStop, "Height", 75d);
        Set(secondStop, "Color", Colors.Yellow);
        Pump();
        rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        Check(((SolidColorBrush)rectangles[5].Brush).Color == Colors.Yellow, "additional color stop reaches exact height");
        ((ICommand)Get(model, "RemoveColorStopCommand")!).Execute(secondStop);
        var unitType = Get(model, "HeightUnit")!.GetType();
        Set(model, "HeightUnit", Enum.Parse(unitType, "Segment"));
        Check((double)Get(stop, "Height")! == 4d, "percent to segment preserves height");
        Set(model, "HeightUnit", Enum.Parse(unitType, "PropertyValue"));
        Check((double)Get(stop, "Height")! == 0d, "segment to property value preserves height");
        Set(model, "HeightUnit", Enum.Parse(unitType, "Percentage"));
        Check((double)Get(stop, "Height")! == 50d, "property value to percent preserves height");
        var colorSaved = ((ITreeItemDocumentData)model).SaveDocumentData();
        var colorCopy = model.Copy();
        Check(((ITreeItemDocumentData)colorCopy).SaveDocumentData().GetRawText() == colorSaved.GetRawText(), "columns and colors survive save and copy");
        ((ICommand)Get(model, "RemoveColorStopCommand")!).Execute(stop);
        Check(stops.Count == 0, "remove color stop");
        ((ITreeItemDocumentData)model).LoadDocumentData(colorSaved);
        Check((int)Get(model, "ColumnCount")! == 3 && stops.Count == 1 && (bool)Get(model, "UseGradient")!, "columns and colors restore");
        Set(model, "ColumnCount", int.MaxValue);
        Pump();
        Check(VisualTreeHelper.GetDrawing(bar).Children.Count < 10000, "large column count is bounded by available space");
        using var oldSettings = JsonDocument.Parse("{\"PropertyName\":\"right\",\"SegmentCount\":8,\"Minimum\":0,\"Maximum\":100,\"ColumnCount\":3}");
        ((ITreeItemDocumentData)model).LoadDocumentData(oldSettings.RootElement);
        Check((int)Get(model, "ColumnCount")! == 3 && (Color)Get(model, "BaseColor")! == Colors.LimeGreen && stops.Count == 0, "legacy settings retain defaults");
        Set(model, "SelectedColumn", 2);
        Check((string)Get(model, "PropertyName")! == "right", "legacy property migrates to every existing column");
        vars.Set("left", 25d);
        vars.Set("right", 50d);
        vars.Set("third", 75d);
        Set(model, "FadeMilliseconds", 0d);
        Set(model, "SelectedColumn", 1);
        Set(model, "PropertyName", "left");
        Set(model, "SelectedColumn", 3);
        Set(model, "PropertyName", "third");
        Set(model, "ColumnCount", 2);
        Set(model, "ColumnCount", 3);
        Set(model, "SelectedColumn", 3);
        Check((string)Get(model, "PropertyName")! == "third", "hidden column assignment retained");
        Pump();
        rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        Check(Enumerable.Range(0, 3).All(column => rectangles.Skip(column * 8).Take(8).Count(item => item.Brush is SolidColorBrush brush && brush.Color == Colors.LimeGreen) == (column + 1) * 2), "columns independently display their own properties");
        Set(model, "VisibleRows", 3);
        Pump();
        rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        Check(Enumerable.Range(0, 3).All(column => rectangles.Skip(column * 8).Take(8).Select((item, row) => (item, row)).Where(pair => pair.item.Brush is SolidColorBrush brush && brush.Color == Colors.LimeGreen).Select(pair => pair.row).SequenceEqual(Enumerable.Range(column * 2, 3))), "centered band follows each column level");
        Set(model, "VisibleRows", 4);
        vars.Set("left", 100d);
        Pump();
        rectangles = VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().ToArray();
        Check(rectangles.Take(8).Select((item, row) => (item, row)).Where(pair => pair.item.Brush is SolidColorBrush brush && brush.Color == Colors.LimeGreen).Select(pair => pair.row).SequenceEqual(Enumerable.Range(4, 4)), "even band stays inside top boundary");
        var savedColumns = ((ITreeItemDocumentData)model).SaveDocumentData();
        var columnsCopy = model.Copy();
        Check(((ITreeItemDocumentData)columnsCopy).SaveDocumentData().GetRawText() == savedColumns.GetRawText(), "independent assignments and row count persist");
        Set(model, "VisibleRows", 0);
        vars.Set("left", 0d);
        Pump();
        Set(model, "FadeMilliseconds", 400d);
        vars.Set("left", 100d);
        PumpFor(120);
        var fadeIn = ((SolidColorBrush)VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().First().Brush).Color;
        Check(fadeIn != Colors.DarkGreen && fadeIn != Colors.LimeGreen, $"rows fade in over time (color {fadeIn})");
        PumpFor(450);
        Check(((SolidColorBrush)VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().First().Brush).Color == Colors.LimeGreen, "fade in reaches full brightness");
        vars.Set("left", 0d);
        PumpFor(120);
        var fadeOut = ((SolidColorBrush)VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().First().Brush).Color;
        Check(fadeOut != Colors.DarkGreen && fadeOut != Colors.LimeGreen, "rows fade out over time");
        PumpFor(450);
        Check(((SolidColorBrush)VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().First().Brush).Color == Colors.DarkGreen, "fade out reaches off color");
        var savedFade = ((ITreeItemDocumentData)model).SaveDocumentData();
        Check((double)Get(model.Copy(), "FadeMilliseconds")! == 400d, "fade duration persists in copies");
        life.OnDocumentItemDetached();
        vars.Set("left", 100d);
        bar.InvalidateVisual();
        Pump();
        Check(((SolidColorBrush)VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().First().Brush).Color == Colors.DarkGreen, "detached display does not read new values");
        life.OnDocumentItemReattached();
        PumpFor(450);
        Check(((SolidColorBrush)VisualTreeHelper.GetDrawing(bar).Children.OfType<GeometryDrawing>().First().Brush).Color == Colors.LimeGreen, "reattached display refreshes independent column");
        ((ITreeItemDocumentData)model).LoadDocumentData(savedFade);
        Check((double)Get(model, "FadeMilliseconds")! == 400d, "fade duration restores from document");
        life.OnDocumentItemDetached();
        window.Close();
    }
    [STAThread]
    static void Main(string[] args)
    {
        _ = new Application();
        var vars = new VariableHandler();
        vars.Set("left", -25);
        vars.Set("right", 35d);
        vars.Set("shade", System.Drawing.Color.Blue);
        if (args.Length == 1 && args[0] == "led-bar")
        {
            CheckLedBar(vars);
            Console.WriteLine("LED bar verification passed.");
            return;
        }
        if (args.Length == 1 && args[0] == "grid-container")
        {
            ContainerChecks.CheckGrid();
            Console.WriteLine("Grid container verification passed.");
            return;
        }
        if (args.Length == 1 && args[0] == "grid-drag")
        {
            GridDragChecks.Run();
            Console.WriteLine("Grid drag verification passed.");
            return;
        }
        if (args.Length == 1 && args[0] == "vu-details")
        {
            VUMeterDetailsChecks.Run();
            Console.WriteLine("VU meter details verification passed.");
            return;
        }
        if (args.Length == 1 && args[0] == "input-details")
        {
            InputDetailsChecks.Run();
            Console.WriteLine("Input details verification passed.");
            return;
        }
        if (args.Length == 1 && args[0] == "plot-details")
        {
            PlotDetailsChecks.Run();
            Console.WriteLine("Plot details verification passed.");
            return;
        }
        if (args.Length == 1 && args[0] == "potentiometer-colors")
        {
            var colorWindow = new Window { Width = 900, Height = 800, ShowInTaskbar = false };
            colorWindow.Show();
            CheckPotentiometerColors(colorWindow, vars);
            colorWindow.Close();
            Console.WriteLine("Potentiometer color verification passed.");
            return;
        }
        var vu = New("VUMeterViewModel", vars);
        var life = (IDocumentItemDetached)vu;
        life.OnDocumentItemReattached();
        var inputs = (IList)Get(vu, "Inputs")!;
        var first = inputs[0]!;
        Set(first, "PropertyName", "left");
        ((ICommand)Get(vu, "AddInputCommand")!).Execute(null);
        var second = inputs[1]!;
        Set(second, "PropertyName", "right");
        Set(second, "Color", "Red");
        Check((double)Get(first, "Angle")! == -25 && (double)Get(second, "Angle")! == 35, "multiple angles");
        vars.Set("right", 80);
        Check((double)Get(second, "Angle")! == 50 && (bool)Get(vu, "IsPeakActive")!, "clamp and peak");
        var view = new VUMeterView { DataContext = vu };
        var window = new Window { Content = view, Width = 900, Height = 800, ShowInTaskbar = false };
        window.Show();
        Pump();
        var lines = Descendants(view).OfType<System.Windows.Shapes.Line>().Where(l => l.Stroke is SolidColorBrush b && (b.Color == Colors.Red || b.Color == Colors.Black)).ToList();
        Check(lines.Any(l => l.RenderTransform is RotateTransform r && r.Angle == -25) && lines.Any(l => l.RenderTransform is RotateTransform r && r.Angle == 50), "render both needles");
        int dirty = 0;
        ((IDocumentDataChangeNotifier)vu).DocumentDataChanged += (_, _) => dirty++;
        Set(vu, "ShowLegend", true);
        Pump();
        Check(dirty == 1 && Descendants(view).OfType<Border>().Any(b => b.IsVisible && b.Background is SolidColorBrush brush && brush.Color == Colors.Red && b.Width == 12), "visible VU legend and dirty state");
        Set(vu, "ShowLegend", false);
        Pump();
        Check(!Descendants(view).OfType<Border>().Any(b => b.IsVisible && b.Background is SolidColorBrush brush && brush.Color == Colors.Red && b.Width == 12), "hidden VU legend");
        Set(vu, "ShowLegend", true);
        var saved = ((ITreeItemDocumentData)vu).SaveDocumentData();
        var copy = ((TreeItemNode)vu).Copy();
        Check(((ITreeItemDocumentData)copy).SaveDocumentData().GetRawText() == saved.GetRawText(), "VU save and copy");
        using var legacy = JsonDocument.Parse("{\"PropertyName\":\"left\"}");
        ((ITreeItemDocumentData)copy).LoadDocumentData(legacy.RootElement);
        var oldInput = ((IList)Get(copy, "Inputs")!)[0]!;
        Check((string)Get(oldInput, "PropertyName")! == "left" && (string)Get(oldInput, "Color")! == "Black" && !(bool)Get(copy, "ShowLegend")!, "legacy VU migration");
        vars.Remove("right");
        Check(!(bool)Get(second, "IsAvailable")! && (string)Get(second, "PropertyName")! == "right", "missing binding retained");
        vars.Set("right", 10);
        Check((bool)Get(second, "IsAvailable")! && (double)Get(second, "Angle")! == 10, "binding restored");
        life.OnDocumentItemDetached();
        vars.Set("right", 22);
        Check((double)Get(second, "Angle")! == 10, "detach stops updates");
        life.OnDocumentItemReattached();
        Check((double)Get(second, "Angle")! == 22, "reattach refreshes");
        var editsBefore = dirty;
        Task.Run(() => vars.Set("right", 33)).GetAwaiter().GetResult();
        Pump();
        Check((double)Get(second, "Angle")! == 33 && dirty == editsBefore, "worker update not a document edit");
        ((ICommand)Get(vu, "RemoveInputCommand")!).Execute(second);
        Check(inputs.Count == 1 && !(bool)Get(vu, "IsPeakActive")!, "remove input");
        var xy = New("XYControlViewModel", vars);
        ((IDocumentItemDetached)xy).OnDocumentItemReattached();
        var point = ((IList)Get(xy, "Points")!)[0]!;
        Set(point, "XProperty", "left");
        Set(point, "YProperty", "right");
        Set(point, "ColorProperty", "shade");
        var xyView = new XYControlView { DataContext = xy };
        window.Content = xyView;
        Set(xy, "ShowLegend", true);
        Pump();
        Check(Descendants(xyView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Blue && b.Width == 12), "XY legend bound color");
        vars.Set("shade", System.Drawing.Color.Green);
        Pump();
        Check(Descendants(xyView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Green && b.Width == 12), "XY legend live color");
        var xySaved = ((ITreeItemDocumentData)xy).SaveDocumentData();
        var xyCopy = ((TreeItemNode)xy).Copy();
        Check(((ITreeItemDocumentData)xyCopy).SaveDocumentData().GetRawText() == xySaved.GetRawText(), "XY legend save copy");
        using var xyLegacy = JsonDocument.Parse("{\"MinimumX\":-100,\"MaximumX\":100,\"MinimumY\":-100,\"MaximumY\":100,\"Points\":[{\"XProperty\":\"left\",\"YProperty\":\"right\",\"Color\":\"Red\",\"Size\":10,\"ShowTrail\":false}]}");
        ((ITreeItemDocumentData)xyCopy).LoadDocumentData(xyLegacy.RootElement);
        Check(!(bool)Get(xyCopy, "ShowLegend")!, "old XY legend default");
        var scope = New("OscilloscopeViewModel", vars);
        var channel = ((IList)Get(scope, "Channels")!)[0]!;
        Set(channel, "InputProperty", "left");
        Set(channel, "ColorProperty", "shade");
        var sample = scope.GetType().GetMethod("Sample", BindingFlags.NonPublic | BindingFlags.Instance)!;
        sample.Invoke(scope, [100d]);
        Set(scope, "ShowLegend", true);
        var scopeView = new OscilloscopeView { DataContext = scope };
        window.Content = scopeView;
        Pump();
        Check(Descendants(scopeView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Green && b.Width == 12), "scope legend color");
        vars.Set("shade", System.Drawing.Color.Blue);
        sample.Invoke(scope, [100.02]);
        Pump();
        Check(Descendants(scopeView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Blue && b.Width == 12), "scope legend live color");
        var scopeSaved = ((ITreeItemDocumentData)scope).SaveDocumentData();
        var scopeCopy = ((TreeItemNode)scope).Copy();
        Check(((ITreeItemDocumentData)scopeCopy).SaveDocumentData().GetRawText() == scopeSaved.GetRawText(), "scope legend save copy");
        using var scopeLegacy = JsonDocument.Parse(scopeSaved.GetRawText().Replace(",\"ShowLegend\":true", ""));
        ((ITreeItemDocumentData)scopeCopy).LoadDocumentData(scopeLegacy.RootElement);
        Check(!(bool)Get(scopeCopy, "ShowLegend")!, "old scope legend default");
        ((IDocumentItemDetached)xy).OnDocumentItemDetached();
        life.OnDocumentItemDetached();
        CheckPotentiometerColors(window, vars);
        window.Close();
        Console.WriteLine("Legend, multi-input VU, and potentiometer color verification passed.");
    }
}
