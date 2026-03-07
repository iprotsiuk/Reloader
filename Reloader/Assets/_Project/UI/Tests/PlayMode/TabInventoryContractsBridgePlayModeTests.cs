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

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

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
