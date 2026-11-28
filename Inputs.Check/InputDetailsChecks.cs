using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using FlexibleHMI.Interfaces;
using Inputs.UI;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager;
using PropertyManager.Interfaces;

internal static class InputDetailsChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static object? Get(object item, string property) => item.GetType().GetProperty(property)!.GetValue(item);

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    public static void Run()
    {
        var variables = new VariableHandler();
        variables.Set("choice", "One");
        variables.Set("switch", false);
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var services = new ServiceCollection();
        services.AddSingleton<IVariableHandler>(variables);
        var resources = new ResourceDictionary { ["DefaultDetailsTemplate"] = new DataTemplate() };
        services.AddSingleton(resources);
        var registrator = new Inputs.UI.AddInRegistrator();
        registrator.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        registrator.InitializeAddIn(provider, new DataTemplateManager(resources));

        foreach (var (kind, viewType, choice) in new[]
        {
            ("input-combobox", typeof(ComboBoxView), true),
            ("input-radiobuttonset", typeof(RadioButtonSetView), true),
            ("input-checkbox", typeof(CheckBoxView), false),
            ("input-onoff", typeof(OnOffButtonView), false)
        })
        {
            variables.Set("choice", "One");
            variables.Set("switch", false);
            var model = provider.GetRequiredKeyedService<TreeItemNode>(kind);
            var registrations = provider.GetServices<ViewTemplateRegistration>().Where(registration => registration.ViewModelType == model.GetType()).ToArray();
            Check(registrations.Length == 2 && registrations.Any(registration => registration.View == ViewKind.MainView && registration.Template.LoadContent().GetType() == viewType)
                && registrations.Any(registration => registration.View == ViewKind.DetailsView && registration.Template.LoadContent() is InputDetails), $"{kind} registers main and details views");
            Check(Application.Current.Resources.Contains(new DataTemplateKey(model.GetType())), $"{kind} retains implicit template");
            ((IDocumentItemDetached)model).OnDocumentItemReattached();
            var view = (FrameworkElement)Activator.CreateInstance(viewType)!;
            view.DataContext = model;
            var details = new InputDetails { DataContext = model, Width = 300 };
            var panel = new StackPanel();
            panel.Children.Add(view);
            panel.Children.Add(details);
            var nestedContent = new ContentControl { Content = model };
            panel.Children.Add(nestedContent);
            var window = new Window { Content = panel, Width = 500, Height = 600, ShowInTaskbar = false };
            window.Show();
            Pump();

            Check(Descendants(nestedContent).Any(element => element.GetType() == viewType), $"{kind} renders through its implicit template");
            Check(!(bool)Get(model, "IsAvailable")!, $"{kind} is disabled before property selection");
            Check(Descendants(view).OfType<TextBox>().All(box => box.Name != "OptionsEditor") &&
                !Descendants(view).OfType<ComboBox>().Any(box => box.IsEditable), $"{kind} has no configuration editors in main view");
            var editor = (ComboBox)details.FindName("PropertyEditor");
            Check(((IEnumerable)editor.ItemsSource).Cast<string>().Contains(choice ? "choice" : "switch"), $"{kind} property choices available");
            editor.Text = choice ? "choice" : "switch";
            editor.GetBindingExpression(ComboBox.TextProperty)!.UpdateSource();
            Pump();
            Check((bool)Get(model, "IsAvailable")!, $"{kind} enables after property selection");
            int dirty = 0;
            ((IDocumentDataChangeNotifier)model).DocumentDataChanged += (_, _) => dirty++;

            if (choice)
            {
                var options = Descendants(details).OfType<TextBox>().Single(box => box.Name == "OptionsEditor");
                options.Text = "One\nTwo";
                options.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                Pump();
                Check((string)Get(model, "OptionsText")! == "One\nTwo" && dirty == 1, $"{kind} option editing marks document dirty");
                if (view is ComboBoxView)
                {
                    var control = Descendants(view).OfType<ComboBox>().Single();
                    control.SelectedItem = "Two";
                }
                else
                {
                    var control = Descendants(view).OfType<RadioButton>().Single(button => Equals(button.Content, "Two"));
                    Check(control.Command is not null && Equals(control.CommandParameter, "Two"), "radio button command is bound");
                    control.Command!.Execute(control.CommandParameter);
                }
                Pump();
                Check(variables.TryGet<string>("choice", out var value) && value == "Two", $"{kind} interaction writes property");
                variables.Set("choice", "One");
                Pump();
                Check((string?)Get(model, "SelectedOption") == "One", $"{kind} follows external changes");
            }
            else
            {
                Check(!Descendants(details).OfType<TextBox>().Any(box => box.Name == "OptionsEditor"), $"{kind} hides choice options");
                var control = Descendants(view).OfType<ToggleButton>().Single();
                if (control is CheckBox checkBox) ((IToggleProvider)new CheckBoxAutomationPeer(checkBox)).Toggle();
                else ((IToggleProvider)new ToggleButtonAutomationPeer(control)).Toggle();
                Pump();
                Check(variables.TryGet<bool>("switch", out var value) && value, $"{kind} interaction writes property");
                variables.Set("switch", false);
                Pump();
                Check(control.IsChecked == false, $"{kind} follows external changes");
            }
            Check(dirty == (choice ? 1 : 0), $"{kind} runtime value changes do not dirty settings");
            var saved = ((ITreeItemDocumentData)model).SaveDocumentData();
            Check(((ITreeItemDocumentData)model.Copy()).SaveDocumentData().GetRawText() == saved.GetRawText(), $"{kind} settings survive copying");
            var restored = provider.GetRequiredKeyedService<TreeItemNode>(kind);
            ((ITreeItemDocumentData)restored).LoadDocumentData(saved);
            Check(((ITreeItemDocumentData)restored).SaveDocumentData().GetRawText() == saved.GetRawText(), $"{kind} settings survive save/load");
            variables.Remove(choice ? "choice" : "switch");
            Pump();
            Check(!(bool)Get(model, "IsAvailable")! && !string.IsNullOrEmpty((string)Get(model, "Status")!), $"{kind} reports missing property");
            ((IDocumentItemDetached)model).OnDocumentItemDetached();
            window.Close();
        }
    }
}
