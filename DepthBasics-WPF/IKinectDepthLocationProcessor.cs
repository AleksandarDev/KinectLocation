using System;
using System.Windows.Media;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public interface IKinectDepthLocationProcessor : IDepthLocationProcessor
    {
        bool IsVisualizationDepthImageEnabled { get; set; }

        ImageSource VisualizationDepthImage { get; set; }
    }
}