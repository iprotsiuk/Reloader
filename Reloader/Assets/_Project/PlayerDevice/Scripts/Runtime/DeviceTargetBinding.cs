namespace Reloader.PlayerDevice.Runtime
{
    public readonly struct DeviceTargetBinding
    {
        public DeviceTargetBinding(string targetId, string displayName, float distanceMeters)
        {
            TargetId = targetId;
            DisplayName = displayName;
            DistanceMeters = distanceMeters;
        }

        public string TargetId { get; }

        public string DisplayName { get; }

        public float DistanceMeters { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(TargetId);

        public static DeviceTargetBinding None => default;
    }
}
