using Containers.Interfaces;
using FlexibleHMI.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace Containers.UI;

/// <summary>A container that arranges its child controls in a grid with configurable placement per control.</summary>
internal sealed class GridContainerViewModel : TreeItemNode, ITreeItemDocumentData, IDocumentDataChangeNotifier, IGridDropTarget
{
    private readonly Queue<GridCellPlacement> _pending = new();
    private int _rowCount = 2;
    private int _columnCount = 2;
    private bool _showGrid;
    private bool _showGrabber = true;
    private bool _equalColumnWidths = true;
    private bool _equalRowHeights = true;
    private double _width;
    private double _height;

    public GridContainerViewModel()
    {
        Name = "Grid Container";
        Description = "Holds controls in rows and columns.";
        Image = TreeItemOption.CreateDiamondImage(Colors.Goldenrod);
        Children.CollectionChanged += OnChildrenChanged;
    }

    public event EventHandler? DocumentDataChanged;

    public override string Kind => GridContainerCreator.KindName;
    public override int MaxChildren => -1;
    public override string ChildRule => "Any number of controls";
    public override bool CanAcceptChildren => true;

    /// <summary>The placement of every child, in child order.</summary>
    public ObservableCollection<GridCellViewModel> Cells { get; } = [];

    public int RowCount
    {
        get => _rowCount;
        set
        {
            if (value < 1 || value > GridLimits.MaxRows) throw new InvalidOperationException($"Row count must be between 1 and {GridLimits.MaxRows}.");
            var required = Cells.Select(cell => cell.Row + cell.RowSpan).DefaultIfEmpty(1).Max();
            if (value < required) throw new InvalidOperationException($"Row count must be at least {required} to keep every control inside the grid.");
            if (SetProperty(ref _rowCount, value)) NotifyDocumentChanged();
        }
    }

    public int ColumnCount
    {
        get => _columnCount;
        set
        {
            if (value < 1 || value > GridLimits.MaxColumns) throw new InvalidOperationException($"Column count must be between 1 and {GridLimits.MaxColumns}.");
            var required = Cells.Select(cell => cell.Column + cell.ColumnSpan).DefaultIfEmpty(1).Max();
            if (value < required) throw new InvalidOperationException($"Column count must be at least {required} to keep every control inside the grid.");
            if (SetProperty(ref _columnCount, value)) NotifyDocumentChanged();
        }
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set { if (SetProperty(ref _showGrid, value)) NotifyDocumentChanged(); }
    }

    public bool ShowGrabber
    {
        get => _showGrabber;
        set { if (SetProperty(ref _showGrabber, value)) NotifyDocumentChanged(); }
    }

    public bool EqualColumnWidths
    {
        get => _equalColumnWidths;
        set { if (SetProperty(ref _equalColumnWidths, value)) NotifyDocumentChanged(); }
    }

    public bool EqualRowHeights
    {
        get => _equalRowHeights;
        set { if (SetProperty(ref _equalRowHeights, value)) NotifyDocumentChanged(); }
    }

    public double Width
    {
        get => _width;
        set => SetDimension(ref _width, value, nameof(Width));
    }

    public double Height
    {
        get => _height;
        set => SetDimension(ref _height, value, nameof(Height));
    }

    private void SetDimension(ref double field, double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name, "Size must be a finite non-negative number; 0 means automatic.");
        if (SetProperty(ref field, value, name)) NotifyDocumentChanged();
    }

    internal void NotifyPlacementChanged() => NotifyDocumentChanged();

    public bool ContainsCell(int row, int column) => row >= 0 && row < RowCount && column >= 0 && column < ColumnCount;

    public IReadOnlyList<TreeItemNode> GetChildrenAt(int row, int column, TreeItemNode? exclude) => Cells
        .Where(cell => !ReferenceEquals(cell.Node, exclude) && row >= cell.Row && row < cell.Row + cell.RowSpan &&
                       column >= cell.Column && column < cell.Column + cell.ColumnSpan)
        .Select(cell => cell.Node).ToArray();

    public void SwapChildren(TreeItemNode child, TreeItemNode other, int row, int column)
    {
        var source = Cells.Single(cell => ReferenceEquals(cell.Node, child));
        var destination = Cells.Single(cell => ReferenceEquals(cell.Node, other));
        var original = source.Placement;
        var nextSource = GridPlacementRules.FitSpans(original with { Row = row, Column = column }, RowCount, ColumnCount);
        var nextDestination = GridPlacementRules.FitSpans(destination.Placement with { Row = original.Row, Column = original.Column }, RowCount, ColumnCount);
        var changed = source.SetPlacement(nextSource);
        changed |= destination.SetPlacement(nextDestination);
        if (changed) NotifyDocumentChanged();
    }

    public (int RowSpan, int ColumnSpan) GetChildSpan(TreeItemNode child)
    {
        var cell = Cells.Single(item => ReferenceEquals(item.Node, child));
        return (cell.RowSpan, cell.ColumnSpan);
    }

    public void PositionChild(TreeItemNode child, int row, int column, (int RowSpan, int ColumnSpan)? span = null)
    {
        var cell = Cells.Single(item => ReferenceEquals(item.Node, child));
        cell.MoveTo(row, column, span);
    }

    private void NotifyDocumentChanged() => DocumentDataChanged?.Invoke(this, EventArgs.Empty);

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (var i = 0; i < e.NewItems!.Count; i++)
                    Cells.Insert(e.NewStartingIndex + i, CreateCell((TreeItemNode)e.NewItems[i]!));
                break;
            case NotifyCollectionChangedAction.Remove:
                for (var i = 0; i < e.OldItems!.Count; i++)
                    Cells.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                for (var i = 0; i < e.NewItems!.Count; i++)
                    Cells[e.NewStartingIndex + i] = new GridCellViewModel(this, (TreeItemNode)e.NewItems[i]!, Cells[e.NewStartingIndex + i].Placement);
                break;
            case NotifyCollectionChangedAction.Move:
                for (var i = 0; i < e.NewItems!.Count; i++)
                    Cells.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            default:
                var previous = Cells.ToDictionary(cell => cell.Node);
                Cells.Clear();
                foreach (var child in Children)
                    Cells.Add(previous.TryGetValue(child, out var cell) ? cell : CreateCell(child));
                break;
        }

        NotifyDocumentChanged();
    }

    private GridCellViewModel CreateCell(TreeItemNode node)
    {
        if (_pending.Count > 0)
            return new GridCellViewModel(this, node, GridPlacementRules.Clamp(_pending.Dequeue(), RowCount, ColumnCount));

        var free = GridPlacementRules.FindFreeCell(Cells.Select(cell => cell.Placement), RowCount, ColumnCount);
        if (free is null && RowCount < GridLimits.MaxRows)
        {
            free = new GridCellPlacement(RowCount, 0);
            SetProperty(ref _rowCount, RowCount + 1, nameof(RowCount));
        }

        return new GridCellViewModel(this, node, free ?? new GridCellPlacement(0, 0));
    }

    public override TreeItemNode Copy()
    {
        var copy = new GridContainerViewModel();
        Copy(copy);
        copy.LoadDocumentData(SaveDocumentData());
        return copy;
    }

    public JsonElement SaveDocumentData() => JsonSerializer.SerializeToElement(new Settings(RowCount, ColumnCount,
        Cells.Select(cell => new CellSettings(cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan)).ToList(), ShowGrid, Width, Height, ShowGrabber,
        EqualColumnWidths, EqualRowHeights));

    public void LoadDocumentData(JsonElement data)
    {
        var settings = data.Deserialize<Settings>() ?? throw new InvalidDataException("Invalid grid container settings.");
        if (settings.RowCount < 1 || settings.RowCount > GridLimits.MaxRows || settings.ColumnCount < 1 || settings.ColumnCount > GridLimits.MaxColumns ||
            !double.IsFinite(settings.Width) || settings.Width < 0 || !double.IsFinite(settings.Height) || settings.Height < 0)
            throw new InvalidDataException("Invalid grid container size.");

        var placements = (settings.Cells ?? []).Select(cell => cell is null
            ? throw new InvalidDataException("Invalid grid container cell.")
            : new GridCellPlacement(cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan)).ToList();
        if (placements.Any(placement => GridPlacementRules.Validate(placement, settings.RowCount, settings.ColumnCount) is not null))
            throw new InvalidDataException("A grid container cell is outside the grid.");

        SetProperty(ref _rowCount, settings.RowCount, nameof(RowCount));
        SetProperty(ref _columnCount, settings.ColumnCount, nameof(ColumnCount));
        ShowGrid = settings.ShowGrid;
        Width = settings.Width;
        Height = settings.Height;
        ShowGrabber = settings.ShowGrabber;
        EqualColumnWidths = settings.EqualColumnWidths;
        EqualRowHeights = settings.EqualRowHeights;

        // Children are attached after their container's data is loaded, so unmatched placements wait for them.
        _pending.Clear();
        for (var i = 0; i < placements.Count; i++)
        {
            if (i < Cells.Count) Cells[i].SetPlacement(placements[i]);
            else _pending.Enqueue(placements[i]);
        }
    }

    private sealed record Settings(int RowCount, int ColumnCount, List<CellSettings>? Cells, bool ShowGrid = false,
        double Width = 0, double Height = 0, bool ShowGrabber = true, bool EqualColumnWidths = true, bool EqualRowHeights = true);
    private sealed record CellSettings(int Row, int Column, int RowSpan, int ColumnSpan);
}
