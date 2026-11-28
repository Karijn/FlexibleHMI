using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VUMeter.UI;
/// <summary>
/// Provides the WPF view used to display a VU meter.
/// </summary>
public partial class VUMeterView
{
    private static readonly Point Pivot = new(250, 280);
    private const double SweepAngle = 50;
    private const double ScaleRadius = 195;

    private void VUMeterView_Loaded(object sender, RoutedEventArgs e)
    {
        DrawScale();
    }

    private void DrawScale()
    {
        MeterCanvas.Children.Clear();
        DrawArc();

        for (var angle = -SweepAngle; angle <= SweepAngle; angle += 5)
        {
            var major = angle % 25 == 0;
            var redZone = angle >= 30;
            var innerRadius = ScaleRadius - (major ? 24 : 12);
            var start = GetPoint(angle, innerRadius);
            var end = GetPoint(angle, ScaleRadius);
            MeterCanvas.Children.Add(new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = redZone ? Brushes.Firebrick : Brushes.Black,
                StrokeThickness = major ? 2 : 1
            });

            if (major)
            {
                var label = new TextBlock
                {
                    Text = $"{angle:+#;-#;0}°",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = redZone ? Brushes.Firebrick : Brushes.Black
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var point = GetPoint(angle, ScaleRadius - 43);
                Canvas.SetLeft(label, point.X - label.DesiredSize.Width / 2);
                Canvas.SetTop(label, point.Y - label.DesiredSize.Height / 2);
                MeterCanvas.Children.Add(label);
            }
        }

        MeterCanvas.Children.Add(new TextBlock
        {
            Text = "VU",
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Times New Roman")
        });
        Canvas.SetLeft(MeterCanvas.Children[^1], 230);
        Canvas.SetTop(MeterCanvas.Children[^1], 185);

        MeterCanvas.Children.Add(new TextBlock
        {
            Text = "NEEDLE ANGLE",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold
        });
        Canvas.SetLeft(MeterCanvas.Children[^1], 211);
        Canvas.SetTop(MeterCanvas.Children[^1], 218);

        MeterCanvas.Children.Add(Needles);
        MeterCanvas.Children.Add(PivotCover);
    }

    private void DrawArc()
    {
        var figure = new PathFigure
        {
            StartPoint = GetPoint(-SweepAngle, ScaleRadius),
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment(
            GetPoint(SweepAngle, ScaleRadius),
            new Size(ScaleRadius, ScaleRadius),
            0,
            false,
            SweepDirection.Clockwise,
            true));
        var geometry = new PathGeometry([figure]);
        MeterCanvas.Children.Add(new Path
        {
            Data = geometry,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        });
    }

    private static Point GetPoint(double angle, double radius)
    {
        var radians = angle * Math.PI / 180;
        return new Point(
            Pivot.X + radius * Math.Sin(radians),
            Pivot.Y - radius * Math.Cos(radians));
    }
}
