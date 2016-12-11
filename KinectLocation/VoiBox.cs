using System.Drawing;

namespace KinectLocation
{
    public class VoiBox : IVoiBox
    {
        public VoiBox(string id, float x, float y, float z, Size face, float depth)
        {
            this.Id = id;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Face = face;
            this.Depth = depth;
        }


        public bool DoesContain(ILoiPoint location)
        {
            if (location.Depth < this.Z - this.Depth ||
                location.Depth > this.Z)
                return false;

            if (!(location.Location.X > this.X) || 
                !(location.Location.X < this.X + this.Face.Width) ||
                !(location.Location.Y > this.Y) || 
                !(location.Location.Y < this.Y + this.Face.Height))
                return false;

            return true;
        }

        public string Id { get; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public SizeF Face { get; set; }

        public float Depth { get; set; }
    }
}