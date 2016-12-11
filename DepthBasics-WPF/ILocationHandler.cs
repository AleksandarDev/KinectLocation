using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    public interface ILocationHandler
    {
        void ProcessRawLoiPoints(IEnumerable<ILoiPoint> points);
    }
}