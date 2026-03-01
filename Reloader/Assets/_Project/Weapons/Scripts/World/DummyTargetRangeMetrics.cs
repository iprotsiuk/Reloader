using Reloader.PlayerDevice.Runtime;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class DummyTargetRangeMetrics : MonoBehaviour, IRangeTargetMetrics
    {
        [SerializeField] private string _targetId = "";
        [SerializeField] private string _displayName = "";
        [SerializeField] private float _authoritativeDistanceMeters = 100f;

        public string TargetId => string.IsNullOrWhiteSpace(_targetId) ? gameObject.name : _targetId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
        public float DistanceMeters => Mathf.Max(0f, _authoritativeDistanceMeters);

        public void Configure(string targetId, string displayName, float authoritativeDistanceMeters)
        {
            _targetId = targetId;
            _displayName = displayName;
            _authoritativeDistanceMeters = authoritativeDistanceMeters;
        }
    }
}
