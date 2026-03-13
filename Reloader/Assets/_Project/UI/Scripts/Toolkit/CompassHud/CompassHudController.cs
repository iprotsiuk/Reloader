using System;
using System.Collections.Generic;
using Reloader.Core;
using Reloader.Contracts.Runtime;
using Reloader.Inventory;
using Reloader.NPCs.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.CompassHud
{
    public sealed class CompassHudController : MonoBehaviour, IUiController, IDisposable
    {
        private const float VisibleHalfAngleDegrees = 100f;
        private const float HorizontalDirectionEpsilonSqr = 0.0001f;

        private CompassHudViewBinder _viewBinder;
        private PlayerInventoryController _inventoryController;
        private IContractRuntimeProvider _contractRuntimeProvider;
        private CivilianPopulationRuntimeBridge _populationBridge;
        private string _resolvedTargetId = string.Empty;
        private Transform _resolvedTargetTransform;

        private void LateUpdate()
        {
            Refresh();
        }

        public void SetViewBinder(CompassHudViewBinder viewBinder)
        {
            _viewBinder = viewBinder;
            Refresh();
        }

        public void SetInventoryController(PlayerInventoryController inventoryController)
        {
            _inventoryController = inventoryController;
            Refresh();
        }

        public void SetContractRuntimeProvider(IContractRuntimeProvider contractRuntimeProvider)
        {
            _contractRuntimeProvider = contractRuntimeProvider;
            Refresh();
        }

        public void SetPopulationBridge(CivilianPopulationRuntimeBridge populationBridge)
        {
            _populationBridge = populationBridge;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        public void Dispose()
        {
            _viewBinder = null;
            _inventoryController = null;
            _contractRuntimeProvider = null;
            _populationBridge = null;
            ClearResolvedTarget();
        }

        public void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            ResolveReferences();
            var viewer = _inventoryController != null ? _inventoryController.transform : null;
            if (viewer == null)
            {
                _viewBinder.Render(CompassHudUiState.Create(
                    entries: new CompassHudUiState.EntryState[0],
                    visibleHalfAngleDegrees: VisibleHalfAngleDegrees,
                    isVisible: false));
                return;
            }

            var headingDegrees = CompassHeadingMath.ResolveHeadingDegrees(viewer.forward);
            var entries = new List<CompassHudUiState.EntryState>(CompassHeadingMath.CreateCardinalEntries(headingDegrees, VisibleHalfAngleDegrees));
            if (TryBuildTargetMarker(viewer, headingDegrees, out var markerEntry))
            {
                entries.Add(markerEntry);
            }

            _viewBinder.Render(CompassHudUiState.Create(entries, VisibleHalfAngleDegrees, isVisible: true));
        }

        private void ResolveReferences()
        {
            _inventoryController ??= FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);

            if (!IsReferenceAlive(_contractRuntimeProvider))
            {
                _contractRuntimeProvider = null;
            }

            if (_contractRuntimeProvider == null)
            {
                _contractRuntimeProvider = DependencyResolutionGuard.FindInterface<IContractRuntimeProvider>(
                    FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }

            if (!IsReferenceAlive(_populationBridge))
            {
                _populationBridge = null;
            }

            if (_populationBridge == null)
            {
                _populationBridge = FindFirstObjectByType<CivilianPopulationRuntimeBridge>(FindObjectsInactive.Include);
            }
        }

        private bool TryBuildTargetMarker(Transform viewer, float headingDegrees, out CompassHudUiState.EntryState markerEntry)
        {
            markerEntry = default;
            if (_contractRuntimeProvider == null
                || !_contractRuntimeProvider.TryGetContractSnapshot(out var snapshot)
                || !snapshot.HasActiveContract
                || string.IsNullOrWhiteSpace(snapshot.TargetId))
            {
                ClearResolvedTarget();
                return false;
            }

            if (!TryResolveTargetTransform(snapshot.TargetId, out var targetTransform))
            {
                return false;
            }

            var horizontalOffset = targetTransform.position - viewer.position;
            horizontalOffset.y = 0f;
            if (horizontalOffset.sqrMagnitude <= HorizontalDirectionEpsilonSqr)
            {
                return false;
            }

            var bearingDegrees = CompassHeadingMath.ResolveHeadingDegrees(horizontalOffset);
            var signedDelta = CompassHeadingMath.ResolveSignedDeltaDegrees(headingDegrees, bearingDegrees);
            if (Mathf.Abs(signedDelta) > VisibleHalfAngleDegrees)
            {
                return false;
            }

            markerEntry = new CompassHudUiState.EntryState(
                "contract-target",
                CompassHudUiState.EntryKind.Marker,
                string.Empty,
                signedDelta);
            return true;
        }

        private bool TryResolveTargetTransform(string targetId, out Transform targetTransform)
        {
            targetTransform = null;
            if (!string.Equals(_resolvedTargetId, targetId, StringComparison.Ordinal))
            {
                _resolvedTargetId = targetId;
                _resolvedTargetTransform = null;
            }

            if (_resolvedTargetTransform != null)
            {
                targetTransform = _resolvedTargetTransform;
                return true;
            }

            if (_populationBridge == null
                || !_populationBridge.TryResolveSpawnedCivilian(targetId, out var spawnedCivilian)
                || spawnedCivilian == null)
            {
                return false;
            }

            _resolvedTargetTransform = spawnedCivilian.transform;
            targetTransform = _resolvedTargetTransform;
            return targetTransform != null;
        }

        private void ClearResolvedTarget()
        {
            _resolvedTargetId = string.Empty;
            _resolvedTargetTransform = null;
        }

        private static bool IsReferenceAlive(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (instance is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }
    }
}
