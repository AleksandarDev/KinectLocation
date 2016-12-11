using System;
using System.ComponentModel;
using System.Windows.Media;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public interface IDepthLocationProcessor : INotifyPropertyChanged, IDisposable
    {
        bool IsVisualizationContoursEnabled { get; set; }

        bool IsVisualizationDownsampledEnabled { get; set; }

        bool IsVisualizationLoiPointsEnabled { get; set; }

        ImageSource VisualizationContours { get; set; }

        ImageSource VisualizationDownsampled { get; set; }

        ImageSource VisualizationLoiPoints { get; set; }
    }
}