using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexibleHMI.Interfaces;
using Inputs.UI;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager;
using PropertyManager.Interfaces;

internal static class Program
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static object? Get(object item, string name) => item.GetType().GetProperty(name)!.GetValue(item);
    private static void Set(object item, string name, object value) => item.GetType().GetProperty(name)!.SetValue(item, value);
    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        yield return node;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(node, i))) yield return child;
    }

    [STAThread]
    private static void Main()
    {
        _ = new Application();
        var variables = new VariableHandler();
        variables.Set("flag", false); variables.Set("choice", "Option 1"); variables.Set("x", 5); variables.Set("y", 0.5);
        var services = new ServiceCollection();
        services.AddSingleton<IVariableHandler>(variables);
        var addin = new AddInRegistrator(); addin.RegisterAddIn(services);
        using var provider = services.BuildServiceProvider();
        addin.InitializeAddIn(provider, new DataTemplateManager());
        var cases = new[] {
            ("input-combobox", "ComboBoxViewModel", "ComboBox", "choice"),
            ("input-checkbox", "CheckBoxViewModel", "CheckBox", "flag"),
            ("input-radiobuttonset", "RadioButtonSetViewModel", "RadioButton", "choice"),
            ("input-onoff", "OnOffButtonViewModel", "ToggleButton", "flag"),
            ("input-joystick", "JoystickViewModel", "JoystickControl", "x"),
            ("input-potentiometer", "PotentiometerViewModel", "PotentiometerControl", "x"),
            ("input-slider", "SliderViewModel", "Slider", "x")
        };
        foreach (var (key, type, control, name) in cases)
        {
            var node = provider.GetRequiredKeyedService<TreeItemNode>(key);
            Assert(node.Kind == key && node.GetType().Name == type, key + " registration");
            var data = (ITreeItemDocumentData)node;
            data.LoadDocumentData(JsonDocument.Parse("{\"PropertyName\":\"" + name + "\",\"YPropertyName\":\"y\",\"OptionsText\":\"Option 1\\nOption 2\",\"Minimum\":0,\"Maximum\":10}").RootElement);
            Assert((bool)Get(node, "IsAvailable")!, key + " availability");
            var copy = node.Copy();
            Assert(copy.GetType() == node.GetType() && ((ITreeItemDocumentData)copy).SaveDocumentData().GetRawText() == data.SaveDocumentData().GetRawText(), key + " copy");
            var presenter = new ContentControl { Content = node };
            presenter.Measure(new Size(1000, 1000)); presenter.Arrange(new Rect(0, 0, 1000, 1000)); presenter.UpdateLayout();
            var elements = Descendants(presenter).ToArray();
            Assert(elements.Any(e => e is InputView), key + " editor template");
            Assert(elements.Any(e => e.GetType().Name == control), key + " control template");
            ((IDocumentItemDetached)node).OnDocumentItemReattached();
            variables.Remove(name);
            Assert(!(bool)Get(node, "IsAvailable")!, key + " removal");
            if (name == "flag") variables.Set(name, false);
            else if (name == "choice") variables.Set(name, "Option 1");
            else variables.Set(name, 5);
            Assert((bool)Get(node, "IsAvailable")!, key + " reappearance");
            if (name == "flag") { Set(node, "BooleanValue", true); Assert(variables.Get<bool>(name), key + " write"); variables.Set(name, false); }
            if (name == "choice") { Set(node, "SelectedOption", "Option 2"); Assert(variables.Get<string>(name) == "Option 2", key + " write"); variables.Set(name, "Option 1"); }
            if (name == "x") { Set(node, "NumericValue", 8.6); Assert(variables.Get<int>(name) == 9, key + " integer write"); variables.Set(name, 5); }
            if (key == "input-joystick") { Set(node, "YValue", 2.5); Assert(variables.Get<double>("y") == 2.5, "joystick Y write"); }
            ((IDocumentItemDetached)node).OnDocumentItemDetached();
            variables.Remove(name);
            Assert((bool)Get(node, "IsAvailable")!, key + " detached update ignored");
            ((IDocumentItemDetached)node).OnDocumentItemReattached();
            Assert(!(bool)Get(node, "IsAvailable")!, key + " reattach refresh");
            ((IDocumentItemDetached)node).OnDocumentItemDetached();
            if (name == "flag") variables.Set(name, false);
            else if (name == "choice") variables.Set(name, "Option 1");
            else variables.Set(name, 5);
            Console.WriteLine(key + " passed");
        }
    }
}
