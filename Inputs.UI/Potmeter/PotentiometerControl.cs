using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Inputs.UI.Potmeter;

/// <summary>Provides a dial-style numeric input controlled by pointer or arrow keys.</summary>
/// <remarks>Pointer and keyboard changes are clamped to <see cref="Minimum"/> and <see cref="Maximum"/>; values assigned directly by callers are not normalized.</remarks>
public sealed class PotentiometerControl : FrameworkElement
{
    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(PotentiometerControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(PotentiometerControl), new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender));
    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(PotentiometerControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    /// <summary>Identifies the <see cref="Step"/> dependency property.</summary>
    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(PotentiometerControl), new PropertyMetadata(1d));

    public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(nameof(BackColor), typeof(Color), typeof(PotentiometerControl), new FrameworkPropertyMetadata(Colors.Gainsboro, FrameworkPropertyMetadataOptions.AffectsRender));

    public Color BackColor
    {
        get => (Color)GetValue(BackColorProperty);
        set => SetValue(BackColorProperty, value);
    }

    // Using a DependencyProperty as the backing store for BackColorDisabled.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty BackColorDisabledProperty =
        DependencyProperty.Register(nameof(BackColorDisabled), typeof(Color), typeof(PotentiometerControl), new PropertyMetadata(Colors.LightGray));


    public Color BackColorDisabled
    {
        get { return (Color)GetValue(BackColorDisabledProperty); }
        set { SetValue(BackColorDisabledProperty, value); }
    }


    public Color BorderColor
    {
        get { return (Color)GetValue(BorderColorProperty); }
        set { SetValue(BorderColorProperty, value); }
    }

    // Using a DependencyProperty as the backing store for BorderColor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register(nameof(BorderColor), typeof(Color), typeof(PotentiometerControl), new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender));


    // Using a DependencyProperty as the backing store for ForeColor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ForeColorProperty =
        DependencyProperty.Register(nameof(ForeColor), typeof(Color), typeof(PotentiometerControl), new FrameworkPropertyMetadata(Colors.Plum, FrameworkPropertyMetadataOptions.AffectsRender));

    public Color ForeColor
    {
        get { return (Color)GetValue(ForeColorProperty); }
        set { SetValue(ForeColorProperty, value); }
    }


    /// <summary>Gets or sets the amount by which the value changes for each arrow-key press; defaults to 1.</summary>
    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }
    /// <summary>Gets or sets the lower bound of the dial value; defaults to 0.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    /// <summary>Gets or sets the upper bound of the dial value; defaults to 100.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }
    /// <summary>Gets or sets the numeric value represented by the dial pointer.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }


    /// <summary>Creates a focusable potentiometer control.</summary>
    public PotentiometerControl()
    {
        Focusable = true;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = Math.Max(0, Math.Min(ActualWidth, ActualHeight) / 2 - 4);
        dc.DrawEllipse(new SolidColorBrush(IsEnabled ? BackColor : BackColorDisabled), 
                       new Pen(new SolidColorBrush(BorderColor), 2), center, radius, radius);
        if (Maximum <= Minimum)
            return;
        var angle = (-135 + 270 * Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1)) * Math.PI / 180;
        var end = new Point(center.X + Math.Sin(angle) * radius * 0.75, center.Y - Math.Cos(angle) * radius * 0.75);
        dc.DrawLine(new Pen(new SolidColorBrush(ForeColor), 4), center, end);
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
        if (Maximum <= Minimum)
            return;
        var angle = Math.Atan2(point.X - ActualWidth / 2, ActualHeight / 2 - point.Y) * 180 / Math.PI;
        SetCurrentValue(ValueProperty, Minimum + (Math.Clamp(angle, -135, 135) + 135) / 270 * (Maximum - Minimum));
    }
    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Maximum <= Minimum)
            return;
        if (e.Key is Key.Left or Key.Down)
            SetCurrentValue(ValueProperty, Math.Clamp(Value - Step, Minimum, Maximum));
        else if (e.Key is Key.Right or Key.Up)
            SetCurrentValue(ValueProperty, Math.Clamp(Value + Step, Minimum, Maximum));
        else
            return;
        e.Handled = true;
    }
}
