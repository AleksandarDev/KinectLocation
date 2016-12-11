namespace KinectLocation
{
    public class LoiPoint : ILoiPoint
    {
        public LoiPoint(System.Drawing.Point location, byte depth)
        {
            this.Location = location;
            this.Depth = depth;
        }


        public System.Drawing.Point Location { get; set; }

        public byte Depth { get; set; }
    }
}
