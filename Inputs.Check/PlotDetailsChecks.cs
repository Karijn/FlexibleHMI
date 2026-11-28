using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager;
using PropertyManager.Interfaces;
using VUMeter.UI;

internal static class PlotDetailsChecks
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static object? Get(object item, string name) => item.GetType().GetProperty(name)!.GetValue(item);
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private static void Click(Button button) { ((IInvokeProvider)new ButtonAutomationPeer(button)).Invoke(); Pump(); }
    private static void SetText(ComboBox editor, string value)
    {
        editor.Text = value;
        editor.GetBindingExpression(ComboBox.TextProperty)!.UpdateSource();
        Pump();
    }
    private static void SetText(TextBox editor, string value)
    {
        editor.Text = value;
        editor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Pump();
    }

    public static void Run()
    {
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var variables = new VariableHandler();
        variables.Set("x", 5d);
        variables.Set("y", 10d);
        variables.Set("legend", true);
        var services = new ServiceCollection();
        services.AddSingleton<IVariableHandler>(variables);
        var resources = new ResourceDictionary { ["DefaultDetailsTemplate"] = new DataTemplate() };
        services.AddSingleton(resources);
        var registrator = new AddinRegistrator();
        registrator.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        registrator.InitializeAddIn(provider, new DataTemplateManager(resources));

        foreach (var (typeName, viewType, detailsType, collectionName, addLabel) in new[]
        {
            ("XYControlViewModel", typeof(XYControlView), typeof(XYControlDetails), "Points", "Add point"),
            ("OscilloscopeViewModel", typeof(OscilloscopeView), typeof(OscilloscopeDetails), "Channels", "Add input")
        })
        {
            var type = typeof(XYPlot).Assembly.GetType("VUMeter.UI." + typeName, true)!;
            var model = (TreeItemNode)Activator.CreateInstance(type, variables)!;
            var registration = provider.GetServices<ViewTemplateRegistration>().Single(r => r.ViewModelType == type && r.View == ViewKind.MainView);
            Check(registration.Template.LoadContent().GetType() == detailsType, $"{typeName} right panel template");
            Check(Application.Current.Resources.Contains(new DataTemplateKey(type)), $"{typeName} implicit plot template");
            ((IDocumentItemDetached)model).OnDocumentItemReattached();
            var view = (FrameworkElement)Activator.CreateInstance(viewType)!;
            view.DataContext = model;
            var details = (FrameworkElement)registration.Template.LoadContent();
            details.DataContext = model;
            details.Width = 300;
            var content = new ContentControl { Content = model };
            var panel = new StackPanel();
            panel.Children.Add(view);
            panel.Children.Add(details);
            panel.Children.Add(content);
            var window = new Window { Content = panel, Width = 700, Height = 900, ShowInTaskbar = false };
            window.Show();
            Pump();
            Check(Descendants(content).Any(element => element.GetType() == viewType), $"{typeName} renders nested implicit view");
            Check(!Descendants(view).OfType<ComboBox>().Any() && !Descendants(view).OfType<TextBox>().Any(), $"{typeName} main plot has no configuration editors");
            var entries = (IList)Get(model, collectionName)!;
            Click(Descendants(details).OfType<Button>().Single(button => Equals(button.Content, addLabel)));
            Check(entries.Count == 2, $"{typeName} add through details");
            var entry = entries[1]!;
            if (details is XYControlDetails xy)
            {
                SetText((TextBox)xy.FindName("MinimumXEditor"), "-50");
                Check((double)Get(model, "MinimumX")! == -50, "XY bounds edit");
                SetText(Descendants(details).OfType<ComboBox>().Single(c => c.Name == "XPropertyEditor" && ReferenceEquals(c.DataContext, entry)), "x");
                SetText(Descendants(details).OfType<ComboBox>().Single(c => c.Name == "YPropertyEditor" && ReferenceEquals(c.DataContext, entry)), "y");
                var size = Descendants(details).OfType<TextBox>().Single(c => c.Name == "PointSizeEditor" && ReferenceEquals(c.DataContext, entry));
                SetText(size, "101");
                Check(Validation.GetHasError(size) && (double)Get(entry, "Size")! == 10, "XY size validation");
                SetText(size, "12");
                variables.Set("x", 15d);
                Pump();
                Check((Point?)Get(entry, "Position") == new Point(15, 10), "XY live position after editing");
            }
            else
            {
                var scope = (OscilloscopeDetails)details;
                var speed = (TextBox)scope.FindName("SpeedEditor");
                SetText(speed, "0");
                Check(Validation.GetHasError(speed), "oscilloscope speed validation");
                SetText(speed, "0.5");
                Check((double)Get(model, "Speed")! == 0.5, "oscilloscope timebase edit");
                SetText(Descendants(details).OfType<ComboBox>().Single(c => c.Name == "InputPropertyEditor" && ReferenceEquals(c.DataContext, entry)), "x");
                var amplitude = Descendants(details).OfType<TextBox>().Single(c => c.Name == "AmplitudeEditor" && ReferenceEquals(c.DataContext, entry));
                SetText(amplitude, "2");
                Check((double)Get(entry, "Amplitude")! == 2, "oscilloscope channel edit");
                var sample = type.GetMethod("Sample", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                sample.Invoke(model, [100d]);
                Check(((IList)Get(entry, "Samples")!).Count > 0, "oscilloscope samples configured input");
            }
            ((CheckBox)details.FindName("ShowLegendEditor")).IsChecked = true;
            Pump();
            Check((bool)Get(model, "EffectiveShowLegend")!, $"{typeName} manual legend edit");
            SetText((ComboBox)details.FindName("LegendPropertyEditor"), "legend");
            variables.Set("legend", false);
            Pump();
            Check(!(bool)Get(model, "EffectiveShowLegend")!, $"{typeName} legend property edit");
            variables.Set("legend", true);
            var saved = ((ITreeItemDocumentData)model).SaveDocumentData();
            Check(((ITreeItemDocumentData)model.Copy()).SaveDocumentData().GetRawText() == saved.GetRawText(), $"{typeName} copy persists settings");
            var restored = (TreeItemNode)Activator.CreateInstance(type, variables)!;
            ((ITreeItemDocumentData)restored).LoadDocumentData(saved);
            Check(((ITreeItemDocumentData)restored).SaveDocumentData().GetRawText() == saved.GetRawText(), $"{typeName} settings round trip");
            Click(Descendants(details).OfType<Button>().Single(button => Equals(button.Content, "Remove") && ReferenceEquals(button.DataContext, entry)));
            Check(entries.Count == 1, $"{typeName} remove through details");
            Click(Descendants(view).OfType<Button>().Single(button => Equals(button.Content, details is XYControlDetails ? "Clear trails" : "Clear")));
            ((IDocumentItemDetached)model).OnDocumentItemDetached();
            window.Close();
        }
    }
}
