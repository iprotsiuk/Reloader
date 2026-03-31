using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class PoliceResponderMover : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _moveSpeedMetersPerSecond = 3.5f;
        [SerializeField, Min(0f)] private float _arrivalThresholdMeters = 0.1f;
        [SerializeField, Min(0f)] private float _searchRadiusMeters = 2f;
        [SerializeField, Min(0f)] private float _searchOrbitDegreesPerSecond = 120f;
        [SerializeField] private bool _orientToMotion = true;

        private ILawEnforcementEvents _subscribedLawEnforcementEvents;
        private Transform _playerTarget;
        private PoliceHeatState _currentHeatState;
        private Vector3 _lastKnownPlayerPosition;
        private bool _hasLastKnownPlayerPosition;
        private float _searchOrbitPhaseDegrees;
        private float _fallbackSearchAnchorAngleDegrees;
        private bool _searchAngleInitialized;
        private int _dispatchSearchSlotIndex = -1;
        private int _dispatchSearchSlotCount;

        private void OnEnable()
        {
            EnsureSearchAngleInitialized();
            SubscribeToLawEnforcementEvents();
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void OnDisable()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            UnsubscribeFromLawEnforcementEvents();
        }

        private void Update()
        {
            if (!_currentHeatState.IsPlayerIdentified)
            {
                return;
            }

            switch (_currentHeatState.Level)
            {
                case PoliceHeatLevel.ActivePursuit:
                    TickActivePursuit();
                    break;
                case PoliceHeatLevel.Search:
                    TickSearch();
                    break;
            }
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SubscribeToLawEnforcementEvents();
        }

        private void HandleHeatChanged(PoliceHeatState state)
        {
            _currentHeatState = state;
            if (state.Level == PoliceHeatLevel.Clear || !state.IsPlayerIdentified)
            {
                _hasLastKnownPlayerPosition = false;
                return;
            }

            if (TryResolvePlayerTarget(out var playerTarget))
            {
                CacheLastKnownPlayerPosition(playerTarget.position);
            }
        }

        private void SubscribeToLawEnforcementEvents()
        {
            var next = RuntimeKernelBootstrapper.LawEnforcementEvents;
            if (ReferenceEquals(_subscribedLawEnforcementEvents, next))
            {
                return;
            }

            UnsubscribeFromLawEnforcementEvents();
            _subscribedLawEnforcementEvents = next;
            if (_subscribedLawEnforcementEvents != null)
            {
                _subscribedLawEnforcementEvents.OnHeatChanged += HandleHeatChanged;
            }

            RefreshHeatStateFromCurrentRuntime();
        }

        private void UnsubscribeFromLawEnforcementEvents()
        {
            if (_subscribedLawEnforcementEvents == null)
            {
                return;
            }

            _subscribedLawEnforcementEvents.OnHeatChanged -= HandleHeatChanged;
            _subscribedLawEnforcementEvents = null;
        }

        private void RefreshHeatStateFromCurrentRuntime()
        {
            var provider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            if (provider == null)
            {
                HandleHeatChanged(new PoliceHeatState(PoliceHeatLevel.Clear, CrimeType.Murder, 0f, false, 0, false));
                return;
            }

            HandleHeatChanged(provider.CurrentHeatState);
        }

        private void TickActivePursuit()
        {
            if (!TryResolvePlayerTarget(out var playerTarget))
            {
                return;
            }

            CacheLastKnownPlayerPosition(playerTarget.position);
            MoveTowardsPlanar(playerTarget.position);
        }

        private void TickSearch()
        {
            if (!_hasLastKnownPlayerPosition)
            {
                if (!TryResolvePlayerTarget(out var playerTarget))
                {
                    return;
                }

                CacheLastKnownPlayerPosition(playerTarget.position);
            }

            var searchCenter = new Vector3(_lastKnownPlayerPosition.x, transform.position.y, _lastKnownPlayerPosition.z);
            var radius = Mathf.Max(0f, _searchRadiusMeters);
            var threshold = Mathf.Max(0.01f, _arrivalThresholdMeters);
            var searchDestination = ResolveSearchDestination(searchCenter, radius);
            var planarDistanceToDestination = PlanarDistance(transform.position, searchDestination);

            if (planarDistanceToDestination > threshold)
            {
                MoveTowardsPlanar(searchDestination);
                return;
            }

            _searchOrbitPhaseDegrees = Mathf.Repeat(
                _searchOrbitPhaseDegrees + Mathf.Max(0f, _searchOrbitDegreesPerSecond) * Time.deltaTime,
                360f);

            MoveTowardsPlanar(ResolveSearchDestination(searchCenter, radius));
        }

        public void ConfigureDispatchSearchSlot(int slotIndex, int slotCount)
        {
            _dispatchSearchSlotCount = Mathf.Max(0, slotCount);
            if (_dispatchSearchSlotCount <= 1)
            {
                _dispatchSearchSlotIndex = -1;
                return;
            }

            _dispatchSearchSlotIndex = Mathf.Clamp(slotIndex, 0, _dispatchSearchSlotCount - 1);
        }

        public void ClearDispatchSearchSlot()
        {
            _dispatchSearchSlotIndex = -1;
            _dispatchSearchSlotCount = 0;
        }

        private void MoveTowardsPlanar(Vector3 destination)
        {
            var currentPosition = transform.position;
            var planarDestination = new Vector3(destination.x, currentPosition.y, destination.z);
            var nextPosition = Vector3.MoveTowards(
                currentPosition,
                planarDestination,
                Mathf.Max(0f, _moveSpeedMetersPerSecond) * Time.deltaTime);
            var delta = nextPosition - currentPosition;
            transform.position = nextPosition;

            if (!_orientToMotion)
            {
                return;
            }

            var planarDelta = new Vector3(delta.x, 0f, delta.z);
            if (planarDelta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(planarDelta.normalized, Vector3.up);
        }

        private bool TryResolvePlayerTarget(out Transform playerTarget)
        {
            playerTarget = _playerTarget;
            if (playerTarget != null)
            {
                return true;
            }

            var bridge = FindFirstObjectByType<PlayerDeathContractBridge>(FindObjectsInactive.Include);
            if (bridge == null)
            {
                return false;
            }

            _playerTarget = bridge.transform;
            playerTarget = _playerTarget;
            return playerTarget != null;
        }

        private void CacheLastKnownPlayerPosition(Vector3 worldPosition)
        {
            _lastKnownPlayerPosition = worldPosition;
            _hasLastKnownPlayerPosition = true;
        }

        private void EnsureSearchAngleInitialized()
        {
            if (_searchAngleInitialized)
            {
                return;
            }

            _fallbackSearchAnchorAngleDegrees = ComputeInitialSearchAngleDegrees(gameObject.name);
            _searchOrbitPhaseDegrees = 0f;
            _searchAngleInitialized = true;
        }

        private float ResolveSearchAnchorAngleDegrees()
        {
            if (_dispatchSearchSlotIndex < 0 || _dispatchSearchSlotCount <= 1)
            {
                return _fallbackSearchAnchorAngleDegrees;
            }

            return (360f * _dispatchSearchSlotIndex) / _dispatchSearchSlotCount;
        }

        private Vector3 ResolveSearchDestination(Vector3 searchCenter, float radius)
        {
            var orbitAngleDegrees = ResolveSearchAnchorAngleDegrees() + _searchOrbitPhaseDegrees;
            var radians = orbitAngleDegrees * Mathf.Deg2Rad;
            var orbitOffset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
            return searchCenter + orbitOffset;
        }

        private static float ComputeInitialSearchAngleDegrees(string stableKey)
        {
            unchecked
            {
                var hash = 17;
                if (!string.IsNullOrEmpty(stableKey))
                {
                    for (var i = 0; i < stableKey.Length; i++)
                    {
                        hash = (hash * 31) + stableKey[i];
                    }
                }

                return Mathf.Repeat(hash & 0x7fffffff, 360f);
            }
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var delta = new Vector3(a.x - b.x, 0f, a.z - b.z);
            return delta.magnitude;
        }
    }
}
