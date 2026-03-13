using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Inventory;
using Reloader.NPCs.Runtime;
using Reloader.UI.Toolkit.CompassHud;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class CompassHudControllerEditModeTests
    {
        [Test]
        public void Refresh_WhenTargetHasNoHorizontalOffset_HidesMarker()
        {
            var fixture = new CompassHudFixture();

            try
            {
                fixture.InventoryController.transform.position = new Vector3(12f, 2f, -7f);
                fixture.InventoryController.transform.forward = Vector3.forward;
                fixture.SetTargetPosition(new Vector3(12f, 40f, -7f));

                fixture.Controller.Refresh();

                Assert.That(FindEntry(fixture.EntriesRoot, "contract-target"), Is.Null);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void Refresh_WhenTargetWasResolved_KeepsUsingCachedTransformWithoutPopulationBridge()
        {
            var fixture = new CompassHudFixture();

            try
            {
                fixture.InventoryController.transform.position = Vector3.zero;
                fixture.InventoryController.transform.forward = Vector3.forward;
                fixture.SetTargetPosition(new Vector3(30f, 5f, 0f));

                fixture.Controller.Refresh();
                var firstMarker = FindEntry(fixture.EntriesRoot, "contract-target");
                Assert.That(firstMarker, Is.Not.Null);

                fixture.Controller.SetPopulationBridge(null);
                fixture.Controller.Refresh();

                var cachedMarker = FindEntry(fixture.EntriesRoot, "contract-target");
                Assert.That(cachedMarker, Is.Not.Null);
                Assert.That(cachedMarker.style.left.value.value, Is.GreaterThan(180f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static VisualElement FindEntry(VisualElement root, string entryKey)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root[i];
                if (string.Equals(child.viewDataKey, entryKey, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static VisualElement BuildRoot()
        {
            var screen = new VisualElement { name = "compass-hud__screen" };
            var root = new VisualElement { name = "compass-hud__root" };
            var frame = new VisualElement { name = "compass-hud__frame" };
            var lane = new VisualElement { name = "compass-hud__lane" };
            var entries = new VisualElement { name = "compass-hud__entries" };
            lane.Add(entries);
            frame.Add(lane);
            root.Add(frame);
            screen.Add(root);
            return screen;
        }

        private sealed class CompassHudFixture : IDisposable
        {
            private readonly GameObject _controllerGo;
            private readonly GameObject _populationGo;
            private readonly GameObject _targetGo;
            private readonly CompassHudViewBinder _viewBinder;
            private bool _disposed;

            public CompassHudFixture()
            {
                Root = BuildRoot();
                EntriesRoot = Root.Q<VisualElement>("compass-hud__entries");
                _viewBinder = new CompassHudViewBinder();
                _viewBinder.Initialize(Root);

                _controllerGo = new GameObject("CompassHudControllerFixture");
                Controller = _controllerGo.AddComponent<CompassHudController>();
                InventoryController = _controllerGo.AddComponent<PlayerInventoryController>();
                InventoryController.Configure(null, null, new PlayerInventoryRuntime());

                Provider = new TestContractRuntimeProvider(new ContractOfferSnapshot(
                    hasAvailableContract: false,
                    hasActiveContract: true,
                    hasFailedContract: false,
                    contractId: "contract.compass",
                    title: "Compass Contract",
                    targetId: "target.compass",
                    targetDisplayName: "Target Compass",
                    targetDescription: string.Empty,
                    briefingText: string.Empty,
                    distanceBandMeters: 0f,
                    payout: 0,
                    canAccept: false,
                    canCancel: true,
                    canClaimReward: false,
                    restrictionsText: string.Empty,
                    failureConditionsText: string.Empty,
                    canClearFailed: false,
                    statusText: "Active contract"));

                _populationGo = new GameObject("CompassPopulationFixture");
                PopulationBridge = _populationGo.AddComponent<CivilianPopulationRuntimeBridge>();
                _targetGo = new GameObject("CompassTargetFixture");
                _targetGo.transform.SetParent(_populationGo.transform, false);
                SpawnedCivilian = _targetGo.AddComponent<MainTownPopulationSpawnedCivilian>();
                SetPrivateField(SpawnedCivilian, "_civilianId", "target.compass");

                Controller.SetInventoryController(InventoryController);
                Controller.SetContractRuntimeProvider(Provider);
                Controller.SetPopulationBridge(PopulationBridge);
                Controller.SetViewBinder(_viewBinder);
            }

            public CompassHudController Controller { get; }
            public PlayerInventoryController InventoryController { get; }
            public TestContractRuntimeProvider Provider { get; }
            public CivilianPopulationRuntimeBridge PopulationBridge { get; }
            public MainTownPopulationSpawnedCivilian SpawnedCivilian { get; }
            public VisualElement Root { get; }
            public VisualElement EntriesRoot { get; }

            public void SetTargetPosition(Vector3 position)
            {
                _targetGo.transform.position = position;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _viewBinder.Dispose();
                UnityEngine.Object.DestroyImmediate(_populationGo);
                UnityEngine.Object.DestroyImmediate(_controllerGo);
            }

            private static void SetPrivateField(object target, string fieldName, object value)
            {
                var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
                field!.SetValue(target, value);
            }
        }

        private sealed class TestContractRuntimeProvider : IContractRuntimeProvider
        {
            private readonly ContractOfferSnapshot _snapshot;

            public TestContractRuntimeProvider(ContractOfferSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public bool TryGetContractSnapshot(out ContractOfferSnapshot snapshot)
            {
                snapshot = _snapshot;
                return true;
            }

            public bool AcceptAvailableContract()
            {
                return false;
            }

            public bool CancelActiveContract()
            {
                return false;
            }

            public bool ClearFailedContract()
            {
                return false;
            }

            public bool ClaimCompletedContractReward()
            {
                return false;
            }
        }
    }
}
