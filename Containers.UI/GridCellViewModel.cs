using CommunityToolkit.Mvvm.ComponentModel;
using Containers.Interfaces;
using FlexibleHMI.Interfaces;

namespace Containers.UI;

/// <summary>Places one child control in a grid container.</summary>
internal sealed class GridCellViewModel : ObservableObject
{
    private readonly GridContainerViewModel _owner;
    private GridCellPlacement _placement;

    public GridCellViewModel(GridContainerViewModel owner, TreeItemNode node, GridCellPlacement placement)
    {
        _owner = owner;
        Node = node;
        _placement = placement;
    }

    public TreeItemNode Node { get; }
    public GridCellPlacement Placement => _placement;

    public int Row
    {
        get => _placement.Row;
        set => Update(_placement with { Row = value }, fitSpans: true);
    }

    public int Column
    {
        get => _placement.Column;
        set => Update(_placement with { Column = value }, fitSpans: true);
    }

    public int RowSpan
    {
        get => _placement.RowSpan;
        set => Update(_placement with { RowSpan = value }, fitSpans: false);
    }

    public int ColumnSpan
    {
        get => _placement.ColumnSpan;
        set => Update(_placement with { ColumnSpan = value }, fitSpans: false);
    }

    internal void MoveTo(int row, int column, (int RowSpan, int ColumnSpan)? span) => Update(_placement with
    {
        Row = row,
        Column = column,
        RowSpan = span?.RowSpan ?? RowSpan,
        ColumnSpan = span?.ColumnSpan ?? ColumnSpan
    }, fitSpans: true);

    private void Update(GridCellPlacement next, bool fitSpans)
    {
        // Moving a control shrinks its spans instead of rejecting the move, but the new position itself must be valid.
        if (fitSpans && GridPlacementRules.Validate(next with { RowSpan = 1, ColumnSpan = 1 }, _owner.RowCount, _owner.ColumnCount) is null)
            next = GridPlacementRules.FitSpans(next, _owner.RowCount, _owner.ColumnCount);

        if (GridPlacementRules.Validate(next, _owner.RowCount, _owner.ColumnCount) is { } error)
            throw new InvalidOperationException(error);

        if (SetPlacement(next)) _owner.NotifyPlacementChanged();
    }

    internal bool SetPlacement(GridCellPlacement next)
    {
        if (next == _placement) return false;
        _placement = next;
        OnPropertyChanged(nameof(Row));
        OnPropertyChanged(nameof(Column));
        OnPropertyChanged(nameof(RowSpan));
        OnPropertyChanged(nameof(ColumnSpan));
        return true;
    }
}
