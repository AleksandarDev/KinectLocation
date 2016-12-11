using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace KinectLocation
{
    public class LocationHandlerDepthVisualizer : LocationHandler
    {
        public Image<Rgb, byte> VisualizeLocations(byte[] depthData, int width)
        {
            // Prepare volumes color
            this.Volumes.ForEach(v => this.GetVolumeColor(v.Id));

            var colorData = new byte[depthData.Length / width, width, 3];
            Parallel.For(0, depthData.Length, i =>
            {
                var x = i % width;
                var y = i / width;

                var point = new LoiPoint(new Point(x, y), depthData[i]);

                colorData[y, x, 0] = depthData[i];
                colorData[y, x, 1] = depthData[i];
                colorData[y, x, 2] = depthData[i];

                foreach (var volume in this.Volumes)
                {
                    if (volume.DoesContain(point))
                    {
                        var color = this.GetVolumeColor(volume.Id);
                        colorData[y, x, 0] = color.Item1;
                        colorData[y, x, 1] = color.Item2;
                        colorData[y, x, 2] = color.Item3;
                    }
                }
            });

            return new Image<Rgb, byte>(colorData);
        }

        public Tuple<byte, byte, byte> GetVolumeColor(string volumeId)
        {
            if (!this.volumeColorMap.ContainsKey(volumeId))
            {
                this.volumeColorMap.Add(volumeId, this.availableColors[usedColors]);
                this.usedColors = (this.usedColors + 1) % this.availableColors.Count;
            }

            return this.volumeColorMap[volumeId];
        }

        private int usedColors;
        private readonly Dictionary<string, Tuple<byte, byte, byte>> volumeColorMap = new Dictionary<string, Tuple<byte, byte, byte>>();
        private readonly List<Tuple<byte, byte, byte>> availableColors = new List<Tuple<byte, byte, byte>>()
        {
            new Tuple<byte, byte, byte>(255,0,0),
            new Tuple<byte, byte, byte>(255,255,0),
            new Tuple<byte, byte, byte>(255,0,255),
            new Tuple<byte, byte, byte>(0,255,255),
            new Tuple<byte, byte, byte>(0,0,255),
            new Tuple<byte, byte, byte>(0,255,0),
        };
    }

    public class LocationHandler : ILocationHandler
    {
        protected readonly List<IVoi> Volumes = new List<IVoi>();

        public event LocationHandlerLocationAvailableEventHandler OnLocations;

        
        public void ProcessRawLoiPoints(IEnumerable<ILoiPoint> points)
        {
            var locationsOfInterest =
                from point in points
                from volume in Volumes
                where volume.DoesContain(point)
                select (ILoi)new Loi(volume.Id, DateTime.Now, point, volume);

            System.Diagnostics.Debug.WriteLine("----");
            foreach (var loiPoint in locationsOfInterest)
            {
                System.Diagnostics.Debug.WriteLine(loiPoint.Point.Location + "\t" + loiPoint.Point.Depth + "\t"  + loiPoint.Id);
            }

            var locationsList = locationsOfInterest.ToList();
            if (!locationsList.Any())
                return;

            this.OnLocations?.Invoke(this, new LocationHandlerLocationAvailableEventArgs(locationsList));
        }

        public void RegisterVolume(IVoi volume)
        {
            this.Volumes.Add(volume);
        }
    }
}