using System;
using System.Collections.Generic;
using System.Linq;

namespace KinectLocation
{
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