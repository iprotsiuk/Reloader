using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Runtime;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class TabInventoryContractsBridgePlayModeTests
    {
        [Test]
        public void RuntimeBridge_BindTabInventory_AcceptsAvailableContractThroughContractsTab()
        {
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            var providerType = Type.GetType("Reloader.Contracts.Runtime.StaticContractRuntimeProvider, Reloader.Core");
            var runtimeType = Type.GetType("Reloader.Contracts.Runtime.ContractEscapeResolutionRuntime, Reloader.Core");
            Assert.That(definitionType, Is.Not.Null);
            Assert.That(providerType, Is.Not.Null);
            Assert.That(runtimeType, Is.Not.Null);

            var go = new GameObject("UiToolkitBridgeContracts");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);
            var inputSource = go.AddComponent<TestInputSource>();

            var providerGo = new GameObject("StaticContractRuntimeProvider");
            var provider = providerGo.AddComponent(providerType);
            var definition = ScriptableObject.CreateInstance(definitionType);
            SetPrivateField(definitionType, definition, "_contractId", "contract.first");
            SetPrivateField(definitionType, definition, "_targetId", "target.first");
            SetPrivateField(definitionType, definition, "_title", "Cafe Exit");
            SetPrivateField(definitionType, definition, "_targetDisplayName", "Maksim Volkov");
            SetPrivateField(definitionType, definition, "_targetDescription", "Gray coat, smoker, exits the cafe at dusk.");
            SetPrivateField(definitionType, definition, "_briefingText", "Observe from the ridge and confirm the target before taking the shot.");
            SetPrivateField(definitionType, definition, "_distanceBand", 420f);
            SetPrivateField(definitionType, definition, "_payout", 1500);
            var setAvailableContractMethod = providerType.GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setAvailableContractMethod, Is.Not.Null);
            setAvailableContractMethod.Invoke(provider, new object[] { definition });

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(bridge, new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, inputSource }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();
                tabController.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
                tabController.HandleIntent(new UiIntent("tab.inventory.contracts.accept"));

                var contractRuntime = GetPrivateField(providerType, provider, "_runtime");
                Assert.That(contractRuntime, Is.Not.Null);

                var activeContract = runtimeType.GetProperty("ActiveContract", BindingFlags.Instance | BindingFlags.Public)?.GetValue(contractRuntime);
                var activeDefinition = runtimeType.GetProperty("ActiveDefinition", BindingFlags.Instance | BindingFlags.Public)?.GetValue(contractRuntime);
                Assert.That(activeContract, Is.Not.Null);
                Assert.That(activeDefinition, Is.SameAs(definition));

                var contractId = activeContract.GetType().GetProperty("ContractId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(activeContract) as string;
                Assert.That(contractId, Is.EqualTo("contract.first"));

                var contractsStatus = root.Q<Label>("inventory__contracts-status");
                Assert.That(contractsStatus.text, Is.EqualTo("Active contract"));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)definition);
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_WhenProviderAppearsAfterBind_RecoversContractsAdapter()
        {
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            var providerType = Type.GetType("Reloader.Contracts.Runtime.StaticContractRuntimeProvider, Reloader.Core");
            var runtimeType = Type.GetType("Reloader.Contracts.Runtime.ContractEscapeResolutionRuntime, Reloader.Core");
            Assert.That(definitionType, Is.Not.Null);
            Assert.That(providerType, Is.Not.Null);
            Assert.That(runtimeType, Is.Not.Null);

            var go = new GameObject("UiToolkitBridgeContractsLateProvider");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, inventoryRuntime);
            var inputSource = go.AddComponent<TestInputSource>();

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(bridge, new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, inputSource }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            GameObject providerGo = null;
            UnityEngine.Object definition = null;

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();
                tabController.HandleIntent(new UiIntent("tab.menu.select", "contracts"));

                var contractsStatus = root.Q<Label>("inventory__contracts-status");
                Assert.That(contractsStatus, Is.Not.Null);
                Assert.That(contractsStatus.text, Is.EqualTo("No contracts currently posted"));

                providerGo = new GameObject("StaticContractRuntimeProviderLate");
                var provider = providerGo.AddComponent(providerType);
                definition = ScriptableObject.CreateInstance(definitionType);
                SetPrivateField(definitionType, definition, "_contractId", "contract.late");
                SetPrivateField(definitionType, definition, "_targetId", "target.late");
                SetPrivateField(definitionType, definition, "_title", "Harbor Walk");
                SetPrivateField(definitionType, definition, "_targetDisplayName", "Ilona Sidorov");
                SetPrivateField(definitionType, definition, "_targetDescription", "Blue coat, circles the warehouse yard.");
                SetPrivateField(definitionType, definition, "_briefingText", "Wait for the yard opening and confirm the route.");
                SetPrivateField(definitionType, definition, "_distanceBand", 510f);
                SetPrivateField(definitionType, definition, "_payout", 1800);
                var setAvailableContractMethod = providerType.GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(setAvailableContractMethod, Is.Not.Null);
                setAvailableContractMethod.Invoke(provider, new object[] { definition });

                tabController.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
                Assert.That(contractsStatus.text, Is.EqualTo("Available contract"));

                tabController.HandleIntent(new UiIntent("tab.inventory.contracts.accept"));

                var contractRuntime = GetPrivateField(providerType, provider, "_runtime");
                Assert.That(contractRuntime, Is.Not.Null);

                var activeContract = runtimeType.GetProperty("ActiveContract", BindingFlags.Instance | BindingFlags.Public)?.GetValue(contractRuntime);
                Assert.That(activeContract, Is.Not.Null, "Late provider discovery should still let the Contracts tab accept the offer.");
                Assert.That(contractsStatus.text, Is.EqualTo("Active contract"));
            }
            finally
            {
                subscription.Dispose();
                if (definition != null)
                {
                    UnityEngine.Object.DestroyImmediate(definition);
                }

                if (providerGo != null)
                {
                    UnityEngine.Object.DestroyImmediate(providerGo);
                }

                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_WhenProceduralContractIsActive_ShowsCompactDeviceTrackingStatus()
        {
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            var providerType = Type.GetType("Reloader.Contracts.Runtime.StaticContractRuntimeProvider, Reloader.Core");
            var populationBridgeType = Type.GetType("Reloader.NPCs.Runtime.CivilianPopulationRuntimeBridge, Reloader.NPCs");
            var populationRecordType = Type.GetType("Reloader.Core.Save.Modules.CivilianPopulationRecord, Reloader.Core");
            var spawnedCivilianType = Type.GetType("Reloader.NPCs.Runtime.MainTownPopulationSpawnedCivilian, Reloader.NPCs");
            Assert.That(definitionType, Is.Not.Null);
            Assert.That(providerType, Is.Not.Null);
            Assert.That(populationBridgeType, Is.Not.Null);
            Assert.That(populationRecordType, Is.Not.Null);
            Assert.That(spawnedCivilianType, Is.Not.Null);

            var go = new GameObject("UiToolkitBridgeContractTracking");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, inventoryRuntime);
            var inputSource = go.AddComponent<TestInputSource>();

            var providerGo = new GameObject("StaticContractRuntimeProviderTracking");
            var provider = providerGo.AddComponent(providerType);
            var definition = ScriptableObject.CreateInstance(definitionType);
            SetPrivateField(definitionType, definition, "_contractId", "contract.track");
            SetPrivateField(definitionType, definition, "_targetId", "target.track");
            SetPrivateField(definitionType, definition, "_title", "Square Watch");
            SetPrivateField(definitionType, definition, "_targetDisplayName", "Tomas Varga");
            SetPrivateField(definitionType, definition, "_targetDescription", "Tan coat, pacing the fountain.");
            SetPrivateField(definitionType, definition, "_briefingText", "Keep visual contact and confirm the square before taking the shot.");
            SetPrivateField(definitionType, definition, "_distanceBand", 160f);
            SetPrivateField(definitionType, definition, "_payout", 1500);
            var setAvailableContractMethod = providerType.GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setAvailableContractMethod, Is.Not.Null);
            setAvailableContractMethod.Invoke(provider, new object[] { definition });

            var populationGo = new GameObject("CivilianPopulationRuntimeBridgeTracking");
            var populationBridge = populationGo.AddComponent(populationBridgeType);
            var runtimeProperty = populationBridgeType.GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null);
            var runtimeState = runtimeProperty.GetValue(populationBridge);
            var civiliansProperty = runtimeState.GetType().GetProperty("Civilians", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(civiliansProperty, Is.Not.Null);
            var civilians = civiliansProperty.GetValue(runtimeState) as System.Collections.IList;
            Assert.That(civilians, Is.Not.Null);

            var record = Activator.CreateInstance(populationRecordType);
            SetProperty(populationRecordType, record, "CivilianId", "target.track");
            SetProperty(populationRecordType, record, "PopulationSlotId", "townsfolk.001");
            SetProperty(populationRecordType, record, "PoolId", "townsfolk");
            SetProperty(populationRecordType, record, "SpawnAnchorId", "Anchor_Fountain");
            SetProperty(populationRecordType, record, "AreaTag", "maintown.square");
            SetProperty(populationRecordType, record, "IsAlive", true);
            civilians.Add(record);

            var spawnedGo = new GameObject("SpawnedProceduralCivilian");
            spawnedGo.transform.SetParent(populationGo.transform, false);
            spawnedGo.transform.position = new Vector3(12f, 0f, 0f);
            var spawnedCivilian = spawnedGo.AddComponent(spawnedCivilianType);
            var initializeMethod = spawnedCivilianType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(initializeMethod, Is.Not.Null);
            initializeMethod.Invoke(spawnedCivilian, new[] { record });

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, inputSource }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();
                tabController.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
                tabController.HandleIntent(new UiIntent("tab.inventory.contracts.accept"));
                tabController.HandleIntent(new UiIntent("tab.menu.select", "device"));

                var trackingText = root.Q<Label>("inventory__device-install-feedback-text");
                var selectedTargetText = root.Q<Label>("inventory__device-selected-target-value");
                Assert.That(trackingText, Is.Not.Null);
                Assert.That(selectedTargetText, Is.Not.Null);
                Assert.That(trackingText.text, Is.EqualTo("TRACK: MainTown Square • LOCKED 12m"));
                Assert.That(selectedTargetText.text, Is.EqualTo("No target marked"), "Contract tracking should not overwrite the manual selected-target slot.");
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(spawnedGo);
                UnityEngine.Object.DestroyImmediate(populationGo);
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)definition);
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_RendersWorldClockAndBalanceInHeader()
        {
            var economyControllerType = Type.GetType("Reloader.Economy.EconomyController, Reloader.Economy");
            var coreWorldControllerType = Type.GetType("Reloader.Core.Runtime.CoreWorldController, Reloader.Core");
            Assert.That(economyControllerType, Is.Not.Null);
            Assert.That(coreWorldControllerType, Is.Not.Null);

            var bridgeGo = new GameObject("UiToolkitBridgeHeader");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = bridgeGo.AddComponent<PlayerInventoryController>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, inventoryRuntime);

            var economyController = bridgeGo.AddComponent(economyControllerType);
            var tryAwardMoneyMethod = economyControllerType.GetMethod("TryAwardMoney", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryAwardMoneyMethod, Is.Not.Null);
            Assert.That((bool)tryAwardMoneyMethod!.Invoke(economyController, new object[] { 1950 }), Is.True);

            var coreWorldController = bridgeGo.AddComponent(coreWorldControllerType);
            var setWorldStateMethod = coreWorldControllerType.GetMethod("SetWorldState", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setWorldStateMethod, Is.Not.Null);
            setWorldStateMethod!.Invoke(coreWorldController, new object[] { 0, 18.6667f });

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod!.Invoke(
                bridge,
                new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, null }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var headerMeta = root.Q<Label>("inventory__header-meta");
                Assert.That(headerMeta, Is.Not.Null);
                Assert.That(headerMeta.text, Is.EqualTo("Monday • 18:40 • $2,450"));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(bridgeGo);
            }
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(Type type, object target, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            return field.GetValue(target);
        }

        private static void SetProperty(Type type, object target, string propertyName, object value)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on {type?.Name}.");
            property.SetValue(target, value);
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            panel.Add(new Label { name = "inventory__header-meta", text = "Monday • 08:00 • $500" });
            panel.Add(new VisualElement { name = "inventory__tabbar" });
            panel.Add(new Button { name = "inventory__tab-inventory", text = "Inventory" });
            panel.Add(new Button { name = "inventory__tab-quests", text = "Quests" });
            panel.Add(new Button { name = "inventory__tab-journal", text = "Journal" });
            panel.Add(new Button { name = "inventory__tab-calendar", text = "Calendar" });
            panel.Add(new Button { name = "inventory__tab-device", text = "Device" });

            panel.Add(new VisualElement { name = "inventory__section-inventory" });
            panel.Add(new VisualElement { name = "inventory__section-quests" });
            panel.Add(new VisualElement { name = "inventory__section-journal" });
            panel.Add(new VisualElement { name = "inventory__section-calendar" });
            panel.Add(new VisualElement { name = "inventory__section-device" });
            panel.Add(new VisualElement { name = "inventory__section-attachments" });
            panel.Add(new VisualElement { name = "inventory__device-notes" });
            panel.Add(new Label { name = "inventory__device-selected-target-value" });
            panel.Add(new Label { name = "inventory__device-shot-count-value" });
            panel.Add(new Label { name = "inventory__device-spread-value" });
            panel.Add(new Label { name = "inventory__device-moa-value" });
            panel.Add(new Label { name = "inventory__device-saved-groups-value" });
            panel.Add(new Label { name = "inventory__device-install-feedback-text" });
            panel.Add(new VisualElement { name = "inventory__device-session-history" });
            var detailPane = new VisualElement { name = "inventory__detail-pane" };
            detailPane.Add(new VisualElement { name = "inventory__detail-pane-generic" });
            detailPane.Add(new VisualElement { name = "inventory__detail-pane-contracts" });
            detailPane.Add(new Label { name = "inventory__detail-pane-base-payout", text = "Payout: --" });
            detailPane.Add(new Label { name = "inventory__detail-pane-bonus-conditions", text = "None" });
            detailPane.Add(new Label { name = "inventory__detail-pane-restrictions", text = "None" });
            detailPane.Add(new Label { name = "inventory__detail-pane-failure-conditions", text = "Wrong target" });
            detailPane.Add(new Label { name = "inventory__detail-pane-reward-state", text = "No contract selected" });
            panel.Add(detailPane);
            panel.Add(new Button { name = "inventory__device-choose-target" });
            panel.Add(new Button { name = "inventory__device-save-group" });
            panel.Add(new Button { name = "inventory__device-clear-group" });
            panel.Add(new Button { name = "inventory__device-install-hooks" });
            panel.Add(new Button { name = "inventory__device-uninstall-hooks" });
            panel.Add(new VisualElement { name = "inventory__backpack-grid" });
            panel.Add(new VisualElement { name = "inventory__grid-row--belt" });

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            panel.Add(tooltip);

            return root;
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool MenuTogglePressedThisFrame { get; set; }

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;

            public bool ConsumeMenuTogglePressed()
            {
                var result = MenuTogglePressedThisFrame;
                MenuTogglePressedThisFrame = false;
                return result;
            }
        }
    }
}
