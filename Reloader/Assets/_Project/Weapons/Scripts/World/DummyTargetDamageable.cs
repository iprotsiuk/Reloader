using System.Collections.Generic;
using Reloader.PlayerDevice.Runtime;
using Reloader.PlayerDevice.World;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class DummyTargetDamageable : MonoBehaviour, IDamageable, IDeviceTargetMarkerClearable, IRangeTargetMetrics
    {
        [SerializeField] private GameObject _impactMarkerPrefab;
        [SerializeField] private Transform _markersRoot;
        [SerializeField] private string _targetId = "";
        [SerializeField] private string _displayName = "";
        [SerializeField] private float _authoritativeDistanceMeters = 100f;
        private readonly List<GameObject> _spawnedMarkers = new List<GameObject>();

        public string TargetId => string.IsNullOrWhiteSpace(_targetId) ? gameObject.name : _targetId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
        public float DistanceMeters => Mathf.Max(0f, _authoritativeDistanceMeters);

        public void ApplyDamage(ProjectileImpactPayload payload)
        {
            PlayerDeviceController.ActiveInstance?.IngestImpact(payload.Point, payload.HitObject, payload.SourcePoint);

            if (_impactMarkerPrefab == null)
            {
                return;
            }

            var parent = _markersRoot != null ? _markersRoot : transform;
            var markerGo = Instantiate(_impactMarkerPrefab, payload.Point, Quaternion.identity, parent);
            _spawnedMarkers.Add(markerGo);
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

        public void ClearTargetMarkers()
        {
            for (var i = _spawnedMarkers.Count - 1; i >= 0; i--)
            {
                var markerObject = _spawnedMarkers[i];
                if (markerObject != null)
                {
                    markerObject.transform.SetParent(null, worldPositionStays: true);
                    Destroy(markerObject);
                }

                _spawnedMarkers.RemoveAt(i);
            }

            var parent = _markersRoot != null ? _markersRoot : transform;
            var residualMarkers = parent.GetComponentsInChildren<TargetImpactMarker>(includeInactive: true);
            for (var i = 0; i < residualMarkers.Length; i++)
            {
                if (residualMarkers[i] != null)
                {
                    var markerObject = residualMarkers[i].gameObject;
                    markerObject.transform.SetParent(null, worldPositionStays: true);
                    Destroy(markerObject);
                }
            }
        }
    }
}
