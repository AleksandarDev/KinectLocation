//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;
        
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for depth frames
        /// </summary>
        private DepthFrameReader depthFrameReader = null;

        /// <summary>
        /// Description of the data contained in the depth frame
        /// </summary>
        private FrameDescription depthFrameDescription = null;
            
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
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private int depthFilled;
        private byte[] depthPixels = null;
        private byte[] depthMovementPixels = null;
        private byte[] maskBytes = null;

        private const int depthHistorySize = 3;
        private List<byte[]> depthHistory = new List<byte[]>();

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            // wire handler for frame arrival
            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            // get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];
            this.depthMovementPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];
            for (int index = 0; index < depthHistorySize; index++)
                this.depthHistory.Add(new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height]);
            this.maskBytes = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            this.depthMovementBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Rgb24, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.depthMovementBitmap;
            }
        }

        public ImageSource DepthImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        public ImageSource ImageCvSource
        {
            get
            {
                return this.depthCvBitmap3;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the depth frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance
                            
                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth,
            ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*) depthFrameData;

            // convert depth to a visual representation
            Parallel.For(0, (int) (depthFrameDataSize / this.depthFrameDescription.BytesPerPixel), i =>
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                var newValue = (byte) (depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);

                this.depthPixels[i] = newValue;

                // Clear noise (smooth image)
                const int noiseRemovalThr = 5;
                if (depthFilled > 2 &&
                    newValue < noiseRemovalThr && this.depthMovementPixels[i] > noiseRemovalThr)
                    newValue = (byte) this.depthMovementPixels[i];

                // Filter out static objects
                Func<byte, byte, bool> compareFrameDepth =
                    (d1, d2) =>
                    {
                        return Math.Abs(d1 - d2) <= 3;
                    };
                if (this.depthFilled > depthHistorySize * 10)
                {
                    var argument = true;
                    argument &= compareFrameDepth(this.depthMovementPixels[i], newValue);
                    argument &= compareFrameDepth(this.depthMovementPixels[i], this.depthHistory[0][i]);
                    argument &= compareFrameDepth(this.depthHistory[0][i], this.depthHistory[1][i]);
                    argument &= compareFrameDepth(this.depthHistory[1][i], this.depthHistory[2][i]);

                    if (!argument)
                        maskBytes[i] = 255;
                    else maskBytes[i] = 0;
                }

                // Copy from last frame
                for (int j = depthHistorySize - 1; j > 0; j--)
                    this.depthHistory[j][i] = this.depthHistory[j - 1][i];
                this.depthHistory[0][i] = this.depthMovementPixels[i];

                this.depthMovementPixels[i] = newValue;
            });

            DateTime start = DateTime.Now;
            
            // Extract LoI points from new frame
            using (var depthImage = this.GetFilledFullSize(maskBytes))
            using (var downsampled = depthImage
                .PyrDown()
                .SmoothBlur(10, 10)
                .PyrUp()
                .ThresholdToZero(new Gray(50))
                .Erode(3))
            {
                // Visualize downsampled image
                if (isVisualizationDownsampledEnabled)
                    this.VisualizeDownsampled(downsampled);

                // Extract countours
                var hierachy = new Mat();
                var contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(
                    downsampled,
                    contours,
                    hierachy,
                    RetrType.External,
                    ChainApproxMethod.ChainApproxSimple);

                // Cast to CvContours
                var rects = ToCvContour(contours).ToList();

                // Visualize contours
                if (isVisualizeContoursEnabled)
                    this.VisualizeContours(rects);

                // Retrieve locations of interest points
                var loiPoints = new List<LoiPoint>();
                foreach (var rect in rects)
                {
                    // Calculate center of contour
                    var moments = CvInvoke.Moments(new VectorOfPoint(rect.Points));
                    var x = moments.M10 / moments.M00;
                    var y = moments.M01 / moments.M00;
                    var contourCenter = new System.Drawing.Point((int)x, (int)y);

                    // Ignore contours out of rect
                    if (contourCenter.X <= rect.BoundingBox.Left ||
                        contourCenter.Y <= rect.BoundingBox.Top ||
                        contourCenter.X >= rect.BoundingBox.Right ||
                        contourCenter.Y >= rect.BoundingBox.Bottom)
                        continue;

                    // Fill the contour and apply the contour mask to the depth image
                    using (var contourCannyFull = this.GetFilledFullSize(this.depthMovementPixels))
                    using (var contourCanny = contourCannyFull.Copy(rect.BoundingBox))
                    using (var contourFlood = this.GetEmptyFullSize())
                    using (var contourFloodMask = this.GetEmptyFullSize(2, 2))
                    {
                        Rectangle contourFloodRect;
                        CvInvoke.DrawContours(
                            contourFlood,
                            new VectorOfVectorOfPoint(new VectorOfPoint(rect.Points)),
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
                        using (var maskMask = depthImage.Copy(rect.BoundingBox))
                        using (var contourCannyMaskedBound = contourCannyBound.Copy(maskMask))
                        {
                            // Find min depth point of contour canny
                            var contourMinDepth =
                                contourCannyMaskedBound.Bytes.Min(b => b > 70 ? b : (byte)255);

                            // Save the location with depth data
                            var loiPoint = new LoiPoint(contourCenter, contourMinDepth);
                            loiPoints.Add(loiPoint);

                            // Visualize LoI point
                            if (isVisualizationLoiPointEnabled)
                                this.VisualizeLoiPoint(contourCannyMaskedBound, rect, loiPoint);
                        }

                    }

                    this.ProcessRawLocationsOfInterest(loiPoints);
                }
            }

            System.Diagnostics.Debug.WriteLine(DateTime.Now - start);

            this.depthFilled++;
        }

        private bool isVisualizationDownsampledEnabled = false;
        private bool isVisualizationLoiPointEnabled = false;
        private bool isVisualizeContoursEnabled = false;

        private void VisualizeDownsampled(Image<Gray, byte> downsampled)
        {
            this.depthCvBitmap = ToBitmapSource(downsampled);
            this.CvImageContainer.Source = this.depthCvBitmap;
        }

        private void VisualizeLoiPoint(Image<Gray, byte> contourCanny, IContour rect, LoiPoint loiPoint)
        {
            // Find first pixel in contour that matches LoI point depth
            System.Drawing.Point? contourMinDepthPixelPoint = null;
            for (var i = 1; i < contourCanny.Height; i++)
            {
                for (var j = 1; j < contourCanny.Width; j++)
                {
                    // Ignore if point mot of required depth
                    if (contourCanny.Data[i, j, 0] != loiPoint.Depth)
                        continue;

                    contourMinDepthPixelPoint = new System.Drawing.Point(j, i);
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
            var contourDepthPoint = new System.Drawing.Point(
                contourMinDepthPixelPoint.Value.X + rect.BoundingBox.X,
                contourMinDepthPixelPoint.Value.Y + rect.BoundingBox.Y);

            // Flood the rect near min depth point inside contour
            using (var depthMovementImage = this.GetFilledFullSize(this.depthMovementPixels))
            using (var floodRectMask = new Image<Gray, byte>(
                this.depthFrameDescription.Width + 2,
                this.depthFrameDescription.Height + 2))
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


                this.depthCvBitmap2 = ToBitmapSource(depthMovementImage);
                this.CvImageContainer2.Source = this.depthCvBitmap2;
            }
        }

        private void VisualizeContours(IEnumerable<IContour> contours)
        {
            if (contours == null) return;
            var contoursList = contours as IList<IContour> ?? contours.ToList();

            using (var cvImage = this.GetEmptyFullSize())
            {
                // Visualize contours and draw bounding rectangles
                CvInvoke.DrawContours(cvImage, new VectorOfVectorOfPoint(contoursList.Select(c => c.Points).ToArray()), -1, new MCvScalar(255));
                foreach (var contour in contoursList)
                    CvInvoke.Rectangle(cvImage, contour.BoundingBox, new MCvScalar(255));

                // Show image
                this.depthCvBitmap1 = ToBitmapSource(cvImage);
                this.CvImageContainer1.Source = this.depthCvBitmap1;
            }
        }


        private static IEnumerable<CvContour> ToCvContour(VectorOfVectorOfPoint contours)
        {
            // Determine contours bounding rectangles
            return contours
                .ToArrayOfArray()
                .Select(contour => new CvContour(contour));
        }

        public void ProcessRawLocationsOfInterest(IEnumerable<LoiPoint> locations)
        {
            foreach (var location in locations)
                System.Diagnostics.Debug.WriteLine($"Got LoI: {location.Location} ({location.Depth})");
        }

        private Image<Gray, byte> GetFilledFullSize(byte[] data)
        {
            var image = this.GetEmptyFullSize();
            image.Bytes = data;
            return image;
        }

        private Image<Gray, byte> GetEmptyFullSize(int sizeWidthOffset = 0, int sizeHeightOffset = 0)
        {
            return new Image<Gray, byte>(
                this.depthFrameDescription.Width + sizeWidthOffset,
                this.depthFrameDescription.Height + sizeHeightOffset);
        }

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (var source = image.Bitmap)
            {
                var ptr = source.GetHbitmap(); //obtain the Hbitmap

                var bs = Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        private bool CheckInBox(RectF nearBox, float nearBoxDepth, RectF farBox, float farBoxDepth, PointF location, float depth)
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

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            //this.depthBitmap.WritePixels(
            //    new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
            //    this.depthMovementPixels,
            //    this.depthBitmap.PixelWidth,
            //    0);

            // Combine masks
            var imagePixels = new byte[this.depthMovementPixels.Length * 3];
            for (int i = 0; i < this.depthMovementPixels.Length; i++)
            {
                var val = this.maskBytes[i] > 0 ? (byte) 255 : this.depthMovementPixels[i];
                imagePixels[i * 3 + 0] = val;
                imagePixels[i * 3 + 1] = this.depthMovementPixels[i];
                imagePixels[i * 3 + 2] = this.depthMovementPixels[i];
                //imagePixels[i * 3 + 0] = val;
                //imagePixels[i * 3 + 1] = val;
                //imagePixels[i * 3 + 2] = val;
            }

            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);

            this.depthMovementBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                imagePixels,
                this.depthBitmap.PixelWidth*3,
                0);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
