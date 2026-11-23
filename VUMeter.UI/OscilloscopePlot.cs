using System.Windows;
using System.Windows.Media;

namespace VUMeter.UI;
/// <summary>
/// Renders time-domain traces from an oscilloscope view model.
/// </summary>
public sealed class OscilloscopePlot : FrameworkElement
{
    /// <summary>
    /// Identifies the model dependency property.
    /// </summary>
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(object), typeof(OscilloscopePlot), new FrameworkPropertyMetadata(null, OnModelChanged));
    /// <summary>
    /// Gets or sets the oscilloscope view model whose channel traces are rendered.
    /// </summary>
    public object? Model { get => GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    private static void OnModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var plot = (OscilloscopePlot)sender;
        if (args.OldValue is OscilloscopeViewModel oldModel)
            WeakEventManager<OscilloscopeViewModel, EventArgs>.RemoveHandler(oldModel, nameof(OscilloscopeViewModel.PlotChanged), plot.OnPlotChanged);
        if (args.NewValue is OscilloscopeViewModel newModel)
            WeakEventManager<OscilloscopeViewModel, EventArgs>.AddHandler(newModel, nameof(OscilloscopeViewModel.PlotChanged), plot.OnPlotChanged);
        plot.InvalidateVisual();
    }

    private void OnPlotChanged(object? sender, EventArgs args) => InvalidateVisual();

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(Brushes.Gainsboro, new Pen(Brushes.Gray, 1), bounds);
        if (Model is not OscilloscopeViewModel model || ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.PushClip(new RectangleGeometry(bounds));
        var grid = new Pen(Brushes.Silver, 1);
        for (var i = 1; i < 10; i++) dc.DrawLine(grid, new Point(ActualWidth * i / 10, 0), new Point(ActualWidth * i / 10, ActualHeight));
        for (var i = 1; i < 8; i++) dc.DrawLine(grid, new Point(0, ActualHeight * i / 8), new Point(ActualWidth, ActualHeight * i / 8));
        foreach (var channel in model.Channels)
        {
            if (channel.EffectiveColor is not { } color || channel.EffectiveAmplitude is not { } amplitude || channel.EffectiveOffset is not { } offset) continue;
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B)), 1.5);
            Point? previous = null;
            foreach (var sample in channel.Samples)
            {
                if (sample.Value is not { } value) { previous = null; continue; }
                var x = (sample.Time - model.DisplayStart) / model.DisplayDuration * ActualWidth;
                var y = ActualHeight * (0.5 - (value - offset) / amplitude / 8);
                if (!double.IsFinite(x) || !double.IsFinite(y)) { previous = null; continue; }
                var current = new Point(x, y);
                if (previous is { } start) dc.DrawLine(pen, start, current);
                previous = current;
            }
        }
        dc.Pop();
    }
}
