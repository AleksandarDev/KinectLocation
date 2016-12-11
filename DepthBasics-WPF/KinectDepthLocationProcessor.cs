using System;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.DepthBasics.Annotations;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public class KinectDepthLocationProcessor : DepthLocationProcessor, IKinectDepthLocationProcessor
    {
        /// <summary>
        /// Map depth range to byte range.
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        private KinectSensor kinectSensor;
        private DepthFrameReader depthFrameReader;
        private FrameDescription frameDescription;

        private ImageSource visualizationDepthImage;
        private byte[] depthData;


        public KinectDepthLocationProcessor([NotNull] ILocationHandler locationHandler) : base(locationHandler)
        {
        }


        public void Start()
        {
            this.Initialize();
        }

        protected void Initialize()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();
            this.depthFrameReader.FrameArrived += DepthFrameSourceOnFrameCaptured;
            this.frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // Initialize data collections
            this.depthData = new byte[this.frameDescription.Width * this.frameDescription.Height];

            // Start the sensor
            this.kinectSensor.Open();

            base.Initialize(
                this.frameDescription.Width,
                this.frameDescription.Height);
        }

        private void DepthFrameSourceOnFrameCaptured(object sender, DepthFrameArrivedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.FrameReference == null)
                throw new NullReferenceException("FrameReference can't be null.");

            // Ignore frame if not initializes fully
            if (this.frameDescription == null)
                return;

            // Process frame
            using (var depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                    return;

                // The fastest way to process the body index data is to directly access 
                // the underlying buffer
                using (var depthBuffer = depthFrame.LockImageBuffer())
                {
                    // verify data and write the color data to the display bitmap
                    if (this.frameDescription.Width * this.frameDescription.Height ==
                        depthBuffer.Size / this.frameDescription.BytesPerPixel)
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth)
                        // we are setting maxDepth to the extreme potential depth threshold
                        const ushort maxDepth = ushort.MaxValue;
                        this.FillDepthData(
                            depthBuffer.UnderlyingBuffer,
                            depthBuffer.Size,
                            depthFrame.DepthMinReliableDistance,
                            maxDepth);

                        // Visualize depth frame
                        if (this.IsVisualizationDepthImageEnabled)
                            this.VisualizeDepthImage();

                        // Pass to implemented class (depth processor)
                        this.ProcessDepthFrame(this.depthData);
                    }
                }
            }
        }

        private unsafe void FillDepthData(
            IntPtr depthFrameData,
            uint depthFrameDataSize,
            ushort minDepth,
            ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            var frameData = (ushort*) depthFrameData;

            // convert depth to a visual representation
            Parallel.For(0, (int) (depthFrameDataSize / this.frameDescription.BytesPerPixel), i =>
            {
                // Get the depth for this pixel
                var depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                var newValue = (byte) (depth >= minDepth && depth <= maxDepth ? depth / MapDepthToByte : 0);

                // Assign previous and current pixel
                this.depthData[i] = newValue;
            });
        }

        private void VisualizeDepthImage()
        {
            this.VisualizationDepthImage = this.GetFilledFullSize(this.depthData).ToBitmapSource();
        }

        public bool IsVisualizationDepthImageEnabled { get; set; }

        public ImageSource VisualizationDepthImage
        {
            get { return this.visualizationDepthImage; }
            set
            {
                this.visualizationDepthImage = value;
                this.OnPropertyChanged();
            }
        }

        #region IDisposable implementation

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.depthData = null;

                if (this.depthFrameReader != null)
                {
                    this.depthFrameReader.Dispose();
                    this.depthFrameReader = null;
                }

                if (this.kinectSensor != null)
                {
                    this.kinectSensor.Close();
                    this.kinectSensor = null;
                }
            }

            base.Dispose(disposing);
        }

        #endregion /IDisposable implementation
    }
}