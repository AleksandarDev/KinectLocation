using System.Drawing;

namespace KinectLocation
{
    public interface IContour
    {
        Rectangle BoundingBox { get; set; }
        Point[] Points { get; set; }
    }
}