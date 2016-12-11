using System;
using System.Collections.Generic;

namespace KinectLocation
{
    public class LocationHandlerLocationAvailableEventArgs : EventArgs
    {
        public LocationHandlerLocationAvailableEventArgs(IEnumerable<ILoi> locations)
        {
            this.Locations = locations ?? new List<ILoi>();
        }


        public IEnumerable<ILoi> Locations { get; }
    }
}