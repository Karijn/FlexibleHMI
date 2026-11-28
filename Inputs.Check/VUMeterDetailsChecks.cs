using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager;
using PropertyManager.Interfaces;
using VUMeter.UI;

internal static class VUMeterDetailsChecks
{
    private static object? Get(object item, string property) => item.GetType().GetProperty(property)!.GetValue(item);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

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

    public static void Run()
    {
        var variables = new VariableHandler();
        variables.Set("level", 20d);
        variables.Set("other", -10d);
        variables.Set("legend", true);
        var services = new ServiceCollection();
        services.AddSingleton<IVariableHandler>(variables);
        var resources = new ResourceDictionary();
        services.AddSingleton(resources);
        var registrator = new AddinRegistrator();
        registrator.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        registrator.InitializeAddIn(provider, new DataTemplateManager(resources));
        var type = typeof(VUMeterView).Assembly.GetType("VUMeter.UI.VUMeterViewModel", true)!;
        var model = (TreeItemNode)Activator.CreateInstance(type, variables)!;
        var registrations = provider.GetServices<ViewTemplateRegistration>().Where(item => item.ViewModelType == type).ToArray();
        Check(registrations.Length == 2 && registrations.Any(item => item.View == ViewKind.MainView && item.Template.LoadContent() is VUMeterView)
            && registrations.Any(item => item.View == ViewKind.DetailsView && item.Template.LoadContent() is VUMeterDetails), "meter main and details template registration");
        Check(Application.Current.Resources.Contains(new DataTemplateKey(type)), "meter implicit template remains registered for containers");
        ((IDocumentItemDetached)model).OnDocumentItemReattached();
        var view = new VUMeterView { DataContext = model, Width = 500, Height = 300 };
        var details = new VUMeterDetails { DataContext = model, Width = 300 };
        var layout = new StackPanel { Orientation = Orientation.Horizontal };
        layout.Children.Add(view);
        layout.Children.Add(details);
        var window = new Window { Content = layout, Width = 900, Height = 700, ShowInTaskbar = false };
        window.Show();
        Pump();

        Check(!Descendants(view).OfType<Button>().Any() && !Descendants(view).OfType<ComboBox>().Any(), "settings are absent from meter view");
        var add = Descendants(details).OfType<Button>().Single(button => Equals(button.Content, "Add input"));
        ((IInvokeProvider)new ButtonAutomationPeer(add)).Invoke();
        Pump();
        var inputs = (IList)Get(model, "Inputs")!;
        Check(inputs.Count == 2, "add input through details");
        var second = inputs[1]!;
        var property = Descendants(details).OfType<ComboBox>().Single(box => box.Name == "InputPropertyEditor" && ReferenceEquals(box.DataContext, second));
        property.Text = "level";
        property.GetBindingExpression(ComboBox.TextProperty)!.UpdateSource();
        var color = Descendants(details).OfType<ComboBox>().Single(box => box.Name == "InputColorEditor" && ReferenceEquals(box.DataContext, second));
        color.SelectedItem = "Red";
        Pump();
        Check((string)Get(second, "PropertyName")! == "level" && (string)Get(second, "Color")! == "Red", "details update input property and color");
        Check(Descendants(view).OfType<Line>().Any(line => line.Stroke is SolidColorBrush brush && brush.Color == Colors.Red
            && line.RenderTransform is RotateTransform rotation && rotation.Angle == 20), "meter displays configured needle");
        variables.Set("level", 35d);
        Pump();
        Check((double)Get(second, "Angle")! == 35 && Descendants(view).OfType<Line>().Any(line => line.RenderTransform is RotateTransform rotation && rotation.Angle == 35), "meter needle follows live property");

        var legend = (CheckBox)details.FindName("ShowLegendEditor");
        legend.IsChecked = true;
        Pump();
        Check((bool)Get(model, "EffectiveShowLegend")!, "manual legend setting updates model");
        var legendProperty = (ComboBox)details.FindName("LegendPropertyEditor");
        legendProperty.Text = "legend";
        legendProperty.GetBindingExpression(ComboBox.TextProperty)!.UpdateSource();
        variables.Set("legend", false);
        Pump();
        Check(!(bool)Get(model, "EffectiveShowLegend")!, "legend property controls visibility");
        var saved = ((ITreeItemDocumentData)model).SaveDocumentData();
        Check(((ITreeItemDocumentData)model.Copy()).SaveDocumentData().GetRawText() == saved.GetRawText(), "meter settings survive copy");
        var restored = (TreeItemNode)Activator.CreateInstance(type, variables)!;
        ((ITreeItemDocumentData)restored).LoadDocumentData(saved);
        Check(((ITreeItemDocumentData)restored).SaveDocumentData().GetRawText() == saved.GetRawText(), "meter settings survive save and load");

        var remove = Descendants(details).OfType<Button>().Single(button => Equals(button.Content, "Remove") && ReferenceEquals(button.DataContext, second));
        ((IInvokeProvider)new ButtonAutomationPeer(remove)).Invoke();
        Pump();
        Check(inputs.Count == 1, "remove input through details");
        ((IDocumentItemDetached)model).OnDocumentItemDetached();
        window.Close();
    }
}
