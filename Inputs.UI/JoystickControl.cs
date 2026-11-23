using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Inputs.UI;

public sealed class JoystickControl : FrameworkElement
{
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(-1d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty XValueProperty = DependencyProperty.Register(nameof(XValue), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty YValueProperty = DependencyProperty.Register(nameof(YValue), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty XStepProperty = DependencyProperty.Register(nameof(XStep), typeof(double), typeof(JoystickControl), new PropertyMetadata(0.01d));
    public static readonly DependencyProperty YStepProperty = DependencyProperty.Register(nameof(YStep), typeof(double), typeof(JoystickControl), new PropertyMetadata(0.01d));
    public double XStep { get => (double)GetValue(XStepProperty); set => SetValue(XStepProperty, value); }
    public double YStep { get => (double)GetValue(YStepProperty); set => SetValue(YStepProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double XValue { get => (double)GetValue(XValueProperty); set => SetValue(XValueProperty, value); }
    public double YValue { get => (double)GetValue(YValueProperty); set => SetValue(YValueProperty, value); }
    public JoystickControl() { Focusable = true; }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var rect = new Rect(1, 1, Math.Max(0, ActualWidth - 2), Math.Max(0, ActualHeight - 2));
        dc.DrawRectangle(IsEnabled ? Brushes.Gainsboro : Brushes.LightGray, new Pen(Brushes.Gray, 1), rect);
        dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(ActualWidth / 2, 0), new Point(ActualWidth / 2, ActualHeight));
        dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(0, ActualHeight / 2), new Point(ActualWidth, ActualHeight / 2));
        if (Maximum <= Minimum) return;
        var x = 8 + Math.Clamp((XValue - Minimum) / (Maximum - Minimum), 0, 1) * Math.Max(0, ActualWidth - 16);
        var y = 8 + (1 - Math.Clamp((YValue - Minimum) / (Maximum - Minimum), 0, 1)) * Math.Max(0, ActualHeight - 16);
        dc.DrawEllipse(Brushes.SteelBlue, new Pen(Brushes.Black, 1), new Point(x, y), 7, 7);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e); Focus(); CaptureMouse(); Update(e.GetPosition(this)); e.Handled = true; }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Update(e.GetPosition(this)); }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { base.OnMouseLeftButtonUp(e); if (IsMouseCaptured) { Update(e.GetPosition(this)); ReleaseMouseCapture(); e.Handled = true; } }
    private void Update(Point point)
    {
        if (Maximum <= Minimum || ActualWidth <= 16 || ActualHeight <= 16) return;
        var x = Math.Clamp((point.X - 8) / (ActualWidth - 16), 0, 1);
        var y = 1 - Math.Clamp((point.Y - 8) / (ActualHeight - 16), 0, 1);
        SetCurrentValue(XValueProperty, Minimum + x * (Maximum - Minimum));
        SetCurrentValue(YValueProperty, Minimum + y * (Maximum - Minimum));
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Maximum <= Minimum) return;
        switch (e.Key)
        {
            case Key.Left: SetCurrentValue(XValueProperty, Math.Clamp(XValue - XStep, Minimum, Maximum)); break;
            case Key.Right: SetCurrentValue(XValueProperty, Math.Clamp(XValue + XStep, Minimum, Maximum)); break;
            case Key.Down: SetCurrentValue(YValueProperty, Math.Clamp(YValue - YStep, Minimum, Maximum)); break;
            case Key.Up: SetCurrentValue(YValueProperty, Math.Clamp(YValue + YStep, Minimum, Maximum)); break;
            default: return;
        }
        e.Handled = true;
    }
}
