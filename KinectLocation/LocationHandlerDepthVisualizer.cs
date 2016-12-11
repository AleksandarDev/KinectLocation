using System.Collections.Generic;
using System.Drawing;
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
            Parallel.For(0, depthData.Length,i =>
            {
                var x = i % width;
                var y = i / width;
                var d = depthData[i];

                var point = new LoiPoint(new Point(x, y), d);

                bool didMatch = false;
                for (var index = 0; index < this.Volumes.Count; index++)
                {
                    var volume = this.Volumes[index];
                    if (volume.DoesContain(point))
                    {
                        var color = this.GetVolumeColorForce(volume.Id);
                        colorData[y, x, 0] = color[0];
                        colorData[y, x, 1] = color[1];
                        colorData[y, x, 2] = color[2];
                        didMatch = true;
                        break;
                    }
                }
                if (!didMatch)
                {
                    colorData[y, x, 0] = d;
                    colorData[y, x, 1] = d;
                    colorData[y, x, 2] = d;
                }
            });

            return new Image<Rgb, byte>(colorData);
        }

        public byte[] GetVolumeColorForce(string volumeId)
        {
            return this.volumeColorMap[volumeId];
        }

        public byte[] GetVolumeColor(string volumeId)
        {
            if (!this.volumeColorMap.ContainsKey(volumeId))
            {
                this.volumeColorMap.Add(volumeId, this.availableColors[usedColors]);
                this.usedColors = (this.usedColors + 1) % this.availableColors.Count;
            }

            return this.volumeColorMap[volumeId];
        }

        private int usedColors;
        private readonly Dictionary<string, byte[]> volumeColorMap = new Dictionary<string, byte[]>();
        private readonly List<byte[]> availableColors = new List<byte[]>
        {
            new[]{(byte)255, (byte)0, (byte)0 },
            new[]{(byte)255, (byte)255, (byte)0 },
            new[]{(byte)0, (byte)255, (byte)0 },
            new[]{(byte)0, (byte)0, (byte)255 },
            new[]{(byte)255, (byte)0, (byte)255 },
            new[]{(byte)0, (byte)255, (byte)255 },
            new[]{(byte)125, (byte)125, (byte)255 },
        };
    }
}