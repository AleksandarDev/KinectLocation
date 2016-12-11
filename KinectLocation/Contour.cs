using System.Drawing;

namespace KinectLocation
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
