using System.Drawing;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public interface IContour
    {
        Rectangle BoundingBox { get; set; }
        Point[] Points { get; set; }
    }
}