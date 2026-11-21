using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FlexibleHMI.ViewModels;

//public partial class VUMeterViewModel : ObservableObject
//{
//    Current VU level(range: -20 to +3)

//    public Double CurrentVuLevel
//    {
//        get => field;
//        set
//        {
//            SetProperty(ref field, value);
//        }
//    }

//    Needle angle in degrees(mapped from VU level)
//     The arc spans roughly from - 45deg(left, -20 VU) to +45deg(right, +3 VU)
//    private double _needleAngle = -45.0;

//    public Double NeedleAngle
//    {
//        get => _needleAngle;
//        set
//        {
//            SetProperty(ref _needleAngle, value);
//        }
//    }

//    Peak LED indicator
//   [ObservableProperty]
//    private bool _isPeakActive = false;

//    // Map VU level to needle angle
//    public void UpdateLevel(double vuLevel)
//    {
//        CurrentVuLevel = vuLevel;

//        // Map -20..+3 VU to -45..+45 degrees
//        // Linear interpolation: angle = ((vuLevel - (-20)) / (3 - (-20))) * 90 - 45
//        NeedleAngle = ((vuLevel + 20.0) / 23.0) * 90.0 - 45.0;

//        // Activate peak LED if level exceeds 0 VU
//        IsPeakActive = vuLevel >= 0.0;
//    }

//    private double _vuLevel;

//    public double VuLevel
//    {
//        get => CurrentVuLevel;
//        set
//        {
//            CurrentVuLevel = value;

//            Map - 20..+3 VU to -45..+45 degrees
//            Linear interpolation: angle = ((vuLevel - (-20)) / (3 - (-20))) * 90 - 45
//            NeedleAngle = ((_vuLevel + 20.0) / 23.0) * 90.0 - 45.0;

//            Activate peak LED if level exceeds 0 VU
//           IsPeakActive = _vuLevel >= 0.0;
//        }
//    }
//}
