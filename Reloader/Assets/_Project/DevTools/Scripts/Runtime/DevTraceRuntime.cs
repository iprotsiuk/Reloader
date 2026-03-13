using System;
using System.Collections.Generic;
using Reloader.Core.Runtime;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevTraceRuntime : IDisposable
    {
        private static DevTraceRuntime s_activeRuntime;

        private sealed class ProjectileTracePath
        {
            public readonly List<Vector3> Points = new();
            public DevTraceSegmentView SegmentView;
            public bool Completed;
        }

        private sealed class ProjectilePathObserver : WeaponProjectile.IPathObserver
        {
            private readonly DevTraceRuntime _owner;
            private readonly ProjectileTracePath _tracePath = new();

            public ProjectilePathObserver(DevTraceRuntime owner)
            {
                _owner = owner;
            }

            public void RecordSegment(Vector3 startPoint, Vector3 endPoint)
            {
                _owner.RecordProjectileSegment(_tracePath, startPoint, endPoint);
            }

            public void Complete(Vector3 terminalPoint, bool didHit)
            {
                _owner.CompleteProjectileTrace(_tracePath, terminalPoint);
            }
        }

        private sealed class Driver : MonoBehaviour
        {
            public DevTraceRuntime Owner { get; set; }

            private void Update()
            {
                Owner?.Tick(Time.unscaledTime);
            }
        }

        private sealed class PendingShot
        {
            public PendingShot(Vector3 origin, Vector3 direction, float fallbackAt, float cleanupAt)
            {
                Origin = origin;
                Direction = direction;
                FallbackAt = fallbackAt;
                CleanupAt = cleanupAt;
            }

            public Vector3 Origin { get; }
            public Vector3 Direction { get; }
            public float FallbackAt { get; }
            public float CleanupAt { get; }
            public DevTraceSegmentView SegmentView { get; set; }
        }

        private readonly DevToolsState _state;
        private readonly Dictionary<string, List<PendingShot>> _pendingShotsByItemId = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<DevTraceSegmentView> _segmentPool = new();
        private readonly Color _traceColor = new(1f, 0.7f, 0.1f, 1f);
        private readonly float _fallbackDistanceMeters = 120f;
        private readonly float _pendingFallbackDelaySeconds = 0.05f;
        private readonly float _pendingCleanupLifetimeSeconds = 10f;
        private readonly GameObject _root;
        private readonly Driver _driver;
        private IWeaponEvents _weaponEvents;
        private bool _disposed;
        private int _pendingObserverShotClaims;

        public DevTraceRuntime(DevToolsState state, IWeaponEvents weaponEvents = null)
        {
            _state = state ?? new DevToolsState();
            s_activeRuntime = this;
            _root = new GameObject("DevTraceRuntime");
            _root.hideFlags = HideFlags.HideAndDontSave;
            _driver = _root.AddComponent<Driver>();
            _driver.Owner = this;
            UnityEngine.Object.DontDestroyOnLoad(_root);

            Bind(weaponEvents ?? RuntimeKernelBootstrapper.WeaponEvents);
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (ReferenceEquals(s_activeRuntime, this))
            {
                s_activeRuntime = null;
            }

            _pendingShotsByItemId.Clear();
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            Bind(null);
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        public void SetTraceTtlSeconds(float ttlSeconds)
        {
            _state.TraceTtlSeconds = ttlSeconds;
            if (!AreTracesEnabled())
            {
                _pendingObserverShotClaims = 0;
                _pendingShotsByItemId.Clear();
                ClearVisibleSegments();
            }
        }

        public WeaponProjectile.IPathObserver CreateProjectilePathObserver()
        {
            if (!AreTracesEnabled())
            {
                return null;
            }

            _pendingObserverShotClaims++;
            return new ProjectilePathObserver(this);
        }

        public void ClearVisibleTraces()
        {
            ClearVisibleSegments();
        }

        public static WeaponProjectile.IPathObserver TryCreateActiveProjectilePathObserver()
        {
            return s_activeRuntime?.CreateProjectilePathObserver();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (_disposed)
            {
                return;
            }

            Bind(RuntimeKernelBootstrapper.WeaponEvents);
        }

        private void Bind(IWeaponEvents weaponEvents)
        {
            if (ReferenceEquals(_weaponEvents, weaponEvents))
            {
                return;
            }

            if (_weaponEvents != null)
            {
                _weaponEvents.OnWeaponFired -= HandleWeaponFired;
                _weaponEvents.OnProjectileHit -= HandleProjectileHit;
            }

            _weaponEvents = weaponEvents;
            if (_weaponEvents != null)
            {
                _weaponEvents.OnWeaponFired += HandleWeaponFired;
                _weaponEvents.OnProjectileHit += HandleProjectileHit;
            }
        }

        private void HandleWeaponFired(string itemId, Vector3 origin, Vector3 direction)
        {
            if (!AreTracesEnabled())
            {
                return;
            }

            if (_pendingObserverShotClaims > 0)
            {
                _pendingObserverShotClaims--;
                return;
            }

            var normalizedItemId = NormalizeItemId(itemId);
            if (!_pendingShotsByItemId.TryGetValue(normalizedItemId, out var pendingShots))
            {
                pendingShots = new List<PendingShot>();
                _pendingShotsByItemId[normalizedItemId] = pendingShots;
            }

            pendingShots.Add(new PendingShot(
                origin,
                direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward,
                Time.unscaledTime + _pendingFallbackDelaySeconds,
                Time.unscaledTime + _pendingCleanupLifetimeSeconds));
        }

        private void HandleProjectileHit(string itemId, Vector3 point, float _)
        {
            if (!AreTracesEnabled())
            {
                return;
            }

            var normalizedItemId = NormalizeItemId(itemId);
            if (!_pendingShotsByItemId.TryGetValue(normalizedItemId, out var pendingShots)
                || pendingShots.Count == 0)
            {
                return;
            }

            var pendingShot = pendingShots[0];
            pendingShots.RemoveAt(0);
            if (pendingShots.Count == 0)
            {
                _pendingShotsByItemId.Remove(normalizedItemId);
            }

            ShowPendingShotSegment(pendingShot, point);
        }

        private void Tick(float now)
        {
            if (!AreTracesEnabled())
            {
                return;
            }

            if (_pendingShotsByItemId.Count == 0)
            {
                return;
            }

            var expiredKeys = new List<string>();
            foreach (var entry in _pendingShotsByItemId)
            {
                var pendingShots = entry.Value;
                for (var i = pendingShots.Count - 1; i >= 0; i--)
                {
                    var pendingShot = pendingShots[i];
                    if (pendingShot.SegmentView == null && now >= pendingShot.FallbackAt)
                    {
                        pendingShot.SegmentView = ShowPendingShotSegment(
                            pendingShot,
                            pendingShot.Origin + pendingShot.Direction * _fallbackDistanceMeters);
                    }

                    if (now < pendingShot.CleanupAt)
                    {
                        continue;
                    }

                    pendingShots.RemoveAt(i);
                }

                if (pendingShots.Count == 0)
                {
                    expiredKeys.Add(entry.Key);
                }
            }

            for (var i = 0; i < expiredKeys.Count; i++)
            {
                _pendingShotsByItemId.Remove(expiredKeys[i]);
            }
        }

        private void ShowSegment(Vector3 startPoint, Vector3 endPoint)
        {
            GetOrCreateSegment().Show(startPoint, endPoint, _traceColor, ResolveVisibleLifetimeSeconds());
        }

        private void RecordProjectileSegment(ProjectileTracePath tracePath, Vector3 startPoint, Vector3 endPoint)
        {
            if (!AreTracesEnabled() || tracePath == null || tracePath.Completed)
            {
                return;
            }

            if (tracePath.Points.Count == 0)
            {
                tracePath.Points.Add(startPoint);
            }
            else if (Vector3.Distance(tracePath.Points[^1], startPoint) > 0.001f)
            {
                tracePath.Points.Add(startPoint);
            }

            tracePath.Points.Add(endPoint);
            tracePath.SegmentView ??= GetOrCreateSegment();
            tracePath.SegmentView.ShowPath(tracePath.Points, _traceColor, -1f);
        }

        private void CompleteProjectileTrace(ProjectileTracePath tracePath, Vector3 terminalPoint)
        {
            if (!AreTracesEnabled() || tracePath == null || tracePath.Completed)
            {
                return;
            }

            tracePath.Completed = true;
            if (tracePath.Points.Count == 0)
            {
                return;
            }

            if (Vector3.Distance(tracePath.Points[^1], terminalPoint) > 0.001f)
            {
                tracePath.Points.Add(terminalPoint);
            }

            tracePath.SegmentView ??= GetOrCreateSegment();
            tracePath.SegmentView.ShowPath(tracePath.Points, _traceColor, ResolveVisibleLifetimeSeconds());
        }

        private DevTraceSegmentView ShowPendingShotSegment(PendingShot pendingShot, Vector3 endPoint)
        {
            var segment = pendingShot.SegmentView ?? GetOrCreateSegment();
            segment.Show(pendingShot.Origin, endPoint, _traceColor, ResolveVisibleLifetimeSeconds());
            return segment;
        }

        private void ClearVisibleSegments()
        {
            for (var i = 0; i < _segmentPool.Count; i++)
            {
                _segmentPool[i].Hide();
            }
        }

        private DevTraceSegmentView GetOrCreateSegment()
        {
            for (var i = 0; i < _segmentPool.Count; i++)
            {
                if (!_segmentPool[i].IsVisible)
                {
                    return _segmentPool[i];
                }
            }

            var segmentGo = new GameObject($"DevTraceSegment_{_segmentPool.Count}");
            segmentGo.transform.SetParent(_root.transform, false);
            segmentGo.AddComponent<LineRenderer>();
            var segment = segmentGo.AddComponent<DevTraceSegmentView>();
            _segmentPool.Add(segment);
            return segment;
        }

        private static string NormalizeItemId(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
        }

        private bool AreTracesEnabled()
        {
            return _state.TraceTtlSeconds > 0f || Mathf.Approximately(_state.TraceTtlSeconds, -1f);
        }

        private float ResolveVisibleLifetimeSeconds()
        {
            if (_state.TraceTtlSeconds < 0f)
            {
                return -1f;
            }

            return Mathf.Max(0.01f, _state.TraceTtlSeconds);
        }
    }
}
