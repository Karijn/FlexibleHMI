using System.Windows;
using System.Windows.Controls;

namespace Containers.UI;

/// <summary>A grid with configurable row and column counts and sizing modes.</summary>
public sealed class GridContainerPanel : Grid
{
    public static readonly DependencyProperty RowCountProperty = DependencyProperty.Register(
        nameof(RowCount), typeof(int), typeof(GridContainerPanel), new PropertyMetadata(1, OnLayoutSettingsChanged));

    public static readonly DependencyProperty ColumnCountProperty = DependencyProperty.Register(
        nameof(ColumnCount), typeof(int), typeof(GridContainerPanel), new PropertyMetadata(1, OnLayoutSettingsChanged));

    public static readonly DependencyProperty EqualRowHeightsProperty = DependencyProperty.Register(
        nameof(EqualRowHeights), typeof(bool), typeof(GridContainerPanel), new PropertyMetadata(true, OnLayoutSettingsChanged));

    public static readonly DependencyProperty EqualColumnWidthsProperty = DependencyProperty.Register(
        nameof(EqualColumnWidths), typeof(bool), typeof(GridContainerPanel), new PropertyMetadata(true, OnLayoutSettingsChanged));

    public GridContainerPanel() => Rebuild();

    public int RowCount { get => (int)GetValue(RowCountProperty); set => SetValue(RowCountProperty, value); }
    public int ColumnCount { get => (int)GetValue(ColumnCountProperty); set => SetValue(ColumnCountProperty, value); }
    public bool EqualRowHeights { get => (bool)GetValue(EqualRowHeightsProperty); set => SetValue(EqualRowHeightsProperty, value); }
    public bool EqualColumnWidths { get => (bool)GetValue(EqualColumnWidthsProperty); set => SetValue(EqualColumnWidthsProperty, value); }

    private static void OnLayoutSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((GridContainerPanel)d).Rebuild();

    private void Rebuild()
    {
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        var rowHeight = EqualRowHeights ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        var columnWidth = EqualColumnWidths ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        for (var i = 0; i < Math.Max(1, RowCount); i++) RowDefinitions.Add(new RowDefinition { Height = rowHeight });
        for (var i = 0; i < Math.Max(1, ColumnCount); i++) ColumnDefinitions.Add(new ColumnDefinition { Width = columnWidth });
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (!EqualRowHeights) return base.MeasureOverride(constraint);

        // Keep row constraints stable between passes so layout can settle and popups can be positioned.
        base.MeasureOverride(new Size(constraint.Width, double.PositiveInfinity));
        var rowHeight = Children.Cast<UIElement>()
            .Select(child => child.DesiredSize.Height / Math.Min(GetRowSpan(child), RowDefinitions.Count - Math.Min(GetRow(child), RowDefinitions.Count - 1)))
            .DefaultIfEmpty(0).Max();
        foreach (var row in RowDefinitions)
        {
            row.MinHeight = Math.Min(rowHeight, constraint.Height / RowDefinitions.Count);
            row.MaxHeight = constraint.Height / RowDefinitions.Count;
        }
        return base.MeasureOverride(constraint);
    }

    /// <summary>Finds the visible cell at a position in this panel.</summary>
    public bool TryGetCellAt(Point position, out int row, out int column)
    {
        row = FindTrack(position.Y, RowDefinitions.Select(definition => definition.ActualHeight));
        column = FindTrack(position.X, ColumnDefinitions.Select(definition => definition.ActualWidth));
        return row >= 0 && column >= 0;
    }

    private static int FindTrack(double position, IEnumerable<double> sizes)
    {
        if (!double.IsFinite(position) || position < 0) return -1;
        var end = 0.0;
        var index = 0;
        var lastVisible = -1;
        foreach (var size in sizes)
        {
            end += size;
            if (size > 0)
            {
                lastVisible = index;
                if (position < end) return index;
            }
            index++;
        }
        return position == end ? lastVisible : -1;
    }
}
