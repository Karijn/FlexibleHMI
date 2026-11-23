using System.Windows;
using System.Windows.Media;

namespace VUMeter.UI;
/// <summary>
/// Renders configured points and trails from an XY control view model.
/// </summary>
public sealed class XYPlot : FrameworkElement
{
    /// <summary>
    /// Identifies the model dependency property.
    /// </summary>
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(object), typeof(XYPlot), new FrameworkPropertyMetadata(null, OnModelChanged));
    /// <summary>
    /// Gets or sets the XY control view model whose points and trails are rendered.
    /// </summary>
    public object? Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var plot = (XYPlot)sender;
        if (args.OldValue is XYControlViewModel oldModel)
            WeakEventManager<XYControlViewModel, EventArgs>.RemoveHandler(oldModel, nameof(XYControlViewModel.PlotChanged), plot.OnPlotChanged);
        if (args.NewValue is XYControlViewModel newModel)
            WeakEventManager<XYControlViewModel, EventArgs>.AddHandler(newModel, nameof(XYControlViewModel.PlotChanged), plot.OnPlotChanged);
        plot.InvalidateVisual();
    }

    private void OnPlotChanged(object? sender, EventArgs args) => InvalidateVisual();

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        drawingContext.DrawRectangle(Brushes.Gainsboro, new Pen(Brushes.Gray, 1), bounds);
        if (Model is not XYControlViewModel model || !model.HasValidBounds || ActualWidth <= 0 || ActualHeight <= 0) return;

        drawingContext.PushClip(new RectangleGeometry(bounds));
        var gridPen = new Pen(Brushes.Silver, 1);
        for (var index = 1; index < 10; index++)
        {
            var x = ActualWidth * index / 10;
            var y = ActualHeight * index / 10;
            drawingContext.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
            drawingContext.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
        }

        foreach (var point in model.Points)
        {
            if (point.EffectiveColor is not { } color || point.EffectiveSize is not { } size) continue;
            var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
            var pen = new Pen(brush, 1.5);
            if (point.EffectiveShowTrail)
            {
                for (var index = 1; index < point.Trail.Count; index++)
                {
                    var first = Transform(point.Trail[index - 1], model);
                    var second = Transform(point.Trail[index], model);
                    if (first is not null && second is not null) drawingContext.DrawLine(pen, first.Value, second.Value);
                }
            }

            if (point.Position is { } position && Transform(position, model) is { } screen)
                drawingContext.DrawEllipse(brush, null, screen, size / 2, size / 2);
        }
        drawingContext.Pop();
    }

    private Point? Transform(Point point, XYControlViewModel model)
    {
        var x = (point.X - model.MinimumX) / (model.MaximumX - model.MinimumX) * ActualWidth;
        var y = (1 - (point.Y - model.MinimumY) / (model.MaximumY - model.MinimumY)) * ActualHeight;
        return double.IsFinite(x) && double.IsFinite(y) ? new Point(x, y) : null;
    }
}
