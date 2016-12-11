using System.Drawing;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public interface ILoiPoint
    {
        byte Depth { get; set; }

        Point Location { get; set; }
    }
}