using UnityEngine;

namespace Reloader.Weapons.Ballistics
{
    public readonly struct ProjectileImpactPayload
    {
        public ProjectileImpactPayload(string itemId, Vector3 point, Vector3 normal, float damage, GameObject hitObject)
        {
            ItemId = itemId;
            Point = point;
            Normal = normal;
            Damage = damage;
            HitObject = hitObject;
        }

        public string ItemId { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Damage { get; }
        public GameObject HitObject { get; }
    }
}
