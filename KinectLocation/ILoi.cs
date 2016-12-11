using System;

namespace KinectLocation
{
    public interface ILoi
    {
        string Id { get; }
        ILoiPoint Point { get; }
        DateTime TimeStamp { get; }
        IVoi Volume { get; }
    }
}