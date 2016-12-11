namespace KinectLocation
{
    public interface IVoi
    {
        string Id { get; }

        bool DoesContain(ILoiPoint location);
    }
}