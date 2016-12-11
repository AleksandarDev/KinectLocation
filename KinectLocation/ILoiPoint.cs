using System.Drawing;

namespace KinectLocation
{
    public interface ILoiPoint
    {
        byte Depth { get; set; }

        Point Location { get; set; }
    }
}