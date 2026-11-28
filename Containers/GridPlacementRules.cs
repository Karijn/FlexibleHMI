using Containers.Interfaces;

namespace Containers;

/// <summary>Validates and computes control placements in a grid container.</summary>
public static class GridPlacementRules
{
    /// <summary>Checks a placement against the grid size.</summary>
    /// <returns>An error message, or null when the placement is valid.</returns>
    public static string? Validate(GridCellPlacement placement, int rowCount, int columnCount)
    {
        if (placement.Row < 0 || placement.Row >= rowCount) return $"Row must be between 0 and {rowCount - 1}.";
        if (placement.Column < 0 || placement.Column >= columnCount) return $"Column must be between 0 and {columnCount - 1}.";
        if (placement.RowSpan < 1 || placement.Row + (long)placement.RowSpan > rowCount) return $"Row span must be between 1 and {rowCount - placement.Row} for row {placement.Row}.";
        if (placement.ColumnSpan < 1 || placement.Column + (long)placement.ColumnSpan > columnCount) return $"Column span must be between 1 and {columnCount - placement.Column} for column {placement.Column}.";
        return null;
    }

    /// <summary>Reduces the spans so the placement fits inside the grid; the row and column must be valid.</summary>
    public static GridCellPlacement FitSpans(GridCellPlacement placement, int rowCount, int columnCount) => placement with
    {
        RowSpan = Math.Clamp(placement.RowSpan, 1, Math.Max(1, rowCount - placement.Row)),
        ColumnSpan = Math.Clamp(placement.ColumnSpan, 1, Math.Max(1, columnCount - placement.Column)),
    };

    /// <summary>Moves a placement into the grid and reduces its spans to fit.</summary>
    public static GridCellPlacement Clamp(GridCellPlacement placement, int rowCount, int columnCount) => FitSpans(placement with
    {
        Row = Math.Clamp(placement.Row, 0, rowCount - 1),
        Column = Math.Clamp(placement.Column, 0, columnCount - 1),
    }, rowCount, columnCount);

    /// <summary>Finds the first cell, scanning row by row, that is not covered by any placement.</summary>
    /// <returns>A one-cell placement, or null when every cell is covered.</returns>
    public static GridCellPlacement? FindFreeCell(IEnumerable<GridCellPlacement> occupied, int rowCount, int columnCount)
    {
        var covered = new bool[rowCount, columnCount];
        foreach (var item in occupied.Select(placement => Clamp(placement, rowCount, columnCount)))
        {
            for (var row = item.Row; row < item.Row + item.RowSpan; row++)
                for (var column = item.Column; column < item.Column + item.ColumnSpan; column++)
                    covered[row, column] = true;
        }

        for (var row = 0; row < rowCount; row++)
            for (var column = 0; column < columnCount; column++)
                if (!covered[row, column]) return new GridCellPlacement(row, column);
        return null;
    }
}
