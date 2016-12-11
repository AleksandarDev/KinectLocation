using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using KinectLocation.Annotations;

namespace KinectLocation
{
    public abstract class DepthLocationProcessor : IDepthLocationProcessor
    {
        private readonly ILocationHandler locationHandler;

        private byte[] currentFrame;
        private byte[] previousFrame;
        
        private ImageSource visualizationDownsampled;
        private ImageSource visualizationLoiPoints;
        private ImageSource visualizationContours;


        protected DepthLocationProcessor([NotNull] ILocationHandler locationHandler)
        {
            if (locationHandler == null) throw new ArgumentNullException(nameof(locationHandler));
            this.locationHandler = locationHandler;
        }


        protected virtual void Initialize(int frameWidth, int frameHeight)
        {
            this.FrameWidth = frameWidth;
            this.FrameHeight = frameHeight;

            // Initialize data collections
            this.currentFrame = new byte[this.FrameWidth * this.FrameHeight];
            this.previousFrame = new byte[this.FrameWidth * this.FrameHeight];
        }

        protected virtual void ProcessDepthFrame(byte[] newFrame)
        {
            // Copt current frame to previous
            Buffer.BlockCopy(this.currentFrame, 0, this.previousFrame, 0, this.currentFrame.Length);

            // Do noise correction on new frame and save it
            Parallel.For(0, this.currentFrame.Length, i =>
            {
                var newValue = newFrame[i];

                // Clear noise (smooth image)
                const int noiseRemovalThr = 5;
                if (newValue < noiseRemovalThr &&
                    this.currentFrame[i] > noiseRemovalThr)
                    newValue = this.currentFrame[i];

                // Assign previous and current pixel
                this.currentFrame[i] = newValue;
            });

            // Process the locations from the frame
            this.ProcessFramesForLocation();
        }

        private void ProcessFramesForLocation()
        {
            // Process movement to extract LoI points
            using (var currentImage = this.GetFilledFullSize(this.currentFrame))
            using (var previousImage = this.GetFilledFullSize(this.previousFrame))
            using (var changesImage = this.GetEmptyFullSize())
            {
                // Do the movement detection
                CvInvoke.AbsDiff(currentImage, previousImage, changesImage);

                // Extract LoI points from new frame
                using (var hierachy = new Mat())
                using (var contours = new VectorOfVectorOfPoint())
                using (var downsampled = changesImage
                    .ThresholdBinary(new Gray(5), new Gray(255))
                    .PyrDown()
                    .SmoothBlur(15, 15)
                    .PyrUp()
                    .ThresholdBinary(new Gray(5), new Gray(255))
                    .Erode(2))
                {
                    // Visualize downsampled image
                    if (this.IsVisualizationDownsampledEnabled)
                        this.VisualizeDownsampled(downsampled);

                    // Extract countours
                    CvInvoke.FindContours(
                        downsampled,
                        contours,
                        hierachy,
                        RetrType.External,
                        ChainApproxMethod.ChainApproxSimple);

                    // Cast to CvContours
                    var rects = ToCvContour(contours).ToList();

                    // Visualize contours
                    if (this.IsVisualizationContoursEnabled)
                        this.VisualizeContours(rects);

                    // Retrieve locations of interest points
                    var loiPoints = new List<LoiPoint>();
                    foreach (var rect in rects)
                    {
                        // Ignore small rectangles
                        if (rect.BoundingBox.Width < 20 &&
                            rect.BoundingBox.Height < 20)
                            continue;

                        // Ignore edge rectangles
                        if (rect.BoundingBox.Y < 10 ||
                            rect.BoundingBox.X < 10)
                            continue;

                        // Calculate center of contour
                        var moments = CvInvoke.Moments(new VectorOfPoint(rect.Points));
                        var x = moments.M10 / moments.M00;
                        var y = moments.M01 / moments.M00;
                        var contourCenter = new Point((int)x, (int)y);

                        // Ignore contours out of rect
                        if (contourCenter.X <= rect.BoundingBox.Left ||
                            contourCenter.Y <= rect.BoundingBox.Top ||
                            contourCenter.X >= rect.BoundingBox.Right ||
                            contourCenter.Y >= rect.BoundingBox.Bottom)
                            continue;

                        // Fill the contour and apply the contour mask to the depth image
                        using (var contourCanny = currentImage.Copy(rect.BoundingBox))
                        using (var contourFlood = this.GetEmptyFullSize())
                        using (var contourFloodMask = this.GetEmptyFullSize(2, 2))
                        using (var contourFloodPoints = new VectorOfVectorOfPoint(new VectorOfPoint(rect.Points)))
                        {
                            Rectangle contourFloodRect;
                            CvInvoke.DrawContours(
                                contourFlood,
                                contourFloodPoints,
                                -1,
                                new MCvScalar(255));
                            CvInvoke.FloodFill(
                                contourFlood,
                                contourFloodMask,
                                contourCenter,
                                new MCvScalar(255),
                                out contourFloodRect,
                                new MCvScalar(0),
                                new MCvScalar(0));

                            // Apply contour and movement mask
                            using (var contourFloodBound = contourFlood.Copy(rect.BoundingBox))
                            using (var contourCannyBound = contourCanny.Copy(contourFloodBound))
                            using (var maskMask = changesImage.Copy(rect.BoundingBox).ThresholdBinary(new Gray(20), new Gray(255)))
                            using (var contourCannyMaskedBound = contourCannyBound.Copy(maskMask))
                            {
                                // Find min depth point of contour canny
                                var contourMinDepth =
                                    contourCannyMaskedBound.Bytes.Min(b => b > 20 ? b : (byte)255);

                                // Save the location with depth data
                                var loiPoint = new LoiPoint(contourCenter, contourMinDepth);
                                loiPoints.Add(loiPoint);
                                
                                // Visualize LoI point
                                if (this.IsVisualizationLoiPointsEnabled)
                                    this.VisualizeLoiPoint(contourCannyMaskedBound, rect, loiPoint);
                            }

                        }

                        // Hand the LoI points to locations handler
                        this.locationHandler.ProcessRawLoiPoints(loiPoints);
                    }
                }
            }
        }

        private void VisualizeDownsampled(IImage downsampled)
        {
            this.VisualizationDownsampled = downsampled.ToBitmapSource();
        }

        private void VisualizeLoiPoint(Image<Gray, byte> contourCanny, IContour rect, LoiPoint loiPoint)
        {
            // Find first pixel in contour that matches LoI point depth
            Point? contourMinDepthPixelPoint = null;
            for (var i = 1; i < contourCanny.Height; i++)
            {
                for (var j = 1; j < contourCanny.Width; j++)
                {
                    // Ignore if point mot of required depth
                    if (contourCanny.Data[i, j, 0] != loiPoint.Depth)
                        continue;

                    contourMinDepthPixelPoint = new Point(j, i);
                    break;
                }

                // Break if found the point
                if (contourMinDepthPixelPoint != null)
                    break;
            }

            // Ignore if point not founc
            if (contourMinDepthPixelPoint == null)
                return;

            // Transform min depth point of contour to full image point
            var contourDepthPoint = new Point(
                contourMinDepthPixelPoint.Value.X + rect.BoundingBox.X,
                contourMinDepthPixelPoint.Value.Y + rect.BoundingBox.Y);

            // Flood the rect near min depth point inside contour
            using (var depthMovementImage = this.GetFilledFullSize(this.currentFrame))
            using (var floodRectMask = new Image<Gray, byte>(
                this.FrameWidth + 2,
                this.FrameHeight + 2))
            {
                Rectangle floodRect;
                CvInvoke.Rectangle(depthMovementImage, rect.BoundingBox, new MCvScalar(0));
                CvInvoke.FloodFill(
                    depthMovementImage,
                    floodRectMask,
                    contourDepthPoint,
                    new MCvScalar(255),
                    out floodRect,
                    new MCvScalar(1),
                    new MCvScalar(1));
                CvInvoke.Rectangle(depthMovementImage, rect.BoundingBox, new MCvScalar(0));

                this.VisualizationLoiPoints = depthMovementImage.ToBitmapSource();
            }
        }

        private void VisualizeContours(IEnumerable<IContour> contours)
        {
            if (contours == null) return;
            var contoursList = contours as IList<IContour> ?? contours.ToList();

            using (var mcvContours = new VectorOfVectorOfPoint(contoursList.Select(c => c.Points).ToArray()))
            using (var cvImage = this.GetEmptyFullSize())
            {
                // Visualize contours and draw bounding rectangles
                CvInvoke.DrawContours(cvImage, mcvContours, -1, new MCvScalar(255));
                foreach (var contour in contoursList.Where(c => c.BoundingBox.Width > 20 && c.BoundingBox.Height > 20))
                    CvInvoke.Rectangle(cvImage, contour.BoundingBox, new MCvScalar(255));

                // Show image
                this.VisualizationContours = cvImage.ToBitmapSource();
            }
        }

        protected Image<Gray, byte> GetFilledFullSize(byte[] data)
        {
            var image = this.GetEmptyFullSize();
            image.Bytes = data;
            return image;
        }

        protected Image<Gray, byte> GetEmptyFullSize(int sizeWidthOffset = 0, int sizeHeightOffset = 0)
        {
            return new Image<Gray, byte>(
                this.FrameWidth + sizeWidthOffset,
                this.FrameHeight + sizeHeightOffset);
        }

        private static IEnumerable<CvContour> ToCvContour(VectorOfVectorOfPoint contours)
        {
            // Determine contours bounding rectangles
            return contours
                .ToArrayOfArray()
                .Select(contour => new CvContour(contour));
        }

        public int FrameWidth { get; set; }
        
        public int FrameHeight { get; set; }

        public bool IsVisualizationContoursEnabled { get; set; }

        public bool IsVisualizationDownsampledEnabled { get; set; }

        public bool IsVisualizationLoiPointsEnabled { get; set; }

        public ImageSource VisualizationDownsampled
        {
            get { return this.visualizationDownsampled; }
            set
            {
                this.visualizationDownsampled = value;
                this.OnPropertyChanged();
            }
        }

        public ImageSource VisualizationLoiPoints
        {
            get { return this.visualizationLoiPoints; }
            set
            {
                this.visualizationLoiPoints = value;
                this.OnPropertyChanged();
            }
        }

        public ImageSource VisualizationContours
        {
            get { return this.visualizationContours; }
            set
            {
                this.visualizationContours = value;
                this.OnPropertyChanged();
            }
        }

        public byte[] CurrentDepthFrame => this.currentFrame;

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion /INotifyPropertyChanged implementation

        #region IDisposable implementation
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.currentFrame = null;
                this.previousFrame = null;

                this.visualizationDownsampled = null;
                this.visualizationContours = null;
                this.visualizationLoiPoints = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DepthLocationProcessor()
        {
            Dispose(false);
        }

        #endregion /IDisposable implementation
    }
}