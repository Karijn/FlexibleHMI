using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FlexibleHMI.Interfaces;
using PropertyManager;
using VUMeter.UI;

internal static class Program
{
    static object? Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetProperty(name)!.SetValue(o, value);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Pump() { var f = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => f.Continue = false)); Dispatcher.PushFrame(f); }
    static IEnumerable<DependencyObject> Descendants(DependencyObject o) { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++) { var c = VisualTreeHelper.GetChild(o, i); yield return c; foreach (var child in Descendants(c)) yield return child; } }
    static object New(string name, VariableHandler vars) => Activator.CreateInstance(typeof(XYPlot).Assembly.GetType("VUMeter.UI." + name, true)!, vars)!;
    [STAThread]
    static void Main()
    {
        _ = new Application();
        var vars = new VariableHandler(); 
        vars.Set("left", -25); 
        vars.Set("right", 35d); 
        vars.Set("shade", System.Drawing.Color.Blue);
        var vu = New("VUMeterViewModel", vars); 
        var life = (IDocumentItemDetached)vu; 
        life.OnDocumentItemReattached();
        var inputs = (IList)Get(vu, "Inputs")!; var first = inputs[0]!; Set(first, "PropertyName", "left");
        ((ICommand)Get(vu, "AddInputCommand")!).Execute(null); var second = inputs[1]!; Set(second, "PropertyName", "right"); Set(second, "Color", "Red");
        Check((double)Get(first, "Angle")! == -25 && (double)Get(second, "Angle")! == 35, "multiple angles"); vars.Set("right", 80); Check((double)Get(second, "Angle")! == 50 && (bool)Get(vu, "IsPeakActive")!, "clamp and peak");
        var view = new VUMeterView { DataContext = vu }; var window = new Window { Content = view, Width = 900, Height = 800, ShowInTaskbar = false }; window.Show(); Pump();
        var lines = Descendants(view).OfType<System.Windows.Shapes.Line>().Where(l => l.Stroke is SolidColorBrush b && (b.Color == Colors.Red || b.Color == Colors.Black)).ToList();
        Check(lines.Any(l => l.RenderTransform is RotateTransform r && r.Angle == -25) && lines.Any(l => l.RenderTransform is RotateTransform r && r.Angle == 50), "render both needles");
        int dirty = 0; ((IDocumentDataChangeNotifier)vu).DocumentDataChanged += (_, _) => dirty++;
        Set(vu, "ShowLegend", true); Pump(); Check(dirty == 1 && Descendants(view).OfType<Border>().Any(b => b.IsVisible && b.Background is SolidColorBrush brush && brush.Color == Colors.Red && b.Width == 12), "visible VU legend and dirty state");
        Set(vu, "ShowLegend", false); Pump(); Check(!Descendants(view).OfType<Border>().Any(b => b.IsVisible && b.Background is SolidColorBrush brush && brush.Color == Colors.Red && b.Width == 12), "hidden VU legend"); Set(vu, "ShowLegend", true);
        var saved = ((ITreeItemDocumentData)vu).SaveDocumentData(); var copy = ((TreeItemNode)vu).Copy(); Check(((ITreeItemDocumentData)copy).SaveDocumentData().GetRawText() == saved.GetRawText(), "VU save and copy");
        using var legacy = JsonDocument.Parse("{\"PropertyName\":\"left\"}"); ((ITreeItemDocumentData)copy).LoadDocumentData(legacy.RootElement); var oldInput = ((IList)Get(copy, "Inputs")!)[0]!;
        Check((string)Get(oldInput, "PropertyName")! == "left" && (string)Get(oldInput, "Color")! == "Black" && !(bool)Get(copy, "ShowLegend")!, "legacy VU migration");
        vars.Remove("right"); Check(!(bool)Get(second, "IsAvailable")! && (string)Get(second, "PropertyName")! == "right", "missing binding retained"); vars.Set("right", 10); Check((bool)Get(second, "IsAvailable")! && (double)Get(second, "Angle")! == 10, "binding restored");
        life.OnDocumentItemDetached(); vars.Set("right", 22); Check((double)Get(second, "Angle")! == 10, "detach stops updates"); life.OnDocumentItemReattached(); Check((double)Get(second, "Angle")! == 22, "reattach refreshes");
        var editsBefore = dirty; Task.Run(() => vars.Set("right", 33)).GetAwaiter().GetResult(); Pump(); Check((double)Get(second, "Angle")! == 33 && dirty == editsBefore, "worker update not a document edit");
        ((ICommand)Get(vu, "RemoveInputCommand")!).Execute(second); Check(inputs.Count == 1 && !(bool)Get(vu, "IsPeakActive")!, "remove input");
        var xy = New("XYControlViewModel", vars); ((IDocumentItemDetached)xy).OnDocumentItemReattached(); var point = ((IList)Get(xy, "Points")!)[0]!; Set(point, "XProperty", "left"); Set(point, "YProperty", "right"); Set(point, "ColorProperty", "shade");
        var xyView = new XYControlView { DataContext = xy }; window.Content = xyView; Set(xy, "ShowLegend", true); Pump();
        Check(Descendants(xyView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Blue && b.Width == 12), "XY legend bound color");
        vars.Set("shade", System.Drawing.Color.Green); Pump(); Check(Descendants(xyView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Green && b.Width == 12), "XY legend live color");
        var xySaved = ((ITreeItemDocumentData)xy).SaveDocumentData(); var xyCopy = ((TreeItemNode)xy).Copy(); Check(((ITreeItemDocumentData)xyCopy).SaveDocumentData().GetRawText() == xySaved.GetRawText(), "XY legend save copy");
        using var xyLegacy = JsonDocument.Parse("{\"MinimumX\":-100,\"MaximumX\":100,\"MinimumY\":-100,\"MaximumY\":100,\"Points\":[{\"XProperty\":\"left\",\"YProperty\":\"right\",\"Color\":\"Red\",\"Size\":10,\"ShowTrail\":false}]}"); ((ITreeItemDocumentData)xyCopy).LoadDocumentData(xyLegacy.RootElement); Check(!(bool)Get(xyCopy, "ShowLegend")!, "old XY legend default");
        var scope = New("OscilloscopeViewModel", vars); var channel = ((IList)Get(scope, "Channels")!)[0]!; Set(channel, "InputProperty", "left"); Set(channel, "ColorProperty", "shade");
        var sample = scope.GetType().GetMethod("Sample", BindingFlags.NonPublic | BindingFlags.Instance)!; sample.Invoke(scope, [100d]); Set(scope, "ShowLegend", true); var scopeView = new OscilloscopeView { DataContext = scope }; window.Content = scopeView; Pump();
        Check(Descendants(scopeView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Green && b.Width == 12), "scope legend color");
        vars.Set("shade", System.Drawing.Color.Blue); sample.Invoke(scope, [100.02]); Pump(); Check(Descendants(scopeView).OfType<Border>().Any(b => b.Background is SolidColorBrush brush && brush.Color == Colors.Blue && b.Width == 12), "scope legend live color");
        var scopeSaved = ((ITreeItemDocumentData)scope).SaveDocumentData(); var scopeCopy = ((TreeItemNode)scope).Copy(); Check(((ITreeItemDocumentData)scopeCopy).SaveDocumentData().GetRawText() == scopeSaved.GetRawText(), "scope legend save copy");
        using var scopeLegacy = JsonDocument.Parse(scopeSaved.GetRawText().Replace(",\"ShowLegend\":true", "")); ((ITreeItemDocumentData)scopeCopy).LoadDocumentData(scopeLegacy.RootElement); Check(!(bool)Get(scopeCopy, "ShowLegend")!, "old scope legend default");
        ((IDocumentItemDetached)xy).OnDocumentItemDetached(); life.OnDocumentItemDetached(); window.Close(); Console.WriteLine("Legend and multi-input VU verification passed.");
    }
}
