using UnityEngine;

namespace Reloader.Weapons.Ballistics
{
    public readonly struct ProjectileImpactPayload
    {
        public ProjectileImpactPayload(
            string itemId,
            Vector3 point,
            Vector3 normal,
            float damage,
            GameObject hitObject,
            Vector3? sourcePoint = null,
            Vector3? direction = null,
            float impactSpeedMetersPerSecond = 0f,
            float projectileMassGrains = 0f,
            float deliveredEnergyJoules = 0f)
        {
            ItemId = itemId;
            Point = point;
            Normal = normal;
            Damage = damage;
            HitObject = hitObject;
            SourcePoint = sourcePoint;
            Direction = ResolveDirection(direction);
            ImpactSpeedMetersPerSecond = impactSpeedMetersPerSecond;
            ProjectileMassGrains = projectileMassGrains;
            DeliveredEnergyJoules = deliveredEnergyJoules;
        }

        public string ItemId { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Damage { get; }
        public GameObject HitObject { get; }
        public Vector3? SourcePoint { get; }
        public Vector3 Direction { get; }
        public float ImpactSpeedMetersPerSecond { get; }
        public float ProjectileMassGrains { get; }
        public float DeliveredEnergyJoules { get; }

        private static Vector3 ResolveDirection(Vector3? direction)
        {
            if (!direction.HasValue)
            {
                return Vector3.forward;
            }

            var value = direction.Value;
            return value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.forward;
        }
    }
}
