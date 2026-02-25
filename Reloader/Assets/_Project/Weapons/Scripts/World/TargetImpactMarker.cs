using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class TargetImpactMarker : MonoBehaviour
    {
        [SerializeField] private float _surfaceOffsetMeters;

        public void Place(Vector3 point, Vector3 normal)
        {
            var safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
            transform.position = point + (safeNormal * _surfaceOffsetMeters);
            transform.rotation = Quaternion.LookRotation(safeNormal);
        }
    }
}
