using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using PointF = System.Drawing.PointF;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;

        private WriteableBitmap depthMovementBitmap = null;
        private BitmapSource depthCvBitmap = null;
        private BitmapSource depthCvBitmap1 = null;
        private BitmapSource depthCvBitmap2 = null;
        private BitmapSource depthCvBitmap3 = null;
        

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }
        


        private bool CheckInBox(RectF nearBox, float nearBoxDepth, RectF farBox, float farBoxDepth, PointF location,
            float depth)
        {
            if (depth < nearBoxDepth ||
                depth > farBoxDepth)
                return false;

            var depthKey = (depth - nearBoxDepth) / (farBoxDepth - nearBoxDepth);

            var nXl = nearBox.X;
            var nXr = nearBox.X + nearBox.Width;
            var nYu = nearBox.Y;
            var nYl = nearBox.Y + nearBox.Height;

            var fXl = farBox.X;
            var fXr = farBox.X + farBox.Width;
            var fYu = farBox.Y;
            var fYl = farBox.Y + farBox.Height;

            // (f-n)*d
            var pXl = nXl + (fXl - nXl) * depthKey;
            var pXr = nXr + (fXr - nXr) * depthKey;
            var pYu = nYu + (fYu - nYu) * depthKey;
            var pYl = nYl + (fYl - nYl) * depthKey;

            // Check if in the box
            if (location.X >= pXl && location.X <= pXr &&
                location.Y >= pYu && location.Y <= pYl)
                return true;
            return false;
        }
    }
}
