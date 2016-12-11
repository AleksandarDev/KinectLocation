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
using System.Windows.Documents;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
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
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

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
                    newValue = (byte)this.depthMovementPixels[i];

                // Filter out static objects
                Func<byte, byte, bool> compareFrameDepth =
                    (d1, d2) =>
                    {
                        return Math.Abs(d1 - d2) <= 3;
                    };
                if (this.depthFilled > depthHistorySize*10)
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


            using (Image<Gray, byte> depthImage = new Image<Gray, byte>(
                this.depthFrameDescription.Width,
                this.depthFrameDescription.Height))
            {
                depthImage.Bytes = maskBytes;

                using (var blur = depthImage.PyrDown().SmoothBlur(10, 10).PyrUp())
                {
                    using (var thr = blur.ThresholdToZero(new Gray(50)))
                    {
                        using (var edge = thr.Canny(1, 1))
                        {
                            Mat hierachy = new Mat();
                            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

                            Image<Gray, byte> eroded = new Image<Gray, byte>(this.depthFrameDescription.Width,
                                this.depthFrameDescription.Height);
                            CvInvoke.Erode(thr, eroded, new Mat(), new System.Drawing.Point(-1, -1), 3, BorderType.Default, new MCvScalar(255));

                            this.depthCvBitmap = ToBitmapSource(eroded);
                            this.CvImageContainer.Source = this.depthCvBitmap;

                            CvInvoke.FindContours(eroded, contours, hierachy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                            using (Image<Gray, byte> cvImage = new Image<Gray, byte>(
                                this.depthFrameDescription.Width,
                                this.depthFrameDescription.Height))
                            {
                                var rects = new List<Tuple<Rectangle, List<System.Drawing.PointF>>>();
                                foreach (var contour in contours.ToArrayOfArray())
                                {
                                    var contourF = contour.Select(p => new System.Drawing.PointF(p.X, p.Y)).ToArray();
                                    var rect = Emgu.CV.PointCollection.BoundingRectangle(contourF);

                                    // Remove small rects
                                    const int minRectSizePass1 = 30;
                                    if (rect.Width < minRectSizePass1 && rect.Height < minRectSizePass1)
                                        continue;

                                    rects.Add(new Tuple<Rectangle, List<System.Drawing.PointF>>(rect, contourF.ToList()));
                                }

                                CvInvoke.DrawContours(cvImage, contours, -1, new MCvScalar(255));

                                foreach (var tuple in rects)
                                {
                                    CvInvoke.Rectangle(cvImage, tuple.Item1, new MCvScalar(255), 1);
                                }

                                // Remove small rects
                                const int minRectSize = 10;
                                rects = rects.Where(r => r.Item1.Width > minRectSize && r.Item1.Height > minRectSize).ToList();

                                using (var floodSource = new Image<Gray, byte>(this.depthFrameDescription.Width,
                                    this.depthFrameDescription.Height))
                                {
                                    floodSource.Bytes = this.depthMovementPixels;
                                    using (var floodSourceCopy = floodSource.Copy())
                                    {
                                        var loiPoints = new List<LoiPoint>();

                                        foreach (var rect in rects)
                                        {
                                            var moments = CvInvoke.Moments(new VectorOfPointF(rect.Item2.ToArray()),
                                                false);
                                            var x = moments.M10 / moments.M00;
                                            var y = moments.M01 / moments.M00;
                                            var contourCenter = new System.Drawing.Point((int) x, (int) y);

                                            var rectCenter = new System.Drawing.Point(
                                                rect.Item1.X + rect.Item1.Width / 2,
                                                rect.Item1.Y + rect.Item1.Height / 2);

                                            // Ignore contours out of rect
                                            if (contourCenter.X <= rect.Item1.Left ||
                                                contourCenter.Y <= rect.Item1.Top ||
                                                contourCenter.X >= rect.Item1.Right ||
                                                contourCenter.Y >= rect.Item1.Bottom)
                                                continue;

                                            // Ignore contours out of image
                                            if (contourCenter.X < 0 || 
                                                contourCenter.Y < 0 ||
                                                contourCenter.X >= floodSourceCopy.Width ||
                                                contourCenter.Y >= floodSourceCopy.Height)
                                                continue;

                                            var contourCanny = new Image<Gray, byte>(
                                                this.depthFrameDescription.Width,
                                                this.depthFrameDescription.Height);
                                            contourCanny.Bytes = this.depthMovementPixels;
                                            contourCanny = contourCanny.Copy(rect.Item1);

                                            Rectangle contourFloodRect;
                                            var contourFlood = new Image<Gray, byte>(
                                                this.depthFrameDescription.Width,
                                                this.depthFrameDescription.Height);
                                            var contourFloodMask = new Image<Gray, byte>(
                                                this.depthFrameDescription.Width + 2,
                                                this.depthFrameDescription.Height + 2);
                                            CvInvoke.DrawContours(
                                                contourFlood,
                                                new VectorOfVectorOfPoint(new[]
                                                {
                                                    rect.Item2.Select(
                                                        p => new System.Drawing.Point((int) p.X, (int) p.Y)).ToArray()
                                                }),
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
                                            contourFlood = contourFlood.Copy(rect.Item1);

                                            contourCanny = contourCanny.Copy(contourFlood);

                                            var maskMask = depthImage.Copy(rect.Item1);

                                            contourCanny = contourCanny.Copy(maskMask);

                                            // Find min depth point of contour canny
                                            var contourMinDepthPixelPoint = new System.Drawing.Point();
                                            var contourMinDepth = contourCanny.Bytes.Min(b => b > 70 ? b : (byte) 255);
                                            for (var i = 0; i < contourCanny.Height; i++)
                                            {
                                                var found = false;
                                                for (int j = 0; j < contourCanny.Width; j++)
                                                {
                                                    if (contourCanny.Data[i,j,0] == contourMinDepth)
                                                    {
                                                        contourMinDepthPixelPoint = new System.Drawing.Point(j, i);
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                                if (found) break;
                                            }

                                            // Transform min depth point of contour to full image point
                                            var contourDepthPoint = new System.Drawing.Point(
                                                (contourMinDepthPixelPoint.X + rect.Item1.X),
                                                (contourMinDepthPixelPoint.Y + rect.Item1.Y));

                                            // Flood the rect near min depth point inside contour
                                            var floodRectMask = new Image<Gray, byte>(
                                                this.depthFrameDescription.Width + 2,
                                                this.depthFrameDescription.Height + 2);
                                            Rectangle floodRect;
                                            CvInvoke.Rectangle(floodSourceCopy, rect.Item1, new MCvScalar(0), 1);
                                            CvInvoke.FloodFill(
                                                floodSourceCopy, floodRectMask,
                                                contourDepthPoint,
                                                new MCvScalar(255), out floodRect, new MCvScalar(1),
                                                new MCvScalar(1));

                                            // Save the location with depth data
                                            loiPoints.Add(new LoiPoint(contourCenter, contourMinDepth));
                                        }

                                        this.ProcessRawLocationsOfInterest(loiPoints);

                                        this.depthCvBitmap1 = ToBitmapSource(cvImage);
                                        this.CvImageContainer1.Source = this.depthCvBitmap1;

                                        this.depthCvBitmap2 = ToBitmapSource(floodSourceCopy);
                                        this.CvImageContainer2.Source = this.depthCvBitmap2;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            this.depthFilled++;
        }

        public class LoiPoint
        {
            public LoiPoint(System.Drawing.Point location, byte depth)
            {
                this.Location = location;
                this.Depth = depth;
            }


            public System.Drawing.Point Location { get; set; }

            public byte Depth { get; set; }
        }

        public void ProcessRawLocationsOfInterest(IEnumerable<LoiPoint> locations)
        {
            
        }

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

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
