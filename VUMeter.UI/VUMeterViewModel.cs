using CommunityToolkit.Mvvm.ComponentModel;
using FlexibleHMI.Interfaces;
using PropertyManager.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace VUMeter.UI;

internal sealed partial class VUMeterViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier
{
    private readonly IVariableHandler _variableHandler;
    private string _selectedPropertyName = string.Empty;
    private double _needleAngle;
    private string _status = "Select a numeric property.";

    public VUMeterViewModel(IVariableHandler variableHandler)
    {
        _variableHandler = variableHandler ?? throw new ArgumentNullException(nameof(variableHandler));
        Name = "VU Meter";
        Description = "Display a numeric property as a needle angle.";
        Image = VUMeterCreator.GetIcon();
        _variableHandler.VariableChanged += OnVariableChanged;
        RefreshPropertyNames();
    }

    public event EventHandler? DocumentDataChanged;

    public double MaximumAngle { get => field; set => SetProperty(ref field, value); } = 50;

    public ObservableCollection<string> NumericPropertyNames { get; } = [];

    public string SelectedPropertyName
    {
        get => _selectedPropertyName;
        set
        {
            var propertyName = value?.Trim() ?? string.Empty;
            if (!SetProperty(ref _selectedPropertyName, propertyName))
            {
                return;
            }

            UpdateNeedle();
            DocumentDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public double NeedleAngle
    {
        get => _needleAngle;
        private set => SetProperty(ref _needleAngle, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsPeakActive => NeedleAngle >= MaximumAngle - 5;

    public override string Kind => VUMeterCreator.jsonkey;

    public override int MaxChildren => 0;

    public override string ChildRule => "No Children here";

    public override bool CanAcceptChildren => false;

    public override TreeItemNode Copy()
    {
        var copy = new VUMeterViewModel(_variableHandler);
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(
        new VUMeterDocumentSettings(SelectedPropertyName));

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<VUMeterDocumentSettings>()
            ?? throw new InvalidDataException("The document contains invalid VU meter settings.");
        SelectedPropertyName = settings.PropertyName ?? string.Empty;
    }

    public override void StopRuntimeActivity() =>
        _variableHandler.VariableChanged -= OnVariableChanged;

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyVariableChange(change);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => ApplyVariableChange(change)));
    }

    private void ApplyVariableChange(VariableChanged change)
    {
        RefreshPropertyNames();
        if (string.Equals(change.Name, SelectedPropertyName, StringComparison.Ordinal))
        {
            UpdateNeedle();
        }
    }

    private void RefreshPropertyNames()
    {
        var propertyNames = _variableHandler.GetVariableNames<double>()
            .Concat(_variableHandler.GetVariableNames<int>())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var availableNames = propertyNames.ToHashSet(StringComparer.Ordinal);
        for (var index = NumericPropertyNames.Count - 1; index >= 0; index--)
        {
            if (!availableNames.Contains(NumericPropertyNames[index]))
            {
                NumericPropertyNames.RemoveAt(index);
            }
        }

        foreach (var name in propertyNames)
        {
            if (NumericPropertyNames.Contains(name, StringComparer.Ordinal))
            {
                continue;
            }

            var index = 0;
            while (index < NumericPropertyNames.Count &&
                   StringComparer.Ordinal.Compare(NumericPropertyNames[index], name) < 0)
            {
                index++;
            }

            NumericPropertyNames.Insert(index, name);
        }

        if (string.IsNullOrEmpty(SelectedPropertyName))
        {
            if (NumericPropertyNames.Count > 0)
            {
                SelectedPropertyName = NumericPropertyNames[0];
            }
            else
            {
                UpdateNeedle();
            }
        }
        else if (!NumericPropertyNames.Contains(SelectedPropertyName, StringComparer.Ordinal))
        {
            SelectedPropertyName = string.Empty;
        }
        else
        {
            UpdateNeedle();
        }
    }

    private void UpdateNeedle()
    {
        if (string.IsNullOrWhiteSpace(SelectedPropertyName))
        {
            NeedleAngle = 0;
            OnPropertyChanged(nameof(IsPeakActive));
            Status = NumericPropertyNames.Count == 0
                ? "Add an Integer or Double property in Property Manager."
                : "Select a numeric property.";
            return;
        }

        double angle;
        if (_variableHandler.TryGet<double>(SelectedPropertyName, out var number) && double.IsFinite(number))
        {
            angle = number;
        }
        else if (_variableHandler.TryGet<int>(SelectedPropertyName, out var integer))
        {
            angle = integer;
        }
        else
        {
            NeedleAngle = 0;
            OnPropertyChanged(nameof(IsPeakActive));
            Status = $"Property '{SelectedPropertyName}' is unavailable or not numeric.";
            return;
        }

        NeedleAngle = Math.Clamp(angle, -MaximumAngle, MaximumAngle);
        OnPropertyChanged(nameof(IsPeakActive));
        Status = $"{SelectedPropertyName}: {angle:0.##}°";
    }

    private sealed record VUMeterDocumentSettings(string? PropertyName);
}
