using CommunityToolkit.Mvvm.Input;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace VUMeter.UI;

internal sealed partial class VUMeterViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IDocumentItemDetached
{
    private readonly IVariableHandler _variables;
    private readonly Dispatcher _dispatcher;
    private bool _detached = true;
    private int _version;
    private bool _showLegend;
    private string _showLegendProperty = "";
    private bool _autoSelect = true;
    /// <summary>
    /// Initializes a new instance of VUMeterViewModel.
    /// </summary>
    public VUMeterViewModel(IVariableHandler variables)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _dispatcher = Dispatcher.CurrentDispatcher;
        Name = "VU Meter";
        Description = "Display numeric properties as needle angles.";
        Image = VUMeterCreator.GetIcon();
        Inputs.Add(new VUMeterInputViewModel(InputChanged));
        RefreshNames();
    }
    /// <summary>
    /// Occurs when meter settings change and the host should mark the document as modified.
    /// </summary>
    public event EventHandler? DocumentDataChanged;
    /// <summary>
    /// Gets the available numeric variable names that can be monitored by meter inputs.
    /// </summary>
    public ObservableCollection<string> NumericPropertyNames { get; } = [];
    /// <summary>
    /// Gets the available Boolean variable names used for conditional legend visibility.
    /// </summary>
    public ObservableCollection<string> BooleanPropertyNames { get; } = [];
    /// <summary>
    /// Gets the editable meter input rows.
    /// </summary>
    public ObservableCollection<VUMeterInputViewModel> Inputs { get; } = [];
    /// <summary>
    /// Gets the supported colors offered for meter inputs.
    /// </summary>
    public IReadOnlyList<string> Colors => XYPointViewModel.Colors;
    /// <summary>
    /// Gets the maximum needle angle, in degrees.
    /// </summary>
    public double MaximumAngle => 50;
    /// <summary>
    /// Gets or sets the manual legend visibility used when no visibility variable is selected.
    /// </summary>
    public bool ShowLegend
    {
        get => _showLegend;
        set { if (SetProperty(ref _showLegend, value)) { NotifyLegendChanged(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); } }
    }
    /// <summary>
    /// Gets or sets the optional Boolean variable name that controls legend visibility.
    /// </summary>
    public string ShowLegendProperty
    {
        get => _showLegendProperty;
        set { if (SetProperty(ref _showLegendProperty, value ?? "")) { NotifyLegendChanged(); DocumentDataChanged?.Invoke(this, EventArgs.Empty); } }
    }
    /// <summary>
    /// Gets the resolved legend visibility after applying the optional variable binding.
    /// </summary>
    public bool EffectiveShowLegend => LegendVisibility.Evaluate(_variables, ShowLegendProperty, ShowLegend).Visible;
    /// <summary>
    /// Gets the status message for the optional legend visibility binding.
    /// </summary>
    public string LegendStatus => LegendVisibility.Evaluate(_variables, ShowLegendProperty, ShowLegend).Status;
    private void NotifyLegendChanged()
    {
        OnPropertyChanged(nameof(EffectiveShowLegend));
        OnPropertyChanged(nameof(LegendStatus));
    }
    /// <summary>
    /// Gets the kind.
    /// </summary>
    public override string Kind => VUMeterCreator.jsonkey;
    /// <summary>
    /// Gets the max children.
    /// </summary>
    public override int MaxChildren => 0;
    /// <summary>
    /// Gets the child rule.
    /// </summary>
    public override string ChildRule => "No Children here";
    /// <summary>
    /// Gets the can accept children.
    /// </summary>
    public override bool CanAcceptChildren => false;

    [RelayCommand]
    private void AddInput()
    {
        var input = new VUMeterInputViewModel(InputChanged) { Color = Colors[Inputs.Count % Colors.Count] };
        Inputs.Add(input);
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveInput(VUMeterInputViewModel? input)
    {
        if (input is null || !Inputs.Remove(input)) return;
        _autoSelect = false;
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }
    /// <inheritdoc/>
    public override TreeItemNode Copy()
    {
        var copy = new VUMeterViewModel(_variables);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }
    /// <summary>
    /// Saves document data.
    /// </summary>
    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new Settings(null,
        Inputs.Select(i => new InputSettings(i.PropertyName, i.Color)).ToList(), ShowLegend, ShowLegendProperty));
    /// <summary>
    /// Loads document data.
    /// </summary>
    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid VU meter settings.");
        if (settings.ShowLegendProperty is null || (settings.Inputs is not null && settings.Inputs.Any(i => i is null || i.PropertyName is null || !Colors.Contains(i.Color, StringComparer.Ordinal))))
            throw new InvalidDataException("Invalid VU meter input settings.");
        _autoSelect = false;
        Inputs.Clear();
        foreach (var saved in settings.Inputs ?? [new InputSettings(settings.PropertyName ?? string.Empty, "Black")])
        {
            var input = new VUMeterInputViewModel(InputChanged) { PropertyName = saved.PropertyName, Color = saved.Color };
            Inputs.Add(input);
            UpdateInput(input);
        }
        ShowLegend = settings.ShowLegend;
        ShowLegendProperty = settings.ShowLegendProperty;
    }
    /// <summary>
    /// Handles the document item detached notification.
    /// </summary>
    public void OnDocumentItemDetached()
    {
        _detached = true;
        Interlocked.Increment(ref _version);
        _variables.VariableChanged -= OnVariableChanged;
    }
    /// <summary>
    /// Handles the document item reattached notification.
    /// </summary>
    public void OnDocumentItemReattached()
    {
        if (!_detached) return;
        _detached = false;
        Interlocked.Increment(ref _version);
        _variables.VariableChanged += OnVariableChanged;
        RefreshNames();
        foreach (var input in Inputs) UpdateInput(input);
        NotifyLegendChanged();
    }

    private void InputChanged(VUMeterInputViewModel input)
    {
        _autoSelect = false;
        UpdateInput(input);
        DocumentDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var version = Volatile.Read(ref _version);
        void Apply()
        {
            if (_detached || version != Volatile.Read(ref _version)) return;
            if (change.OldValue is null || change.NewValue is null || change.OldValue.GetType() != change.NewValue.GetType()) RefreshNames();
            foreach (var input in Inputs)
                if (string.Equals(input.PropertyName, change.Name, StringComparison.Ordinal)) UpdateInput(input);
            if (string.Equals(ShowLegendProperty, change.Name, StringComparison.Ordinal)) NotifyLegendChanged();
        }
        if (_dispatcher.CheckAccess()) Apply();
        else _dispatcher.BeginInvoke(new Action(Apply));
    }

    private void RefreshNames()
    {
        var names = _variables.GetVariableNames<double>().Concat(_variables.GetVariableNames<int>())
            .Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        for (var index = NumericPropertyNames.Count - 1; index >= 0; index--)
            if (!names.Contains(NumericPropertyNames[index], StringComparer.Ordinal)) NumericPropertyNames.RemoveAt(index);
        foreach (var name in names)
            if (!NumericPropertyNames.Contains(name, StringComparer.Ordinal)) NumericPropertyNames.Add(name);
        var booleanNames = _variables.GetVariableNames<bool>().Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        for (var index = BooleanPropertyNames.Count - 1; index >= 0; index--)
            if (!booleanNames.Contains(BooleanPropertyNames[index], StringComparer.Ordinal)) BooleanPropertyNames.RemoveAt(index);
        foreach (var name in booleanNames)
            if (!BooleanPropertyNames.Contains(name, StringComparer.Ordinal)) BooleanPropertyNames.Add(name);
        if (_autoSelect && Inputs.Count == 1 && string.IsNullOrEmpty(Inputs[0].PropertyName) && names.Length > 0)
            Inputs[0].PropertyName = names[0];
    }

    private void UpdateInput(VUMeterInputViewModel input)
    {
        double? value = null;
        if (!string.IsNullOrWhiteSpace(input.PropertyName))
        {
            if (_variables.TryGet<double>(input.PropertyName, out var number) && double.IsFinite(number)) value = number;
            else if (_variables.TryGet<int>(input.PropertyName, out var integer)) value = integer;
        }
        input.Update(value, MaximumAngle);
    }

    private sealed record Settings(string? PropertyName = null, List<InputSettings>? Inputs = null, bool ShowLegend = false, string ShowLegendProperty = "");
    private sealed record InputSettings(string PropertyName, string Color);
}
