using Reloader.Inventory;
using Reloader.PlayerDevice.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.PlayerDevice.World
{
    public sealed class PlayerDeviceController
    {
        public readonly struct SavedGroupSummary
        {
            public SavedGroupSummary(int shotCount, bool isMoaAvailable, double moa, double spreadMeters)
            {
                ShotCount = shotCount;
                IsMoaAvailable = isMoaAvailable;
                Moa = moa;
                SpreadMeters = spreadMeters;
            }

            public int ShotCount { get; }
            public bool IsMoaAvailable { get; }
            public double Moa { get; }
            public double SpreadMeters { get; }
        }

        public static PlayerDeviceController ActiveInstance { get; private set; }

        private readonly PlayerDeviceRuntimeState _runtimeState;
        private readonly PlayerInventoryController _inventoryController;
        private readonly DeviceAttachmentCatalog _attachmentCatalog;
        private DeviceGroupMetrics _activeGroupMetrics;

        public PlayerDeviceController(
            PlayerDeviceRuntimeState runtimeState,
            PlayerInventoryController inventoryController,
            DeviceAttachmentCatalog attachmentCatalog)
        {
            _runtimeState = runtimeState;
            _inventoryController = inventoryController;
            _attachmentCatalog = attachmentCatalog ?? DeviceAttachmentCatalog.Empty;
            _activeGroupMetrics = DeviceGroupMetrics.Unavailable;
            ActiveInstance = this;
            RecomputeActiveGroupMetrics();
        }

        public DeviceGroupMetrics ActiveGroupMetrics => _activeGroupMetrics;
        public bool HasSelectedTargetBinding => _runtimeState?.HasSelectedTargetBinding == true;
        public DeviceTargetBinding SelectedTargetBinding => _runtimeState?.SelectedTargetBinding ?? DeviceTargetBinding.None;
        public int ActiveShotCount => _runtimeState?.ActiveGroupSession?.ShotCount ?? 0;
        public int SavedGroupCount => _runtimeState?.SavedGroupSessions?.Count ?? 0;
        public bool IsRangefinderHooksInstalled => _runtimeState?.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder) == true;

        public bool OpenDeviceSection()
        {
            return true;
        }

        public bool BeginTargetSelection()
        {
            return true;
        }

        public void IngestImpact(Vector3 impactPoint, GameObject hitObject)
        {
            IngestImpact(impactPoint, hitObject, null);
        }

        public void IngestImpact(Vector3 impactPoint, GameObject hitObject, Vector3? sourcePoint)
        {
            if (_runtimeState == null || !_runtimeState.HasSelectedTargetBinding)
            {
                return;
            }

            var selectedTargetId = _runtimeState.SelectedTargetBinding.TargetId;
            if (!TryResolveRangeTargetMetrics(hitObject, selectedTargetId, out var targetMetrics))
            {
                return;
            }

            if (targetMetrics is not Component targetComponent)
            {
                return;
            }

            var localImpact = targetComponent.transform.InverseTransformPoint(impactPoint);
            var impactDistanceMeters = ResolveImpactDistanceMeters(impactPoint, sourcePoint, targetMetrics);
            _runtimeState.AddShotSampleToActiveGroup(new DeviceShotSample(
                new Vector2(localImpact.x, localImpact.y),
                impactDistanceMeters));

            RecomputeActiveGroupMetrics();
        }

        public void SaveCurrentGroup()
        {
            _runtimeState?.SaveCurrentGroup();
        }

        public void ClearCurrentGroup()
        {
            _runtimeState?.ClearActiveGroup();
            _activeGroupMetrics = DeviceGroupMetrics.Unavailable;
            ClearSelectedTargetMarkers();
        }

        public bool TryInstallSelectedAttachmentFromInventory()
        {
            if (_runtimeState == null || _inventoryController == null)
            {
                return false;
            }

            if (!_inventoryController.TryGetSelectedInventoryItemId(out var selectedItemId))
            {
                return false;
            }

            if (!_attachmentCatalog.TryGetAttachmentType(selectedItemId, out var attachmentType))
            {
                return false;
            }

            if (_runtimeState.IsAttachmentInstalled(attachmentType))
            {
                return false;
            }

            if (!_inventoryController.TryConsumeSelectedBeltItem(out var consumedItemId)
                || !string.Equals(consumedItemId, selectedItemId, System.StringComparison.Ordinal))
            {
                return false;
            }

            _runtimeState.InstallAttachment(attachmentType);
            return true;
        }

        public bool CanInstallSelectedAttachmentFromInventory()
        {
            if (_runtimeState == null || _inventoryController == null)
            {
                return false;
            }

            if (!_inventoryController.TryGetSelectedInventoryItemId(out var selectedItemId))
            {
                return false;
            }

            if (!_attachmentCatalog.TryGetAttachmentType(selectedItemId, out var attachmentType))
            {
                return false;
            }

            return attachmentType != DeviceAttachmentType.None && !_runtimeState.IsAttachmentInstalled(attachmentType);
        }

        public bool TryUninstallAttachment(DeviceAttachmentType attachmentType)
        {
            if (_runtimeState == null || _inventoryController == null)
            {
                return false;
            }

            if (attachmentType == DeviceAttachmentType.None || !_runtimeState.IsAttachmentInstalled(attachmentType))
            {
                return false;
            }

            if (!_attachmentCatalog.TryGetItemId(attachmentType, out var attachmentItemId)
                || string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return false;
            }

            if (!_inventoryController.TryStoreItemWithBeltPriority(attachmentItemId))
            {
                return false;
            }

            _runtimeState.UninstallAttachment(attachmentType);
            return true;
        }

        public bool CanUninstallAttachment(DeviceAttachmentType attachmentType)
        {
            if (_runtimeState == null || _inventoryController == null)
            {
                return false;
            }

            if (attachmentType == DeviceAttachmentType.None || !_runtimeState.IsAttachmentInstalled(attachmentType))
            {
                return false;
            }

            if (!_attachmentCatalog.TryGetItemId(attachmentType, out var attachmentItemId)
                || string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return false;
            }

            return true;
        }

        public IReadOnlyList<SavedGroupSummary> BuildSavedGroupSummaries()
        {
            if (_runtimeState?.SavedGroupSessions == null || _runtimeState.SavedGroupSessions.Count == 0)
            {
                return Array.Empty<SavedGroupSummary>();
            }

            var groups = _runtimeState.SavedGroupSessions;
            var summaries = new SavedGroupSummary[groups.Count];
            for (var i = 0; i < groups.Count; i++)
            {
                var metrics = CalculateGroupMetrics(groups[i]);
                summaries[i] = new SavedGroupSummary(
                    shotCount: metrics.ShotCount,
                    isMoaAvailable: metrics.IsMoaAvailable,
                    moa: metrics.Moa,
                    spreadMeters: metrics.LinearSpreadMeters);
            }

            return summaries;
        }

        private void RecomputeActiveGroupMetrics()
        {
            _activeGroupMetrics = CalculateGroupMetrics(_runtimeState?.ActiveGroupSession);
        }

        private static DeviceGroupMetrics CalculateGroupMetrics(DeviceGroupSession session)
        {
            if (session?.ShotSamples == null)
            {
                return DeviceGroupMetrics.Unavailable;
            }

            var shots = session.ShotSamples;
            var calculatorShots = new List<DeviceGroupMetricsCalculator.ShotSample>(shots.Count);
            for (var i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                calculatorShots.Add(new DeviceGroupMetricsCalculator.ShotSample(
                    shot.TargetPlanePointMeters,
                    shot.DistanceMeters));
            }

            return DeviceGroupMetricsCalculator.Calculate(calculatorShots);
        }

        private void ClearSelectedTargetMarkers()
        {
            if (_runtimeState == null || !_runtimeState.HasSelectedTargetBinding)
            {
                return;
            }

            if (TryResolveMarkerClearable(_runtimeState.SelectedTargetBinding.TargetId, out var clearable))
            {
                clearable.ClearTargetMarkers();
            }
        }

        private static bool TryResolveRangeTargetMetrics(GameObject hitObject, string preferredTargetId, out IRangeTargetMetrics targetMetrics)
        {
            targetMetrics = null;
            if (hitObject == null)
            {
                return false;
            }

            if (TryResolveMatchingMetrics(hitObject.GetComponents<IRangeTargetMetrics>(), preferredTargetId, out targetMetrics))
            {
                return true;
            }

            var parentMetrics = hitObject.GetComponentsInParent<IRangeTargetMetrics>(includeInactive: false);
            return TryResolveMatchingMetrics(parentMetrics, preferredTargetId, out targetMetrics);
        }

        private static float ResolveImpactDistanceMeters(Vector3 impactPoint, Vector3? sourcePoint, IRangeTargetMetrics targetMetrics)
        {
            if (sourcePoint.HasValue)
            {
                var dynamicDistance = Vector3.Distance(sourcePoint.Value, impactPoint);
                if (dynamicDistance > 0f && !float.IsNaN(dynamicDistance) && !float.IsInfinity(dynamicDistance))
                {
                    return dynamicDistance;
                }
            }

            return targetMetrics != null ? Mathf.Max(0f, targetMetrics.DistanceMeters) : 0f;
        }

        private static bool TryResolveMarkerClearable(string targetId, out IDeviceTargetMarkerClearable clearable)
        {
            clearable = null;
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IDeviceTargetMarkerClearable candidate || behaviours[i] is not Component component)
                {
                    continue;
                }

                var metricsComponents = component.GetComponents<IRangeTargetMetrics>();
                if (!TryResolveMatchingMetrics(metricsComponents, targetId, out _))
                {
                    continue;
                }

                clearable = candidate;
                return true;
            }

            return false;
        }

        private static bool TryResolveMatchingMetrics(
            IReadOnlyList<IRangeTargetMetrics> metricsCandidates,
            string preferredTargetId,
            out IRangeTargetMetrics resolved)
        {
            resolved = null;
            if (metricsCandidates == null || metricsCandidates.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < metricsCandidates.Count; i++)
            {
                var candidate = metricsCandidates[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(preferredTargetId)
                    && string.Equals(candidate.TargetId, preferredTargetId, StringComparison.Ordinal))
                {
                    resolved = candidate;
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(preferredTargetId))
            {
                return false;
            }

            for (var i = 0; i < metricsCandidates.Count; i++)
            {
                if (metricsCandidates[i] == null)
                {
                    continue;
                }

                resolved = metricsCandidates[i];
                return true;
            }

            return false;
        }
    }
}
