using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Player;
using Reloader.PlayerDevice.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.PlayerDevice.World
{
    public sealed class PlayerDeviceTargetSelectionController : MonoBehaviour
    {
        private const string PendingHintContextId = "player-device.target-selection.pending";
        private const string PendingHintActionText = "Mark target";
        private const string ConfirmedHintContextId = "player-device.target-selection.confirmed";
        private const string ConfirmedHintActionText = "Target marked";

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private Camera _selectionCamera;
        [SerializeField] private float _maxSelectionDistanceMeters = 1200f;
        [SerializeField] private float _confirmedHintVisibleSeconds = 2f;

        private IPlayerInputSource _inputSource;
        private PlayerDeviceRuntimeState _runtimeState;
        private bool _isSelectingTarget;
        private float _clearConfirmedHintAt = -1f;

        private void Awake()
        {
            ResolveReferences();
            _runtimeState ??= new PlayerDeviceRuntimeState();
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(
            IPlayerInputSource inputSource,
            Camera selectionCamera = null,
            PlayerDeviceRuntimeState runtimeState = null)
        {
            _inputSource = inputSource;
            _selectionCamera = selectionCamera;
            _runtimeState = runtimeState ?? _runtimeState ?? new PlayerDeviceRuntimeState();
        }

        public void BeginTargetSelection()
        {
            _isSelectingTarget = true;
            _clearConfirmedHintAt = -1f;
            ResolveUiStateEvents()?.RaiseTabInventoryVisibilityChanged(false);
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintShown(
                new InteractionHintPayload(PendingHintContextId, PendingHintActionText, "Awaiting marked target"));
        }

        public void Tick()
        {
            if (!_isSelectingTarget)
            {
                TickConfirmedHintTimeout();
                return;
            }

            ResolveReferences();
            if (!ConsumeSelectionClickThisFrame())
            {
                return;
            }

            if (!TryResolveTargetMetrics(out var metrics, out var selectionDistanceMeters))
            {
                _isSelectingTarget = false;
                RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(PendingHintContextId);
                return;
            }

            _runtimeState ??= new PlayerDeviceRuntimeState();
            var resolvedSelectionDistance = selectionDistanceMeters > 0f ? selectionDistanceMeters : metrics.DistanceMeters;
            _runtimeState.SetSelectedTargetBinding(new DeviceTargetBinding(metrics.TargetId, metrics.DisplayName, resolvedSelectionDistance));
            _isSelectingTarget = false;

            var path = TryBuildHierarchyPath(metrics);
            var subjectText = string.IsNullOrWhiteSpace(path)
                ? $"{metrics.DisplayName} ({resolvedSelectionDistance:0.0} m)"
                : $"{metrics.DisplayName} ({resolvedSelectionDistance:0.0} m) [{path}]";
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintShown(
                new InteractionHintPayload(ConfirmedHintContextId, ConfirmedHintActionText, subjectText));
            _clearConfirmedHintAt = Time.unscaledTime + Mathf.Max(0f, _confirmedHintVisibleSeconds);
        }

        private void TickConfirmedHintTimeout()
        {
            if (_clearConfirmedHintAt < 0f)
            {
                return;
            }

            if (Time.unscaledTime < _clearConfirmedHintAt)
            {
                return;
            }

            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(ConfirmedHintContextId);
            _clearConfirmedHintAt = -1f;
        }

        private bool TryResolveTargetMetrics(out IRangeTargetMetrics metrics, out float selectionDistanceMeters)
        {
            metrics = null;
            selectionDistanceMeters = 0f;
            Physics.SyncTransforms();

            var camera = _selectionCamera != null ? _selectionCamera : Camera.main;
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out var hit, Mathf.Max(0.01f, _maxSelectionDistanceMeters)))
            {
                return false;
            }

            if (!PlayerDeviceTargetMetricsResolver.TryResolve(hit.collider, preferredTargetId: null, out metrics))
            {
                return false;
            }

            selectionDistanceMeters = Mathf.Max(0f, hit.distance);
            return metrics != null;
        }

        private bool ConsumeSelectionClickThisFrame()
        {
            if (_inputSource != null && _inputSource.ConsumePickupPressed())
            {
                return true;
            }

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
        }

        private static string TryBuildHierarchyPath(IRangeTargetMetrics metrics)
        {
            if (metrics is not Component component || component.transform == null)
            {
                return string.Empty;
            }

            var current = component.transform;
            var path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }

        private static IUiStateEvents ResolveUiStateEvents()
        {
            return RuntimeKernelBootstrapper.UiStateEvents;
        }
    }

    internal static class PlayerDeviceTargetMetricsResolver
    {
        public static bool TryResolve(Component hitComponent, string preferredTargetId, out IRangeTargetMetrics metrics)
        {
            metrics = null;
            if (hitComponent == null)
            {
                return false;
            }

            var candidates = new List<IRangeTargetMetrics>();
            AppendCandidates(hitComponent.GetComponents<IRangeTargetMetrics>(), candidates);
            AppendCandidates(hitComponent.GetComponentsInParent<IRangeTargetMetrics>(), candidates);
            return TryResolve(candidates, preferredTargetId, out metrics);
        }

        public static bool TryResolve(
            IReadOnlyList<IRangeTargetMetrics> candidates,
            string preferredTargetId,
            out IRangeTargetMetrics metrics)
        {
            metrics = null;
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            IRangeTargetMetrics best = null;
            var bestScore = int.MinValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(preferredTargetId)
                    && !string.Equals(candidate.TargetId, preferredTargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                var score = Score(candidate);
                if (best == null || score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            metrics = best;
            return metrics != null;
        }

        private static void AppendCandidates(IReadOnlyList<IRangeTargetMetrics> source, List<IRangeTargetMetrics> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var candidate = source[i];
                if (candidate == null || target.Contains(candidate))
                {
                    continue;
                }

                target.Add(candidate);
            }
        }

        private static int Score(IRangeTargetMetrics candidate)
        {
            var score = 0;
            if (!string.IsNullOrWhiteSpace(candidate.TargetId))
            {
                score += 100;
            }

            if (candidate is not Component component)
            {
                return score;
            }

            var typeName = component.GetType().Name;
            if (typeName.IndexOf("RangeMetrics", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 50;
            }

            if (typeName.IndexOf("Damageable", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 10;
            }

            return score;
        }
    }
}
