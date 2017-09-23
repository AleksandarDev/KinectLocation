using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;
using KinectLocation;
using RestSharp;
using Timer = System.Timers.Timer;

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

            this.locationHandler = new LocationHandlerDepthVisualizer();
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Kitchen", 0, 0, 255, new Size(190, 800), 90));
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Bathroom", 220, 0, 255, new Size(40, 800), 60));
            //this.locationHandler.RegisterVolume(new VoiBox(
            //    "In Hall", 190, 0, 255, new Size(90, 800), 90));
            //this.locationHandler.RegisterVolume(new VoiBox(
            //    "In Bedroom", 270, 0, 190, new Size(90, 800), 40));
            this.locationHandler.RegisterVolume(new VoiBox(
                "In Livingroom", 0, 0, 140, new Size(400, 800), 120));
            //this.locationHandler.RegisterVolume(new VoiBox(
            //    "On Terrace", 400, 0, 90, new Size(150, 800), 80));
            this.locationHandler.OnLocations+= LocationHandlerOnOnLocations;

            this.kinectDepthLocationProcessor = new KinectDepthLocationProcessor(this.locationHandler);
            this.kinectDepthLocationProcessor.Start();
            this.kinectDepthLocationProcessor.IsVisualizationDepthImageEnabled = true;
            //this.kinectDepthLocationProcessor.IsVisualizationDownsampledEnabled = true;
            //this.kinectDepthLocationProcessor.IsVisualizationContoursEnabled = true;
            this.kinectDepthLocationProcessor.IsVisualizationLoiPointsEnabled = true;
            this.kinectDepthLocationProcessor.PropertyChanged += KinectDepthLocationProcessorOnPropertyChanged;
        }

        private int lastLocationsPointer = 0;
        private string[] lastLocations = new string[4];
        private bool isKitchenSwitchOn = false;
        private bool isBathroomSwitchOn = false;
        private int isSendingCommand = 0;

        private System.Timers.Timer locationChangeTimer;

        private void ConfirmNewLocation(object state, ElapsedEventArgs elapsedEventArgs)
        {
            this.currentLocation = this.newLocation;
            this.locationChangeTimer.Stop();
            this.newLocation = null;
            Dispatcher.Invoke(() => this.LocationIdTextBlock.Text = this.currentLocation.Id);

            Debug.WriteLine("CONFIRMED " + this.currentLocation.Id);

            Task.Run(() => this.SendCommand());
        }

        private void SendCommand()
        {
            if (Interlocked.CompareExchange(ref this.isSendingCommand, 1, 0) == 1)
                return;

            Debug.WriteLine("SENDING COMMAND...");

            if (this.currentLocation?.Id == "In Kitchen")
            {
                if (!this.isKitchenSwitchOn)
                {
                    //this.isKitchenSwitchOn = true;
                    //new RestClient("http://192.168.0.230").Get(
                    //    new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=1"));
                }
            }
            else if (this.currentLocation?.Id == "In Bathroom")
            {
                if (!this.isBathroomSwitchOn)
                {
                    this.isBathroomSwitchOn = true;
                    new RestClient("http://192.168.0.231").Get(
                        new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=1"));
                }
            }
            else if (this.currentLocation?.Id == "In Bedroom")
            {
                if (!this.isBathroomSwitchOn)
                {
                    //this.isBathroomSwitchOn = true;
                    //new RestClient("http://192.168.0.233").Get(
                    //    new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=1"));
                }
            }
            else
            {
                if (this.isBathroomSwitchOn)
                {
                    //new RestClient("http://192.168.0.233").Get(
                    //    new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=0"));
                    //this.isBathroomSwitchOn = false;
                }
                if (this.isKitchenSwitchOn)
                {
                    //new RestClient("http://192.168.0.230").Get(
                    //    new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=0"));
                    //this.isKitchenSwitchOn = false;
                }
                if (this.isBathroomSwitchOn)
                {
                    this.isBathroomSwitchOn = false;
                    new RestClient("http://192.168.0.231").Get(
                        new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=0"));
                }
            }

            Debug.WriteLine("SENT COMMAND.");
            this.isSendingCommand = 0;
        }

        private ILoi currentLocation = null;
        private ILoi newLocation = null;

        private void ChangeLocation(ILoi location)
        {
            // Initial setup
            if (this.currentLocation == null)
                this.currentLocation = location;

            // Ignore if not changed
            if (location == null || this.currentLocation.Id == location.Id || this.newLocation?.Id == location.Id)
                return;

            if (this.locationChangeTimer == null)
            {
                this.locationChangeTimer = new Timer(500);
                this.locationChangeTimer.Elapsed += this.ConfirmNewLocation;
            }

            if (this.newLocation != null && location.Id != this.newLocation.Id)
            {
                this.locationChangeTimer.Stop();
                System.Diagnostics.Debug.WriteLine("CANCELED " + this.newLocation.Id);
            }

            this.newLocation = location;
            this.locationChangeTimer.Start();
            System.Diagnostics.Debug.WriteLine("SET " + location.Id);
        }

        private void LocationHandlerOnOnLocations(object sender, LocationHandlerLocationAvailableEventArgs args)
        {
            var locationsList = args.Locations.ToList();
            var invalidLocations = locationsList.Where(l => l.Point.Location.X < 10 || l.Point.Location.Y < 10 || l.Point.Depth > 254).ToList();
            invalidLocations.ForEach(l => locationsList.Remove(l));

            if (!locationsList.Any())
                return;

            var orderedLocations = locationsList.GroupBy(l => l.Id).OrderByDescending(l => l.Count());
            this.ChangeLocation(orderedLocations.FirstOrDefault()?.FirstOrDefault());

            //this.LocationIdTextBlock.Text = orderedLocations.FirstOrDefault()?.Key;

            //if (location?.Key == "In Kitchen")
            //{
            //    if (!this.isKitchenSwitchOn)
            //    {
            //        this.isKitchenSwitchOn = true;
            //        new RestClient("http://192.168.0.231").Get(
            //            new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=1"));
            //    }
            //}
            //else             
            //    {
            //    new RestClient("http://192.168.0.231").Get(
            //        new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=0"));
            //    this.isKitchenSwitchOn = false;
            //}

            //if (location?.Key == "In Kitchen")
            //{
            //    if (!this.isKitchenSwitchOn)
            //    {
            //        this.isKitchenSwitchOn = true;
            //        new RestClient("http://192.168.0.230").Get(
            //            new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=1"));
            //    }
            //}
            //else if (location?.Key == "In Bedroom")
            //{
            //    if (!this.isBathroomSwitchOn)
            //    {
            //        this.isBathroomSwitchOn = true;
            //        new RestClient("http://192.168.0.233").Get(
            //            new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=1"));
            //    }
            //}
            //else
            //{
            //    if (this.isBathroomSwitchOn)
            //    {
            //        new RestClient("http://192.168.0.233").Get(
            //            new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=0"));
            //        this.isBathroomSwitchOn = false;
            //    }
            //    if (this.isKitchenSwitchOn)
            //    {
            //        new RestClient("http://192.168.0.230").Get(
            //            new RestRequest("/api/relay/0?apikey=46AA68B3F1134768&value=0"));
            //        this.isKitchenSwitchOn = false;
            //    }
            //}
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

            //this.CvImageContainer5.Source = (this.locationHandler as LocationHandlerDepthVisualizer).VisualizeLocations(
            //    this.kinectDepthLocationProcessor.DepthData, this.kinectDepthLocationProcessor.FrameWidth).ToBitmapSource();
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
