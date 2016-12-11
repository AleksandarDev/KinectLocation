using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using KinectLocation;

namespace Microsoft.Samples.Kinect.DepthBasics
{
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
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Kitchen", 0, 0, 255, new Size(190, 800), 90));
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Bathroom", 220, 0, 255, new Size(40, 800), 60));
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Hall", 190, 0, 255, new Size(90, 800), 90));
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Bedroom", 270, 0, 190, new Size(90, 800), 40));
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Livingroom", 0, 0, 140, new Size(400, 800), 120));
            this.locationHandler.RegisterVolume(new VoiBox(
                "On Terrace", 400, 0, 90, new Size(150, 800), 80));
            this.locationHandler.OnLocations+= LocationHandlerOnOnLocations;

            this.kinectDepthLocationProcessor = new KinectDepthLocationProcessor(this.locationHandler);
            this.kinectDepthLocationProcessor.Start();
            this.kinectDepthLocationProcessor.IsVisualizationDepthImageEnabled = true;
            //this.kinectDepthLocationProcessor.IsVisualizationDownsampledEnabled = true;
            //this.kinectDepthLocationProcessor.IsVisualizationContoursEnabled = true;
            //this.kinectDepthLocationProcessor.IsVisualizationLoiPointsEnabled = true;
            this.kinectDepthLocationProcessor.PropertyChanged += KinectDepthLocationProcessorOnPropertyChanged;
        }

        private int lastLocationsPointer = 0;
        private string[] lastLocations = new string[4];

        private void LocationHandlerOnOnLocations(object sender, LocationHandlerLocationAvailableEventArgs args)
        {
            var locationsList = args.Locations.ToList();
            var invalidLocations = locationsList.Where(l => l.Point.Location.X < 10 || l.Point.Location.Y < 10 || l.Point.Depth > 254).ToList();
            invalidLocations.ForEach(l => locationsList.Remove(l));

            if (!locationsList.Any())
                return;

            this.lastLocations[this.lastLocationsPointer++] = locationsList.First().Id;
            this.lastLocationsPointer = this.lastLocationsPointer % this.lastLocations.Length;

            var location = lastLocations.GroupBy(l => l).OrderByDescending(g => g.Count()).FirstOrDefault();
            this.LocationIdTextBlock.Text = location?.Key;
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
