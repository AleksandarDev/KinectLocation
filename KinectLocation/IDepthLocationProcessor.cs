using System;
using System.ComponentModel;
using System.Windows.Media;

namespace KinectLocation
{
    public interface IDepthLocationProcessor : INotifyPropertyChanged, IDisposable
    {
        bool IsVisualizationContoursEnabled { get; set; }

        bool IsVisualizationDownsampledEnabled { get; set; }

        bool IsVisualizationLoiPointsEnabled { get; set; }

        ImageSource VisualizationContours { get; set; }

        ImageSource VisualizationDownsampled { get; set; }

        ImageSource VisualizationLoiPoints { get; set; }

        int FrameWidth { get; set; }

        int FrameHeight { get; set; }

        byte[] CurrentDepthFrame { get; }
    }
}