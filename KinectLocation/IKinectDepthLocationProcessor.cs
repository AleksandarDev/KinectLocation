using System.Windows.Media;

namespace KinectLocation
{
    public interface IKinectDepthLocationProcessor : IDepthLocationProcessor
    {
        bool IsVisualizationDepthImageEnabled { get; set; }

        ImageSource VisualizationDepthImage { get; set; }

        byte[] DepthData { get; }

        void Start();
    }
}