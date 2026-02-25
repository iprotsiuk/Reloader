using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.TabInventory;
using Reloader.UI.Toolkit.Trade;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class UiToolkitScreenFlowPlayModeTests
    {
        [Test]
        public void TabInventoryController_HandleSwapIntent_MovesItemsBetweenAreas()
        {
            var go = new GameObject("TabInventoryController");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(null, null, runtime);
            runtime.SetBackpackCapacity(2);

            // Seed belt/backpack through runtime API semantics so swap flow reflects gameplay state.
            runtime.BeltSlotItemIds[0] = "item-belt";
            runtime.BeltSlotItemIds[1] = "slot-1";
            runtime.BeltSlotItemIds[2] = "slot-2";
            runtime.BeltSlotItemIds[3] = "slot-3";
            runtime.BeltSlotItemIds[4] = "slot-4";
            Assert.That(runtime.TryStoreItem("item-pack", out _, out var storedIndex, out _), Is.True);
            Assert.That(storedIndex, Is.EqualTo(0));

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var payload = new TabInventoryDragController.DragIntentPayload("belt", 0, "backpack", 0);
            controller.HandleIntent(new UiIntent("inventory.drag.swap", payload));

            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("item-pack"));
            Assert.That(runtime.BackpackItemIds[0], Is.EqualTo("item-belt"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_HandleSwapIntent_BackpackSparseTargetIndex_AppendsMove()
        {
            var go = new GameObject("TabInventoryControllerSparseTarget");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(null, null, runtime);
            runtime.SetBackpackCapacity(16);

            runtime.BeltSlotItemIds[0] = "item-belt";
            runtime.BeltSlotItemIds[1] = "slot-1";
            runtime.BeltSlotItemIds[2] = "slot-2";
            runtime.BeltSlotItemIds[3] = "slot-3";
            runtime.BeltSlotItemIds[4] = "slot-4";
            Assert.That(runtime.TryStoreItem("item-pack", out _, out var storedIndex, out _), Is.True);
            Assert.That(storedIndex, Is.EqualTo(0));
            Assert.That(runtime.BackpackItemIds.Count, Is.EqualTo(1));

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 16);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var payload = new TabInventoryDragController.DragIntentPayload("belt", 0, "backpack", 8);
            controller.HandleIntent(new UiIntent("inventory.drag.swap", payload));

            Assert.That(runtime.BeltSlotItemIds[0], Is.Null);
            Assert.That(runtime.BackpackItemIds.Count, Is.EqualTo(2));
            Assert.That(runtime.BackpackItemIds[0], Is.EqualTo("item-pack"));
            Assert.That(runtime.BackpackItemIds[1], Is.EqualTo("item-belt"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryViewBinder_Render_SwitchesActiveSection()
        {
            var root = BuildTabRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            binder.Render(TabInventoryUiState.Create(
                isOpen: true,
                beltSlots: new TabInventoryUiState.SlotState[5],
                backpackSlots: new TabInventoryUiState.SlotState[2],
                tooltipTitle: string.Empty,
                tooltipVisible: false,
                activeSection: "quests"));

            var inventoryPanel = root.Q<VisualElement>("inventory__section-inventory");
            var questsPanel = root.Q<VisualElement>("inventory__section-quests");
            Assert.That(inventoryPanel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(questsPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void TabInventoryViewBinder_Render_SwitchesToJournalSection()
        {
            var root = BuildTabRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            binder.Render(TabInventoryUiState.Create(
                isOpen: true,
                beltSlots: new TabInventoryUiState.SlotState[5],
                backpackSlots: new TabInventoryUiState.SlotState[2],
                tooltipTitle: string.Empty,
                tooltipVisible: false,
                activeSection: "journal"));

            var inventoryPanel = root.Q<VisualElement>("inventory__section-inventory");
            var journalPanel = root.Q<VisualElement>("inventory__section-journal");
            Assert.That(inventoryPanel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(journalPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void TradeController_ShopEvents_ToggleTradeVisibility()
        {
            var go = new GameObject("TradeController");
            var root = BuildTradeRoot();
            root.style.display = DisplayStyle.None;

            var binder = new TradeViewBinder();
            binder.Initialize(root);

            var controller = go.AddComponent<TradeController>();
            controller.SetViewBinder(binder);

            GameEvents.RaiseShopTradeOpened("vendor-1");
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            GameEvents.RaiseShopTradeClosed();
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_Tick_MenuToggleInput_TogglesPanelVisibility()
        {
            var go = new GameObject("TabInventoryController");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var input = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(input);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var panel = root.Q<VisualElement>("inventory__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            input.MenuTogglePressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_Tick_ResolvesInputSource_WhenSpawnedLater()
        {
            var go = new GameObject("TabInventoryController");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var panel = root.Q<VisualElement>("inventory__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            controller.Tick();
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            var lateInput = go.AddComponent<TestInputSource>();
            lateInput.MenuTogglePressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Object.DestroyImmediate(go);
        }

        private static VisualElement BuildTabRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            var tabBar = new VisualElement { name = "inventory__tabbar" };
            tabBar.Add(new Button { name = "inventory__tab-inventory", text = "Inventory" });
            tabBar.Add(new Button { name = "inventory__tab-quests", text = "Quests" });
            tabBar.Add(new Button { name = "inventory__tab-journal", text = "Journal" });
            tabBar.Add(new Button { name = "inventory__tab-calendar", text = "Calendar" });
            panel.Add(tabBar);

            var inventorySection = new VisualElement { name = "inventory__section-inventory" };
            panel.Add(inventorySection);
            for (var i = 0; i < 5; i++)
            {
                inventorySection.Add(new VisualElement { name = $"inventory__belt-slot-{i}" });
            }

            for (var i = 0; i < 2; i++)
            {
                inventorySection.Add(new VisualElement { name = $"inventory__backpack-slot-{i}" });
            }

            panel.Add(new VisualElement { name = "inventory__section-quests" });
            panel.Add(new VisualElement { name = "inventory__section-journal" });
            panel.Add(new VisualElement { name = "inventory__section-calendar" });

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            inventorySection.Add(tooltip);

            return root;
        }

        private static VisualElement BuildTradeRoot()
        {
            var root = new VisualElement { name = "trade__root" };
            root.Add(new VisualElement { name = "trade__buy-panel" });
            root.Add(new VisualElement { name = "trade__sell-panel" });
            root.Add(new VisualElement { name = "trade__order-panel" });
            root.Add(new Label { name = "trade__cart-total" });
            return root;
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool MenuTogglePressedThisFrame;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;

            public bool ConsumeMenuTogglePressed()
            {
                if (!MenuTogglePressedThisFrame)
                {
                    return false;
                }

                MenuTogglePressedThisFrame = false;
                return true;
            }
        }
    }
}
