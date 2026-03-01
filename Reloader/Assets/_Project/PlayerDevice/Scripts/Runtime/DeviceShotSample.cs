using UnityEngine;

namespace Reloader.PlayerDevice.Runtime
{
    public readonly struct DeviceShotSample
    {
        public DeviceShotSample(Vector2 targetPlanePointMeters, float distanceMeters)
        {
            TargetPlanePointMeters = targetPlanePointMeters;
            DistanceMeters = distanceMeters;
        }

        public Vector2 TargetPlanePointMeters { get; }

        public float DistanceMeters { get; }

        public bool IsValid => DistanceMeters > 0f;
    }
}
