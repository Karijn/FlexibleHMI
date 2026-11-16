using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FlexibleHMI;

internal sealed class DropTargetAdorner : Adorner
{
    private static readonly Brush IndicatorBrush = new SolidColorBrush(Color.FromRgb(32, 130, 72));
    private static readonly Pen IndicatorPen = new(IndicatorBrush, 3);

    public DropTargetAdorner(UIElement adornedElement, DropPosition position)
        : base(adornedElement)
    {
        Position = position;
        IsHitTestVisible = false;
    }

    private DropPosition Position { get; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var bounds = new Rect(AdornedElement.RenderSize);
        bounds.Inflate(-2, -2);

        switch (Position)
        {
            case DropPosition.Before:
                drawingContext.DrawLine(IndicatorPen, new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top));
                break;
            case DropPosition.After:
                drawingContext.DrawLine(IndicatorPen, new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom));
                break;
            case DropPosition.Child:
                drawingContext.DrawRoundedRectangle(null, IndicatorPen, bounds, 3, 3);
                break;
        }
    }
}
