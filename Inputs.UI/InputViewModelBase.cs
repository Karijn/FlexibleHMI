using FlexibleHMI.Interfaces;
using Inputs.UI.JoyStick;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;

namespace Inputs.UI;

internal abstract class InputViewModelBase : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IDocumentItemDetached
{
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private bool _detached = true;
    private int _version;
    private string _propertyName = "";
    private string _status = "";
    private bool _isAvailable;
    protected string SavedYPropertyName = "";
    protected string SavedOptionsText = "Option 1\nOption 2";
    protected double SavedMinimum;
    protected double SavedMaximum = 100;

    protected Color _foreColor = Colors.Red;
    public Color ForeColor
    {
        get => _foreColor;
        set
        {
            if (SetProperty(ref _foreColor, value))
                SettingsChanged();
        }
    }

    protected Color _backColor = Colors.SteelBlue;
    public Color BackColor
    {
        get => _backColor;
        set
        {
            if (SetProperty(ref _backColor, value))
                SettingsChanged();
        }
    }

    protected Color _backColorDisabled = Colors.LightGray;
    public Color BackColorDisabled
    {
        get => _backColorDisabled;
        set
        {
            if (SetProperty(ref _backColorDisabled, value))
                SettingsChanged();
        }
    }

    protected Color _borderColor = Colors.Black;
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            if (SetProperty(ref _borderColor, value))
                SettingsChanged();
        }
    }

    protected InputViewModelBase(IVariableHandler variables, string title)
    {
        Variables = variables;
        Name = title;
        Description = "Property-bound input control";
        Image = TreeItemOption.CreateDiamondImage(Colors.SteelBlue);
    }

    protected IVariableHandler Variables
    {
        get;
    }
    public override int MaxChildren => 0;
    public override string ChildRule => "No children";
    public override bool CanAcceptChildren => false;
    public event EventHandler? DocumentDataChanged;
    public ObservableCollection<string> PropertyNames { get; } = [];


    public string PropertyName
    {
        get => _propertyName;
        set
        {
            if (SetProperty(ref _propertyName, value?.Trim() ?? ""))
                SettingsChanged();
        }
    }
    public string Status
    {
        get => _status;
        protected set => SetProperty(ref _status, value);
    }
    public bool IsAvailable
    {
        get => _isAvailable;
        protected set => SetProperty(ref _isAvailable, value);
    }
    [Browsable(false)] public ChoiceInputViewModel? ChoiceSettings => this as ChoiceInputViewModel;
    [Browsable(false)] public NumericInputViewModel? NumericSettings => this as NumericInputViewModel;
    [Browsable(false)] public JoystickViewModel? JoystickSettings => this as JoystickViewModel;

    protected void InitializeInput()
    {
        RefreshNames();
        RefreshValue();
    }
    protected void SettingsChanged()
    {
        RefreshValue();
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }
    protected abstract IEnumerable<string> GetNames();
    protected abstract void RefreshValue();
    protected virtual bool IsRelevantVariable(string name) => name == PropertyName;
    protected virtual void SettingsLoaded()
    {
    }
    protected abstract InputViewModelBase CreateCopy();

    private void RefreshNames()
    {
        var ordered = GetNames().Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        for (var i = PropertyNames.Count - 1; i >= 0; i--)
            if (!ordered.Contains(PropertyNames[i], StringComparer.Ordinal))
                PropertyNames.RemoveAt(i);

        foreach (var name in ordered)
            if (!PropertyNames.Contains(name, StringComparer.Ordinal))
                PropertyNames.Add(name);
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var namesChanged = change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType();
        var version = Volatile.Read(ref _version);
        void Apply()
        {
            if (_detached || version != Volatile.Read(ref _version))
                return;
            if (namesChanged)
                RefreshNames();
            if (IsRelevantVariable(change.Name))
                RefreshValue();
        }
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.BeginInvoke(new Action(Apply));
    }

    public void OnDocumentItemDetached()
    {
        _detached = true;
        Interlocked.Increment(ref _version);
        Variables.VariableChanged -= OnVariableChanged;
    }
    public void OnDocumentItemReattached()
    {
        if (!_detached)
            return;
        _detached = false;
        Interlocked.Increment(ref _version);
        Variables.VariableChanged += OnVariableChanged;
        RefreshNames();
        RefreshValue();
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(
        new Settings(PropertyName, SavedYPropertyName, SavedOptionsText, SavedMinimum, SavedMaximum, BackColor, BackColorDisabled, ForeColor, BorderColor));

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid input settings.");
        if (settings.PropertyName is null ||
            settings.YPropertyName is null ||
            settings.OptionsText is null ||
            !double.IsFinite(settings.Minimum) ||
            !double.IsFinite(settings.Maximum) ||
            settings.Minimum >= settings.Maximum ||
            !double.IsFinite(settings.Maximum - settings.Minimum))
            throw new InvalidDataException("Invalid input settings or range.");

        var yPropertyName = settings.YPropertyName.Trim();
        var additionalSettingsChanged = SavedYPropertyName != yPropertyName ||
                                        SavedOptionsText != settings.OptionsText;
        SavedMinimum = settings.Minimum;
        SavedMaximum = settings.Maximum;
        BackColor = settings.BackColor;
        BackColorDisabled = settings.BackColorDisabled;
        ForeColor = settings.ForeColor;
        BorderColor = settings.BorderColor;
        SavedYPropertyName = yPropertyName;
        SavedOptionsText = settings.OptionsText;
        PropertyName = settings.PropertyName;
        SettingsLoaded();
        RefreshValue();
        if (additionalSettingsChanged)
            DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }
    public override TreeItemNode Copy()
    {
        var copy = CreateCopy();
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }
    private sealed record Settings(string PropertyName, string YPropertyName, string OptionsText, double Minimum, double Maximum, Color BackColor, Color BackColorDisabled, Color ForeColor, Color BorderColor);
}
