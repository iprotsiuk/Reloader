using UnityEngine;

namespace Reloader.World.Runtime.Origin
{
    public sealed class DynamicOriginRebaseController : MonoBehaviour
    {
        [SerializeField] private PersistentPlayerRoot _persistentPlayerRoot;
        [SerializeField] private DynamicOriginRebaseState _rebaseState;
        [SerializeField] private StableWorldCoordinateBridge _coordinateBridge;
        [SerializeField] private float _rebaseDistanceMeters = 500f;
        [SerializeField] private float _rebaseCooldownSeconds = 1f;
        [SerializeField] private float _lastRebaseTime = float.NegativeInfinity;

        public PersistentPlayerRoot PersistentPlayerRoot => _persistentPlayerRoot;
        public DynamicOriginRebaseState RebaseState => _rebaseState;
        public StableWorldCoordinateBridge CoordinateBridge => _coordinateBridge;
        public float RebaseDistanceMeters => _rebaseDistanceMeters;
        public float RebaseCooldownSeconds => _rebaseCooldownSeconds;
        public float LastRebaseTime => _lastRebaseTime;

        public void Configure(PersistentPlayerRoot persistentPlayerRoot, DynamicOriginRebaseState rebaseState, StableWorldCoordinateBridge coordinateBridge)
        {
            _persistentPlayerRoot = persistentPlayerRoot;
            _rebaseState = rebaseState;
            _coordinateBridge = coordinateBridge;
        }

        public void ResetState()
        {
            _lastRebaseTime = float.NegativeInfinity;
        }
    }
}
