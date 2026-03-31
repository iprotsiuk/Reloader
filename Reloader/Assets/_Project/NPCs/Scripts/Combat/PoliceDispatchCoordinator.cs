using System.Collections.Generic;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PoliceDispatchCoordinator : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _maxActiveDispatchCount = 4;

        private readonly Dictionary<Transform, DispatchEntry> _dispatchEntries = new Dictionary<Transform, DispatchEntry>();
        private readonly List<DispatchEntry> _sortedDispatchEntries = new List<DispatchEntry>();
        private ILawEnforcementEvents _subscribedLawEnforcementEvents;
        private PoliceHeatState _currentHeatState;
        private Vector3 _cachedDispatchSearchPoint;
        private bool _hasCachedDispatchSearchPoint;
        private int _activeResponderCount;
        private int _registeredResponderCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapRuntimeCoordinator()
        {
            if (FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var coordinatorGo = new GameObject(nameof(PoliceDispatchCoordinator));
            DontDestroyOnLoad(coordinatorGo);
            coordinatorGo.AddComponent<PoliceDispatchCoordinator>();
        }

        private void OnEnable()
        {
            SubscribeToLawEnforcementEvents();
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void OnDisable()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            UnsubscribeFromLawEnforcementEvents();
            SetAllDispatchComponentsEnabled(false);
            _hasCachedDispatchSearchPoint = false;
            _activeResponderCount = 0;
        }

        public PoliceHeatState CurrentHeatState => _currentHeatState;
        public int ActiveResponderCount => _activeResponderCount;
        public int RegisteredResponderCount => _registeredResponderCount;

        private void Update()
        {
            if (!ShouldStageDispatch())
            {
                return;
            }

            RefreshDispatchAssignments();
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
            if (!ShouldStageDispatch())
            {
                SetAllDispatchComponentsEnabled(false);
                _hasCachedDispatchSearchPoint = false;
                return;
            }

            RefreshDispatchAssignments();
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

        private bool ShouldStageDispatch()
        {
            return _currentHeatState.IsPlayerIdentified
                   && (_currentHeatState.Level == PoliceHeatLevel.ActivePursuit
                       || _currentHeatState.Level == PoliceHeatLevel.Search);
        }

        private void RefreshDispatchAssignments()
        {
            if (_currentHeatState.Level == PoliceHeatLevel.ActivePursuit)
            {
                if (!TryResolvePlayerTarget(out var playerTarget))
                {
                    SetAllDispatchComponentsEnabled(false);
                    return;
                }

                _cachedDispatchSearchPoint = playerTarget.position;
                _hasCachedDispatchSearchPoint = true;
                StageDispatchAtPoint(playerTarget.position);
                return;
            }

            if (!_hasCachedDispatchSearchPoint)
            {
                if (!TryResolvePlayerTarget(out var searchSeedTarget))
                {
                    SetAllDispatchComponentsEnabled(false);
                    return;
                }

                _cachedDispatchSearchPoint = searchSeedTarget.position;
                _hasCachedDispatchSearchPoint = true;
            }

            StageDispatchAtPoint(_cachedDispatchSearchPoint);
        }

        private void StageDispatchAtPoint(Vector3 selectionPoint)
        {
            GatherDispatchEntries();
            _registeredResponderCount = _sortedDispatchEntries.Count;
            if (_sortedDispatchEntries.Count == 0)
            {
                _activeResponderCount = 0;
                return;
            }

            var activeCount = Mathf.Min(Mathf.Max(1, _maxActiveDispatchCount), _sortedDispatchEntries.Count);
            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                var entry = _sortedDispatchEntries[i];
                entry.DistanceMeters = PlanarDistance(entry.Root.position, selectionPoint);
            }

            _sortedDispatchEntries.Sort(CompareDispatchEntries);
            _activeResponderCount = activeCount;

            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                SetDispatchEntryEnabled(_sortedDispatchEntries[i], i < activeCount);
            }
        }

        private void GatherDispatchEntries()
        {
            _dispatchEntries.Clear();
            _sortedDispatchEntries.Clear();

            var movers = FindObjectsByType<PoliceResponderMover>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < movers.Length; i++)
            {
                RegisterMover(movers[i]);
            }

            var shooters = FindObjectsByType<PoliceHostileShooter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < shooters.Length; i++)
            {
                RegisterShooter(shooters[i]);
            }

            foreach (var entry in _dispatchEntries.Values)
            {
                _sortedDispatchEntries.Add(entry);
            }
        }

        private void RegisterMover(PoliceResponderMover mover)
        {
            if (mover == null)
            {
                return;
            }

            var root = ResolveDispatchRoot(mover.transform);
            var entry = GetOrCreateEntry(root);
            entry.Mover = mover;
        }

        private void RegisterShooter(PoliceHostileShooter shooter)
        {
            if (shooter == null)
            {
                return;
            }

            var root = ResolveDispatchRoot(shooter.transform);
            var entry = GetOrCreateEntry(root);
            entry.Shooter = shooter;
        }

        private DispatchEntry GetOrCreateEntry(Transform root)
        {
            if (!_dispatchEntries.TryGetValue(root, out var entry))
            {
                entry = new DispatchEntry(root);
                _dispatchEntries[root] = entry;
            }

            return entry;
        }

        private void SetAllDispatchComponentsEnabled(bool isEnabled)
        {
            GatherDispatchEntries();
            _registeredResponderCount = _sortedDispatchEntries.Count;
            _activeResponderCount = isEnabled ? _sortedDispatchEntries.Count : 0;
            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                SetDispatchEntryEnabled(_sortedDispatchEntries[i], isEnabled);
            }
        }

        private static void SetDispatchEntryEnabled(DispatchEntry entry, bool isEnabled)
        {
            if (entry.Mover != null)
            {
                entry.Mover.enabled = isEnabled;
            }

            if (entry.Shooter != null)
            {
                entry.Shooter.enabled = isEnabled;
            }
        }

        private bool TryResolvePlayerTarget(out Transform playerTarget)
        {
            var bridge = FindFirstObjectByType<PlayerDeathContractBridge>(FindObjectsInactive.Include);
            if (bridge == null)
            {
                playerTarget = null;
                return false;
            }

            playerTarget = bridge.transform;
            return playerTarget != null;
        }

        private static int CompareDispatchEntries(DispatchEntry left, DispatchEntry right)
        {
            var distanceComparison = left.DistanceMeters.CompareTo(right.DistanceMeters);
            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            return left.Root.GetInstanceID().CompareTo(right.Root.GetInstanceID());
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var delta = new Vector3(a.x - b.x, 0f, a.z - b.z);
            return delta.magnitude;
        }

        private static Transform ResolveDispatchRoot(Transform candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            var spawnedCivilian = candidate.GetComponentInParent<MainTownPopulationSpawnedCivilian>(true);
            return spawnedCivilian != null ? spawnedCivilian.transform : candidate.root;
        }

        private sealed class DispatchEntry
        {
            public DispatchEntry(Transform root)
            {
                Root = root;
            }

            public Transform Root { get; }
            public PoliceResponderMover Mover { get; set; }
            public PoliceHostileShooter Shooter { get; set; }
            public float DistanceMeters { get; set; }
        }
    }
}
