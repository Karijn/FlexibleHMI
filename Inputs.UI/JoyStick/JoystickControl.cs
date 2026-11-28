using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Inputs.UI.JoyStick;

/// <summary>Provides a two-axis joystick surface with pointer and keyboard input.</summary>
/// <remarks>Pointer and keyboard changes are clamped to <see cref="Minimum"/> and <see cref="Maximum"/>; values assigned directly by callers are not normalized. Arrow keys move each axis by its configured step.</remarks>
public sealed class JoystickControl : FrameworkElement
{
    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(-1d, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the <see cref="XValue"/> dependency property.</summary>
    public static readonly DependencyProperty XValueProperty = DependencyProperty.Register(nameof(XValue), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    /// <summary>Identifies the <see cref="YValue"/> dependency property.</summary>
    public static readonly DependencyProperty YValueProperty = DependencyProperty.Register(nameof(YValue), typeof(double), typeof(JoystickControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    /// <summary>Identifies the <see cref="XStep"/> dependency property.</summary>
    public static readonly DependencyProperty XStepProperty = DependencyProperty.Register(nameof(XStep), typeof(double), typeof(JoystickControl), new PropertyMetadata(0.01d));
    /// <summary>Identifies the <see cref="YStep"/> dependency property.</summary>
    public static readonly DependencyProperty YStepProperty = DependencyProperty.Register(nameof(YStep), typeof(double), typeof(JoystickControl), new PropertyMetadata(0.01d));
    /// <summary>Gets or sets the amount by which the X value changes for each horizontal arrow-key press; defaults to 0.01.</summary>
    public double XStep
    {
        get => (double)GetValue(XStepProperty);
        set => SetValue(XStepProperty, value);
    }
    /// <summary>Gets or sets the amount by which the Y value changes for each vertical arrow-key press; defaults to 0.01.</summary>
    public double YStep
    {
        get => (double)GetValue(YStepProperty);
        set => SetValue(YStepProperty, value);
    }
    /// <summary>Gets or sets the lower bound for both axis values; defaults to -1.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    /// <summary>Gets or sets the upper bound for both axis values; defaults to 1.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }
    /// <summary>Gets or sets the horizontal value represented by the joystick handle.</summary>
    public double XValue
    {
        get => (double)GetValue(XValueProperty);
        set => SetValue(XValueProperty, value);
    }
    /// <summary>Gets or sets the vertical value represented by the joystick handle.</summary>
    public double YValue
    {
        get => (double)GetValue(YValueProperty);
        set => SetValue(YValueProperty, value);
    }
    /// <summary>Creates a focusable joystick control.</summary>
    public JoystickControl()
    {
        Focusable = true;
    }
    /// <inheritdoc />
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var rect = new Rect(1, 1, Math.Max(0, ActualWidth - 2), Math.Max(0, ActualHeight - 2));
        dc.DrawRectangle(IsEnabled ? Brushes.Gainsboro : Brushes.LightGray, new Pen(Brushes.Gray, 1), rect);
        dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(ActualWidth / 2, 0), new Point(ActualWidth / 2, ActualHeight));
        dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(0, ActualHeight / 2), new Point(ActualWidth, ActualHeight / 2));
        if (Maximum <= Minimum)
            return;
        var x = 8 + Math.Clamp((XValue - Minimum) / (Maximum - Minimum), 0, 1) * Math.Max(0, ActualWidth - 16);
        var y = 8 + (1 - Math.Clamp((YValue - Minimum) / (Maximum - Minimum), 0, 1)) * Math.Max(0, ActualHeight - 16);
        dc.DrawEllipse(Brushes.SteelBlue, new Pen(Brushes.Black, 1), new Point(x, y), 7, 7);
    }
    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        CaptureMouse();
        Update(e.GetPosition(this));
        e.Handled = true;
    }
    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            Update(e.GetPosition(this));
    }
    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (IsMouseCaptured)
        {
            Update(e.GetPosition(this));
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }
    private void Update(Point point)
    {
        if (Maximum <= Minimum || ActualWidth <= 16 || ActualHeight <= 16)
            return;
        var x = Math.Clamp((point.X - 8) / (ActualWidth - 16), 0, 1);
        var y = 1 - Math.Clamp((point.Y - 8) / (ActualHeight - 16), 0, 1);
        SetCurrentValue(XValueProperty, Minimum + x * (Maximum - Minimum));
        SetCurrentValue(YValueProperty, Minimum + y * (Maximum - Minimum));
    }
    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Maximum <= Minimum)
            return;
        switch (e.Key)
        {
            case Key.Left:
                SetCurrentValue(XValueProperty, Math.Clamp(XValue - XStep, Minimum, Maximum));
                break;
            case Key.Right:
                SetCurrentValue(XValueProperty, Math.Clamp(XValue + XStep, Minimum, Maximum));
                break;
            case Key.Down:
                SetCurrentValue(YValueProperty, Math.Clamp(YValue - YStep, Minimum, Maximum));
                break;
            case Key.Up:
                SetCurrentValue(YValueProperty, Math.Clamp(YValue + YStep, Minimum, Maximum));
                break;
            default:
                return;
        }
        e.Handled = true;
    }
}
