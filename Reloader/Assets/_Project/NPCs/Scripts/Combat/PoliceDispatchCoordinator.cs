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
        [SerializeField, Min(0f)] private float _dispatchReassignmentHoldSeconds = 0.5f;
        [SerializeField, Min(0f)] private float _dispatchReplacementDistanceThresholdMeters = 1f;

        private readonly Dictionary<Transform, DispatchEntry> _dispatchEntries = new Dictionary<Transform, DispatchEntry>();
        private readonly List<DispatchEntry> _sortedDispatchEntries = new List<DispatchEntry>();
        private readonly List<Transform> _staleDispatchRoots = new List<Transform>();
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
            var currentTime = Time.unscaledTime;

            var selectedCount = 0;
            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                if (_sortedDispatchEntries[i].IsSelected)
                {
                    selectedCount++;
                }
            }

            for (var i = 0; i < _sortedDispatchEntries.Count && selectedCount < activeCount; i++)
            {
                var entry = _sortedDispatchEntries[i];
                if (entry.IsSelected)
                {
                    continue;
                }

                entry.MarkSelected(currentTime);
                selectedCount++;
            }

            while (selectedCount > activeCount && TryFindFarthestSelectedEntry(out var excessSelectedEntry))
            {
                excessSelectedEntry.ResetSelection();
                selectedCount--;
            }

            while (TryFindDispatchReplacementCandidate(
                       currentTime,
                       Mathf.Max(0f, _dispatchReassignmentHoldSeconds),
                       Mathf.Max(0f, _dispatchReplacementDistanceThresholdMeters),
                       out var replacementCandidate,
                       out var replacedEntry))
            {
                replacedEntry.ResetSelection();
                replacementCandidate.MarkSelected(currentTime);
            }

            var enabledCount = 0;
            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                var entry = _sortedDispatchEntries[i];
                SetDispatchEntryEnabled(entry, entry.IsSelected);
                if (entry.IsSelected)
                {
                    enabledCount++;
                }
            }

            _activeResponderCount = enabledCount;
        }

        private void GatherDispatchEntries()
        {
            _sortedDispatchEntries.Clear();
            _staleDispatchRoots.Clear();

            foreach (var entry in _dispatchEntries.Values)
            {
                entry.ClearRuntimeBindings();
            }

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

            foreach (var pair in _dispatchEntries)
            {
                var root = pair.Key;
                var entry = pair.Value;
                if (root == null || !entry.HasRuntimeBindings)
                {
                    _staleDispatchRoots.Add(root);
                    continue;
                }

                _sortedDispatchEntries.Add(entry);
            }

            for (var i = 0; i < _staleDispatchRoots.Count; i++)
            {
                _dispatchEntries.Remove(_staleDispatchRoots[i]);
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
                var entry = _sortedDispatchEntries[i];
                if (!isEnabled)
                {
                    entry.ResetSelection();
                }

                SetDispatchEntryEnabled(entry, isEnabled);
            }
        }

        private bool TryFindDispatchReplacementCandidate(
            float currentTime,
            float reassignmentHoldSeconds,
            float replacementDistanceThresholdMeters,
            out DispatchEntry replacementCandidate,
            out DispatchEntry replacedEntry)
        {
            replacementCandidate = null;
            replacedEntry = null;

            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                var candidate = _sortedDispatchEntries[i];
                if (candidate.IsSelected)
                {
                    continue;
                }

                var currentReplacedEntry = FindReplaceableSelectedEntry(
                    candidate.DistanceMeters,
                    currentTime,
                    reassignmentHoldSeconds,
                    replacementDistanceThresholdMeters);
                if (currentReplacedEntry == null)
                {
                    continue;
                }

                replacementCandidate = candidate;
                replacedEntry = currentReplacedEntry;
                return true;
            }

            return false;
        }

        private bool TryFindFarthestSelectedEntry(out DispatchEntry farthestSelectedEntry)
        {
            farthestSelectedEntry = null;
            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                var entry = _sortedDispatchEntries[i];
                if (!entry.IsSelected)
                {
                    continue;
                }

                if (farthestSelectedEntry == null || entry.DistanceMeters > farthestSelectedEntry.DistanceMeters)
                {
                    farthestSelectedEntry = entry;
                }
            }

            return farthestSelectedEntry != null;
        }

        private DispatchEntry FindReplaceableSelectedEntry(
            float candidateDistanceMeters,
            float currentTime,
            float reassignmentHoldSeconds,
            float replacementDistanceThresholdMeters)
        {
            DispatchEntry replaceableEntry = null;
            for (var i = 0; i < _sortedDispatchEntries.Count; i++)
            {
                var entry = _sortedDispatchEntries[i];
                if (!entry.IsSelected)
                {
                    continue;
                }

                if (currentTime - entry.SelectedAtUnscaledTime < reassignmentHoldSeconds)
                {
                    continue;
                }

                if (entry.DistanceMeters - candidateDistanceMeters <= replacementDistanceThresholdMeters)
                {
                    continue;
                }

                if (replaceableEntry == null || entry.DistanceMeters > replaceableEntry.DistanceMeters)
                {
                    replaceableEntry = entry;
                }
            }

            return replaceableEntry;
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
            public float SelectedAtUnscaledTime { get; private set; }
            public bool IsSelected { get; private set; }
            public bool HasRuntimeBindings => Mover != null || Shooter != null;

            public void ClearRuntimeBindings()
            {
                Mover = null;
                Shooter = null;
            }

            public void MarkSelected(float currentTime)
            {
                if (!IsSelected)
                {
                    SelectedAtUnscaledTime = currentTime;
                }

                IsSelected = true;
            }

            public void ResetSelection()
            {
                IsSelected = false;
                SelectedAtUnscaledTime = 0f;
            }
        }
    }
}
