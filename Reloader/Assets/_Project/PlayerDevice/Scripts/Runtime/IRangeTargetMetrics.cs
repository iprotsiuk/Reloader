namespace Reloader.PlayerDevice.Runtime
{
    public interface IRangeTargetMetrics
    {
        string TargetId { get; }

        string DisplayName { get; }

        float DistanceMeters { get; }
    }
}
