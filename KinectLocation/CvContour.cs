using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;

namespace KinectLocation
{
    public class CvContour : Contour
    {
        public CvContour(Point[] points)
            : this(points, CvInvoke.BoundingRectangle(new VectorOfPoint(points)))
        {
        }

        public CvContour(Point[] points, Rectangle boundingBox) : base(points, boundingBox)
        {
        }
    }
}
