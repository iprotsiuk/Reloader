using System;
using System.Collections.Generic;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevTraceRuntime : IDisposable
    {
        private sealed class Driver : MonoBehaviour
        {
            public DevTraceRuntime Owner { get; set; }

            private void Update()
            {
                Owner?.Tick(Time.unscaledTime);
            }
        }

        private readonly struct PendingShot
        {
            public PendingShot(Vector3 origin, Vector3 direction, float expiresAt)
            {
                Origin = origin;
                Direction = direction;
                ExpiresAt = expiresAt;
            }

            public Vector3 Origin { get; }
            public Vector3 Direction { get; }
            public float ExpiresAt { get; }
        }

        private readonly DevToolsState _state;
        private readonly Dictionary<string, PendingShot> _pendingShotsByItemId = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<DevTraceSegmentView> _segmentPool = new();
        private readonly Color _traceColor = new(1f, 0.7f, 0.1f, 1f);
        private readonly float _traceLifetimeSeconds = 5f;
        private readonly float _fallbackDistanceMeters = 120f;
        private readonly float _pendingLifetimeSeconds = 0.05f;
        private readonly GameObject _root;
        private readonly Driver _driver;
        private IWeaponEvents _weaponEvents;
        private bool _disposed;

        public DevTraceRuntime(DevToolsState state, IWeaponEvents weaponEvents = null)
        {
            _state = state ?? new DevToolsState();
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
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            Bind(null);
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        public void SetPersistentTracesEnabled(bool isEnabled)
        {
            _state.PersistentTracesEnabled = isEnabled;
            if (!isEnabled)
            {
                _pendingShotsByItemId.Clear();
                ClearVisibleSegments();
            }
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
            if (!_state.PersistentTracesEnabled)
            {
                return;
            }

            var normalizedItemId = NormalizeItemId(itemId);
            if (_pendingShotsByItemId.TryGetValue(normalizedItemId, out var previousShot))
            {
                ShowSegment(previousShot.Origin, previousShot.Origin + previousShot.Direction * _fallbackDistanceMeters);
            }

            _pendingShotsByItemId[normalizedItemId] = new PendingShot(
                origin,
                direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward,
                Time.unscaledTime + _pendingLifetimeSeconds);
        }

        private void HandleProjectileHit(string itemId, Vector3 point, float _)
        {
            if (!_state.PersistentTracesEnabled)
            {
                return;
            }

            var normalizedItemId = NormalizeItemId(itemId);
            if (!_pendingShotsByItemId.TryGetValue(normalizedItemId, out var pendingShot))
            {
                return;
            }

            _pendingShotsByItemId.Remove(normalizedItemId);
            ShowSegment(pendingShot.Origin, point);
        }

        private void Tick(float now)
        {
            if (!_state.PersistentTracesEnabled || _pendingShotsByItemId.Count == 0)
            {
                return;
            }

            var expiredKeys = new List<string>();
            foreach (var entry in _pendingShotsByItemId)
            {
                if (now < entry.Value.ExpiresAt)
                {
                    continue;
                }

                ShowSegment(entry.Value.Origin, entry.Value.Origin + entry.Value.Direction * _fallbackDistanceMeters);
                expiredKeys.Add(entry.Key);
            }

            for (var i = 0; i < expiredKeys.Count; i++)
            {
                _pendingShotsByItemId.Remove(expiredKeys[i]);
            }
        }

        private void ShowSegment(Vector3 startPoint, Vector3 endPoint)
        {
            GetOrCreateSegment().Show(startPoint, endPoint, _traceColor, _traceLifetimeSeconds);
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
    }
}
