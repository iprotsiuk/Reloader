using System.Collections.Generic;
using Reloader.Contracts.Runtime;
using Reloader.Inventory;
using Reloader.NPCs.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.CompassHud
{
    public sealed class CompassHudController : MonoBehaviour, IUiController
    {
        private const float VisibleHalfAngleDegrees = 100f;

        private CompassHudViewBinder _viewBinder;
        private PlayerInventoryController _inventoryController;
        private IContractRuntimeProvider _contractRuntimeProvider;
        private CivilianPopulationRuntimeBridge _populationBridge;

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

            if (_contractRuntimeProvider == null)
            {
                _contractRuntimeProvider = FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            }

            _populationBridge ??= FindFirstObjectByType<CivilianPopulationRuntimeBridge>(FindObjectsInactive.Include);
        }

        private bool TryBuildTargetMarker(Transform viewer, float headingDegrees, out CompassHudUiState.EntryState markerEntry)
        {
            markerEntry = default;
            if (_contractRuntimeProvider == null
                || !_contractRuntimeProvider.TryGetContractSnapshot(out var snapshot)
                || !snapshot.HasActiveContract
                || _populationBridge == null
                || !_populationBridge.TryResolveSpawnedCivilian(snapshot.TargetId, out var spawnedCivilian)
                || spawnedCivilian == null)
            {
                return false;
            }

            var bearingDegrees = CompassHeadingMath.ResolveBearingDegrees(viewer.position, spawnedCivilian.transform.position);
            var signedDelta = CompassHeadingMath.ResolveSignedDeltaDegrees(headingDegrees, bearingDegrees);
            markerEntry = new CompassHudUiState.EntryState(
                "contract-target",
                CompassHudUiState.EntryKind.Marker,
                string.Empty,
                signedDelta);
            return true;
        }
    }
}
