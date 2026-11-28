namespace Containers.Interfaces;

/// <summary>Describes where a control is placed in a grid container. Rows and columns are zero-based.</summary>
/// <param name="Row">The first row occupied by the control.</param>
/// <param name="Column">The first column occupied by the control.</param>
/// <param name="RowSpan">The number of rows occupied by the control.</param>
/// <param name="ColumnSpan">The number of columns occupied by the control.</param>
public readonly record struct GridCellPlacement(int Row, int Column, int RowSpan = 1, int ColumnSpan = 1);

/// <summary>Defines the size limits of a grid container.</summary>
public static class GridLimits
{
    /// <summary>The maximum number of rows.</summary>
    public const int MaxRows = 100;

    /// <summary>The maximum number of columns.</summary>
    public const int MaxColumns = 100;
}
