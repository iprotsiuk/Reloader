using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class DummyTargetDamageable : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameObject _impactMarkerPrefab;
        [SerializeField] private Transform _markersRoot;

        public void ApplyDamage(ProjectileImpactPayload payload)
        {
            if (_impactMarkerPrefab == null)
            {
                return;
            }

            var parent = _markersRoot != null ? _markersRoot : transform;
            var markerGo = Instantiate(_impactMarkerPrefab, payload.Point, Quaternion.identity, parent);
            var marker = markerGo.GetComponent<TargetImpactMarker>();
            if (marker != null)
            {
                marker.Place(payload.Point, payload.Normal);
            }
            else
            {
                markerGo.transform.position = payload.Point;
                if (payload.Normal.sqrMagnitude > 0.0001f)
                {
                    markerGo.transform.rotation = Quaternion.LookRotation(payload.Normal.normalized);
                }
            }

        }
    }
}
