using System.Drawing;

namespace KinectLocation
{
    public interface IVoiBox : IVoi
    {
        float X { get; set; }

        float Y { get; set; }

        float Z { get; set; }

        SizeF Face { get; set; }

        float Depth { get; set; }
    }
}