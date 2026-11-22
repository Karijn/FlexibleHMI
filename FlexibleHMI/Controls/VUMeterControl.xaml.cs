//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Media;
//using System.Windows.Shapes;

//namespace FlexibleHMI.Controls;

///// <summary>
///// Interaction logic for VUMeterControl.xaml
///// </summary>
//public partial class VUMeterControl : UserControl
//{
//    // Pivot point of the needle (bottom-left area of the meter face)
//    private readonly Point _pivotPoint = new Point(250, 280);

//    // Radius of the arc from pivot to where tick marks appear
//    private readonly double _arcRadius = 200;

//    // Angle range: the needle sweeps from startAngle to endAngle
//    // Measured in degrees from vertical (negative = left, positive = right)
//    private readonly double _startAngle = -50;
//    private readonly double _endAngle = 50;

//    // VU scale values and their approximate angular positions
//    // Real VU meters have a non-linear (logarithmic) scale
//    private readonly (double vuValue, double angleFraction)[] _scaleMarks = new[]
//    {
//            (-20.0, 0.00),
//            (-10.0, 0.20),
//            (-7.0,  0.30),
//            (-5.0,  0.40),
//            (-3.0,  0.52),
//            (0.0,   0.71),
//            (1.0,   0.80),
//            (2.0,   0.88),
//            (3.0,   1.00),
//        };

//    public VUMeterControl()
//    {
//        InitializeComponent();
//        Loaded += (s, e) => DrawScale();
//    }

//    private void DrawScale()
//    {
//        double totalAngleSpan = _endAngle - _startAngle;

//        foreach (var (vuValue, fraction) in _scaleMarks)
//        {
//            double angle = _startAngle + fraction * totalAngleSpan;
//            double angleRad = angle * Math.PI / 180.0;

//            // Calculate tick endpoints
//            double innerRadius = _arcRadius - 15;
//            double outerRadius = _arcRadius;

//            // Major ticks are longer
//            bool isMajor = (vuValue == -20 || vuValue == -10 || vuValue == -7
//                || vuValue == -5 || vuValue == -3 || vuValue == 0
//                || vuValue == 3);
//            if (isMajor)
//            {
//                innerRadius = _arcRadius - 25;
//            }

//            Point innerPt = GetPointOnArc(_pivotPoint, innerRadius, angleRad);
//            Point outerPt = GetPointOnArc(_pivotPoint, outerRadius, angleRad);

//            var tick = new Line
//            {
//                X1 = innerPt.X,
//                Y1 = innerPt.Y,
//                X2 = outerPt.X,
//                Y2 = outerPt.Y,
//                Stroke = vuValue >= 0 ? Brushes.Red : Brushes.Black,
//                StrokeThickness = isMajor ? 2 : 1
//            };
//            MeterCanvas.Children.Add(tick);

//            // Add label
//            if (isMajor)
//            {
//                Point labelpt = GetPointOnArc(_pivotPoint, _arcRadius + 20, angleRad);
//                string label;

//                // Remove the minus sign from negative values on the scale;
//                // the original meter just shows "20", "10", etc. with a
//                // small "-" prefix at -20
//                if (vuValue < 0)
//                {
//                    label = Math.Abs(vuValue).ToString();
//                }
//                else if (vuValue == 0)
//                {
//                    label = "0";
//                }
//                else
//                {
//                    label = "+" + vuValue;
//                }

//                var textBlock = new TextBlock
//                {
//                    Text = label,
//                    FontSize = 14,
//                    FontWeight = FontWeights.Bold,
//                    Foreground = vuValue >= 0 ? Brushes.Red : Brushes.Black,
//                };

//                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
//                double textWidth = textBlock.DesiredSize.Width;
//                double textHeight = textBlock.DesiredSize.Height;

//                Canvas.SetLeft(textBlock, labelpt.X - textWidth / 2);
//                Canvas.SetTop(textBlock, labelpt.Y - textHeight / 2);
//                MeterCanvas.Children.Add(textBlock);
//            }
//        }
//    }

//    private Point GetPointOnArc(Point center, double radius, double angleRadians)
//    {
//        // Angle measured from vertical (12 o'clock), clockwise positive
//        return new Point(
//            center.X + radius * Math.Sin(angleRadians),
//            center.Y - radius * Math.Cos(angleRadians)
//        );
//    }

//    // Use for smoother needle movement ic case the binding to NeedleAngle is not smooth enough
//    //private void AnimateNeedle(double targetAngle)
//    //{
//    //    var animation = new DoubleAnimation
//    //    {
//    //        To = targetAngle,
//    //        Duration = TimeSpan.FromMilliseconds(50),
//    //        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
//    //    };

//    //    NeedleRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
//    //}
//}
