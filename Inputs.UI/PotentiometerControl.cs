using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Inputs.UI;

public sealed class PotentiometerControl : FrameworkElement
{
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(PotentiometerControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(PotentiometerControl), new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(PotentiometerControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(PotentiometerControl), new PropertyMetadata(1d));
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public PotentiometerControl() { Focusable = true; }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = Math.Max(0, Math.Min(ActualWidth, ActualHeight) / 2 - 4);
        dc.DrawEllipse(IsEnabled ? Brushes.Gainsboro : Brushes.LightGray, new Pen(Brushes.Gray, 2), center, radius, radius);
        if (Maximum <= Minimum) return;
        var angle = (-135 + 270 * Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1)) * Math.PI / 180;
        var end = new Point(center.X + Math.Sin(angle) * radius * 0.75, center.Y - Math.Cos(angle) * radius * 0.75);
        dc.DrawLine(new Pen(Brushes.SteelBlue, 4), center, end);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e); Focus(); CaptureMouse(); Update(e.GetPosition(this)); e.Handled = true; }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Update(e.GetPosition(this)); }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { base.OnMouseLeftButtonUp(e); if (IsMouseCaptured) { Update(e.GetPosition(this)); ReleaseMouseCapture(); e.Handled = true; } }
    private void Update(Point point)
    {
        if (Maximum <= Minimum) return;
        var angle = Math.Atan2(point.X - ActualWidth / 2, ActualHeight / 2 - point.Y) * 180 / Math.PI;
        SetCurrentValue(ValueProperty, Minimum + (Math.Clamp(angle, -135, 135) + 135) / 270 * (Maximum - Minimum));
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Maximum <= Minimum) return;
        if (e.Key is Key.Left or Key.Down) SetCurrentValue(ValueProperty, Math.Clamp(Value - Step, Minimum, Maximum));
        else if (e.Key is Key.Right or Key.Up) SetCurrentValue(ValueProperty, Math.Clamp(Value + Step, Minimum, Maximum));
        else return;
        e.Handled = true;
    }
}
