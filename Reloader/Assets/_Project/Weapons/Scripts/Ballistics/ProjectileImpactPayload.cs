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
            Vector3 sourcePoint = default)
        {
            ItemId = itemId;
            Point = point;
            Normal = normal;
            Damage = damage;
            HitObject = hitObject;
            SourcePoint = sourcePoint;
        }

        public string ItemId { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Damage { get; }
        public GameObject HitObject { get; }
        public Vector3 SourcePoint { get; }
    }
}
