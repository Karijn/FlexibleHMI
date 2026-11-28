using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace VUMeter.UI;

/// <summary>Draws a vertical segmented LED meter.</summary>
public sealed class LedBarDisplay : FrameworkElement
{
    private readonly Dictionary<(int Column, int Row), FadeState> _fadeStates = new();
    private bool _renderingSubscribed;
    private int _lastRows;
    private int _lastColumns;
    private int _lastSegmentCount;
    private int _lastColumnCount;

    /// <summary>Creates a display that animates only while loaded.</summary>
    public LedBarDisplay()
    {
        Unloaded += (_, _) => StopAnimation();
    }

    /// <summary>Identifies the number of segments.</summary>
    public static readonly DependencyProperty SegmentCountProperty = DependencyProperty.Register(
        nameof(SegmentCount), typeof(int), typeof(LedBarDisplay), new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the number of illuminated segments.</summary>
    public static readonly DependencyProperty LitSegmentsProperty = DependencyProperty.Register(
        nameof(LitSegments), typeof(int), typeof(LedBarDisplay), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the number of columns.</summary>
    public static readonly DependencyProperty ColumnCountProperty = DependencyProperty.Register(
        nameof(ColumnCount), typeof(int), typeof(LedBarDisplay), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the normalized color stops.</summary>
    public static readonly DependencyProperty ColorStopsProperty = DependencyProperty.Register(
        nameof(ColorStops), typeof(GradientStopCollection), typeof(LedBarDisplay), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies whether colors are interpolated between stops.</summary>
    public static readonly DependencyProperty UseGradientProperty = DependencyProperty.Register(
        nameof(UseGradient), typeof(bool), typeof(LedBarDisplay), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the source of individual column values.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(object), typeof(LedBarDisplay), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the source update revision.</summary>
    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(
        nameof(Revision), typeof(int), typeof(LedBarDisplay), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the size of the centered illuminated band; zero displays all rows below the level.</summary>
    public static readonly DependencyProperty VisibleRowsProperty = DependencyProperty.Register(
        nameof(VisibleRows), typeof(int), typeof(LedBarDisplay), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the fade duration in milliseconds.</summary>
    public static readonly DependencyProperty FadeMillisecondsProperty = DependencyProperty.Register(
        nameof(FadeMilliseconds), typeof(double), typeof(LedBarDisplay), new FrameworkPropertyMetadata(200d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the number of segments.</summary>
    public int SegmentCount { get => (int)GetValue(SegmentCountProperty); set => SetValue(SegmentCountProperty, value); }
    /// <summary>Gets or sets the number of illuminated segments.</summary>
    public int LitSegments { get => (int)GetValue(LitSegmentsProperty); set => SetValue(LitSegmentsProperty, value); }
    /// <summary>Gets or sets the number of identical columns.</summary>
    public int ColumnCount { get => (int)GetValue(ColumnCountProperty); set => SetValue(ColumnCountProperty, value); }
    /// <summary>Gets or sets the colors and their normalized heights.</summary>
    public GradientStopCollection? ColorStops { get => (GradientStopCollection?)GetValue(ColorStopsProperty); set => SetValue(ColorStopsProperty, value); }
    /// <summary>Gets or sets whether to interpolate between colors.</summary>
    public bool UseGradient { get => (bool)GetValue(UseGradientProperty); set => SetValue(UseGradientProperty, value); }
    /// <summary>Gets or sets the source of column values.</summary>
    public object? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    /// <summary>Gets or sets the source revision.</summary>
    public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }
    /// <summary>Gets or sets the number of centered rows, or zero for a full bar.</summary>
    public int VisibleRows { get => (int)GetValue(VisibleRowsProperty); set => SetValue(VisibleRowsProperty, value); }
    /// <summary>Gets or sets fade duration in milliseconds.</summary>
    public double FadeMilliseconds { get => (double)GetValue(FadeMillisecondsProperty); set => SetValue(FadeMillisecondsProperty, value); }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (SegmentCount < 1 || ColumnCount < 1 || ActualWidth <= 0 || ActualHeight <= 0) return;
        var rows = (int)Math.Min(SegmentCount, Math.Max(1, Math.Floor(ActualHeight / 2)));
        var columns = (int)Math.Min(ColumnCount, Math.Max(1, Math.Floor(ActualWidth / 2)));
        if (rows != _lastRows || columns != _lastColumns || SegmentCount != _lastSegmentCount || ColumnCount != _lastColumnCount)
        {
            _fadeStates.Clear();
            _lastRows = rows;
            _lastColumns = columns;
            _lastSegmentCount = SegmentCount;
            _lastColumnCount = ColumnCount;
        }
        var height = ActualHeight / rows;
        var width = ActualWidth / columns;
        var now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        var animating = false;
        for (var column = 0; column < columns; column++)
        {
            var actualColumn = (int)((long)column * ColumnCount / columns) + 1;
            var lit = Math.Clamp(Source is LedBarViewModel model ? model.GetColumnLevel(actualColumn) : LitSegments, 0, SegmentCount);
            var band = Math.Min((long)Math.Max(0, VisibleRows), SegmentCount);
            var start = band == 0 ? 1 : Math.Clamp((long)lit - (band - 1) / 2, 1, (long)SegmentCount - band + 1);
            var end = band == 0 ? lit : start + band - 1;
            for (var row = 0; row < rows; row++)
            {
                var threshold = (long)(row + 1) * SegmentCount / rows;
                var on = lit > 0 && threshold >= start && threshold <= end;
                var key = (column, row);
                if (!_fadeStates.TryGetValue(key, out var state))
                    _fadeStates[key] = state = new FadeState(on ? 1 : 0, on ? 1 : 0, now);
                var duration = double.IsFinite(FadeMilliseconds) ? Math.Max(0, FadeMilliseconds) / 1000 : 0;
                var brightness = state.ValueAt(now, duration);
                var target = on ? 1d : 0d;
                if (state.Target != target)
                    _fadeStates[key] = state = new FadeState(brightness, target, now);
                brightness = state.ValueAt(now, duration);
                if (brightness != target) animating = true;
                else if (state.Start != target) _fadeStates[key] = new FadeState(target, target, now);
                var brush = BlendBrush(ColorAt((double)threshold / SegmentCount), brightness);
                dc.DrawRoundedRectangle(brush, null, new Rect(column * width, ActualHeight - (row + 1) * height,
                    Math.Max(0, width - Math.Min(2, width / 4)), Math.Max(0, height - Math.Min(2, height / 4))), 2, 2);
            }
        }
        if (animating && IsLoaded && !_renderingSubscribed)
        {
            CompositionTarget.Rendering += OnAnimationFrame;
            _renderingSubscribed = true;
        }
        else if (!animating) StopAnimation();
    }

    private void OnAnimationFrame(object? sender, EventArgs e) => InvalidateVisual();

    private void StopAnimation()
    {
        if (!_renderingSubscribed) return;
        CompositionTarget.Rendering -= OnAnimationFrame;
        _renderingSubscribed = false;
    }

    private static Brush BlendBrush(Brush litBrush, double brightness)
    {
        if (brightness <= 0) return Brushes.DarkGreen;
        if (brightness >= 1) return litBrush;
        var color = ((SolidColorBrush)litBrush).Color;
        var dark = Colors.DarkGreen;
        byte Blend(byte a, byte b) => (byte)Math.Round(a + (b - a) * brightness);
        return new SolidColorBrush(Color.FromArgb(Blend(dark.A, color.A), Blend(dark.R, color.R), Blend(dark.G, color.G), Blend(dark.B, color.B)));
    }

    private sealed record FadeState(double Start, double Target, double StartedAt)
    {
        public double ValueAt(double now, double duration) => duration <= 0 ? Target : Start + (Target - Start) * Math.Clamp((now - StartedAt) / duration, 0, 1);
    }

    private Brush ColorAt(double height)
    {
        if (ColorStops is null || ColorStops.Count == 0) return Brushes.LimeGreen;
        var previous = ColorStops[0];
        for (var index = 1; index < ColorStops.Count; index++)
        {
            var next = ColorStops[index];
            if (height < next.Offset)
            {
                if (!UseGradient || next.Offset <= previous.Offset) return new SolidColorBrush(previous.Color);
                var amount = Math.Clamp((height - previous.Offset) / (next.Offset - previous.Offset), 0, 1);
                byte Blend(byte a, byte b) => (byte)Math.Round(a + (b - a) * amount);
                return new SolidColorBrush(Color.FromArgb(Blend(previous.Color.A, next.Color.A), Blend(previous.Color.R, next.Color.R),
                    Blend(previous.Color.G, next.Color.G), Blend(previous.Color.B, next.Color.B)));
            }
            previous = next;
        }
        return new SolidColorBrush(previous.Color);
    }
}
