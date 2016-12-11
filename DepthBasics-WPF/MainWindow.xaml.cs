using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using PointF = System.Drawing.PointF;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public class LocationHandler : ILocationHandler
    {
        private bool CheckInBox(
            RectF nearBox, float nearBoxDepth, 
            RectF farBox, float farBoxDepth, 
            PointF location, float depth)
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

        public void ProcessRawLoiPoints(IEnumerable<ILoiPoint> points)
        {
            
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        private readonly ILocationHandler locationHandler;
        private readonly IKinectDepthLocationProcessor kinectDepthLocationProcessor;

        private BitmapSource depthCvBitmap1 = null;
        private BitmapSource depthCvBitmap2 = null;
        private BitmapSource depthCvBitmap3 = null;
        private BitmapSource depthCvBitmap4 = null;
        private BitmapSource depthCvBitmap5 = null;
        private BitmapSource depthCvBitmap6 = null;


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            this.locationHandler = new LocationHandler();
            this.kinectDepthLocationProcessor = new KinectDepthLocationProcessor(this.locationHandler);
            this.kinectDepthLocationProcessor.Start();
            this.kinectDepthLocationProcessor.IsVisualizationDepthImageEnabled = true;
            this.kinectDepthLocationProcessor.IsVisualizationDownsampledEnabled = true;
            this.kinectDepthLocationProcessor.IsVisualizationContoursEnabled = true;
            this.kinectDepthLocationProcessor.IsVisualizationLoiPointsEnabled = true;
            this.kinectDepthLocationProcessor.PropertyChanged += KinectDepthLocationProcessorOnPropertyChanged;
        }

        private void KinectDepthLocationProcessorOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == nameof(IKinectDepthLocationProcessor.VisualizationDepthImage))
                this.RefreshDepthImage();
            else if (propertyChangedEventArgs.PropertyName == nameof(IKinectDepthLocationProcessor.VisualizationDownsampled))
                this.RefreshDownsampledImage();
            else if (propertyChangedEventArgs.PropertyName == nameof(IKinectDepthLocationProcessor.VisualizationContours))
                this.RefreshContoursImage();
            else if (propertyChangedEventArgs.PropertyName == nameof(IKinectDepthLocationProcessor.VisualizationLoiPoints))
                this.RefreshLoiPointsImage();
        }

        private void RefreshDepthImage()
        {
            this.CvImageContainer1.Source = this.kinectDepthLocationProcessor.VisualizationDepthImage;
        }

        private void RefreshDownsampledImage()
        {
            this.CvImageContainer2.Source = this.kinectDepthLocationProcessor.VisualizationDownsampled;
        }

        private void RefreshContoursImage()
        {
            this.CvImageContainer3.Source = this.kinectDepthLocationProcessor.VisualizationContours;
        }

        private void RefreshLoiPointsImage()
        {
            this.CvImageContainer4.Source = this.kinectDepthLocationProcessor.VisualizationLoiPoints;
        }
    }
}
