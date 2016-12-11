using System.Drawing;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public class Contour : IContour
    {
        public Contour(Point[] points, Rectangle boundingBox)
        {
            this.Points = points;
            this.BoundingBox = boundingBox;
        }

        public Point[] Points { get; set; }

        public Rectangle BoundingBox { get; set; }
    }
}
