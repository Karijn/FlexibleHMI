using FlexibleHMI.Interfaces;
using Inputs.UI.JoyStick;
using Inputs.UI.Potmeter;
using Inputs.UI.Slider;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Interfaces;
using System.Windows.Media;

namespace Inputs.UI;

/// <summary>Registers the property-bound input controls and their document templates.</summary>
public sealed class AddInRegistrator : IAddInRegistrator
{
    /// <summary>Registers keyed tree-item options and nodes for each supported input kind.</summary>
    /// <param name="services">The service collection to configure.</param>
    public void RegisterAddIn(IServiceCollection services)
    {
        Register<ComboBoxViewModel>(services, "input-combobox", "Combo box", variables => new ComboBoxViewModel(variables));
        Register<CheckBoxViewModel>(services, "input-checkbox", "Check box", variables => new CheckBoxViewModel(variables));
        Register<RadioButtonSetViewModel>(services, "input-radiobuttonset", "Radio button set", variables => new RadioButtonSetViewModel(variables));
        Register<OnOffButtonViewModel>(services, "input-onoff", "On-off button", variables => new OnOffButtonViewModel(variables));
        Register<JoystickViewModel>(services, "input-joystick", "Joystick", variables => new JoystickViewModel(variables));
        Register<PotentiometerViewModel>(services, "input-potentiometer", "Potentiometer", variables => new PotentiometerViewModel(variables));
        Register<SliderViewModel>(services, "input-slider", "Slider", variables => new SliderViewModel(variables));

        services.AddMainAndDetail<ComboBoxViewModel>("InputDetails", "DefaultDetailsTemplate");
        services.AddMainAndDetail<CheckBoxViewModel>("InputDetails", "DefaultDetailsTemplate");
        services.AddMainAndDetail<RadioButtonSetViewModel>("InputDetails", "DefaultDetailsTemplate");
        services.AddMainAndDetail<OnOffButtonViewModel>("InputDetails", "DefaultDetailsTemplate");
        services.AddMainAndDetail<PotentiometerViewModel>("PotentiometerDetails", "DefaultDetailsTemplate");
        services.AddMainAndDetail<SliderViewModel>("SliderDetails", "DefaultDetailsTemplate");
        services.AddMainAndDetail<JoystickViewModel>("JoystickDetails", "DefaultDetailsTemplate");
    }

    private static void Register<T>(IServiceCollection services, string key, string title, Func<IVariableHandler, T> create) where T : InputViewModelBase
    {
        services.AddKeyedTransient<TreeItemOption>(key, (provider, _) =>
            new TreeItemOption(key, title, "Inputs", "Property-bound input control", TreeItemOption.CreateDiamondImage(Colors.SteelBlue), provider));
        services.AddKeyedTransient<TreeItemNode>(key, (provider, _) => create(provider.GetRequiredService<IVariableHandler>()));
    }

    /// <summary>Registers the input editor view for input tree items.</summary>
    /// <param name="serviceProvider">The built service provider supplied by the add-in host.</param>
    /// <param name="dataTemplateManager">The manager that receives the view template registration.</param>
    public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
    {
        dataTemplateManager.RegisterDataTemplate<ComboBoxViewModel, ComboBoxView>();
        dataTemplateManager.RegisterDataTemplate<CheckBoxViewModel, CheckBoxView>();
        dataTemplateManager.RegisterDataTemplate<RadioButtonSetViewModel, RadioButtonSetView>();
        dataTemplateManager.RegisterDataTemplate<OnOffButtonViewModel, OnOffButtonView>();
        dataTemplateManager.RegisterDataTemplate<JoystickViewModel, JoystickView>();
        dataTemplateManager.RegisterDataTemplate<PotentiometerViewModel, PotentiometerView>();
        dataTemplateManager.RegisterDataTemplate<SliderViewModel, SliderView>();

        dataTemplateManager.RegisterKeyedTemplate<ComboBoxView>("ComboBoxMain");
        dataTemplateManager.RegisterKeyedTemplate<CheckBoxView>("CheckBoxMain");
        dataTemplateManager.RegisterKeyedTemplate<RadioButtonSetView>("RadioButtonSetMain");
        dataTemplateManager.RegisterKeyedTemplate<OnOffButtonView>("OnOffButtonMain");
        dataTemplateManager.RegisterKeyedTemplate<InputDetails>("InputDetails");
        dataTemplateManager.RegisterKeyedTemplate<PotentiometerDetails>("PotentiometerDetails");
        dataTemplateManager.RegisterKeyedTemplate<SliderDetails>("SliderDetails");
        dataTemplateManager.RegisterKeyedTemplate<JoystickDetails>("JoystickDetails");

    }
}
