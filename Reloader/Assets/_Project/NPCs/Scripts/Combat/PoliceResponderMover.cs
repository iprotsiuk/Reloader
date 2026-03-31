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
        private float _searchAngleDegrees;
        private bool _searchAngleInitialized;

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
            var planarDistanceToCenter = PlanarDistance(transform.position, searchCenter);
            var radius = Mathf.Max(0f, _searchRadiusMeters);
            var threshold = Mathf.Max(0.01f, _arrivalThresholdMeters);

            if (planarDistanceToCenter > Mathf.Max(radius, threshold))
            {
                MoveTowardsPlanar(searchCenter);
                return;
            }

            _searchAngleDegrees = Mathf.Repeat(
                _searchAngleDegrees + Mathf.Max(0f, _searchOrbitDegreesPerSecond) * Time.deltaTime,
                360f);

            var radians = _searchAngleDegrees * Mathf.Deg2Rad;
            var orbitOffset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
            MoveTowardsPlanar(searchCenter + orbitOffset);
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

            _searchAngleDegrees = ComputeInitialSearchAngleDegrees(gameObject.name);
            _searchAngleInitialized = true;
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
