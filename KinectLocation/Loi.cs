using System;

namespace KinectLocation
{
    public class Loi : ILoi
    {
        public Loi(string id, DateTime timestamp, ILoiPoint point, IVoi volume)
        {
            this.Id = id;
            this.TimeStamp = timestamp;
            this.Point = point;
            this.Volume = volume;
        }


        public string Id { get; }

        public DateTime TimeStamp { get; }

        public ILoiPoint Point { get; }

        public IVoi Volume { get; }
    }
}