using System.Collections.Generic;

namespace KinectLocation
{
    public interface ILocationHandler
    {
        event LocationHandlerLocationAvailableEventHandler OnLocations;

        void RegisterVolume(IVoi volume);

        void ProcessRawLoiPoints(IEnumerable<ILoiPoint> points);
    }
}