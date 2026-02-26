using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Reloading;
using Reloader.UI.Toolkit.Runtime;
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

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_HandleMergeIntent_MovesItemsBetweenAreas()
        {
            var go = new GameObject("TabInventoryControllerMerge");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(null, null, runtime);
            runtime.SetBackpackCapacity(2);

            runtime.BeltSlotItemIds[0] = "item-shared";
            runtime.BeltSlotItemIds[1] = "slot-1";
            runtime.BeltSlotItemIds[2] = "slot-2";
            runtime.BeltSlotItemIds[3] = "slot-3";
            runtime.BeltSlotItemIds[4] = "slot-4";
            Assert.That(runtime.TryStoreItem("item-shared", out _, out var storedIndex, out _), Is.True);
            Assert.That(storedIndex, Is.EqualTo(0));

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var payload = new TabInventoryDragController.DragIntentPayload("belt", 0, "backpack", 0);
            controller.HandleIntent(new UiIntent("inventory.drag.merge", payload));

            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("item-shared"));
            Assert.That(runtime.BackpackItemIds[0], Is.EqualTo("item-shared"));

            UnityEngine.Object.DestroyImmediate(go);
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

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void UiToolkitScreenRuntimeBridge_BindTabInventory_UsesRuntimeBackpackCapacityWithoutFloor()
        {
            var bridgeGo = new GameObject("UiBridge");
            var inventoryGo = new GameObject("InventoryController");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(9);
            inventoryController.Configure(null, null, runtime);
            var input = bridgeGo.AddComponent<TestInputSource>();
            var root = BuildTabRoot(backpackSlotCount: 9);

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, "tab-menu-controller", inventoryController, input }) as System.IDisposable;
            Assert.That(subscription, Is.Not.Null);

            var tabController = bridgeGo.transform.Find("tab-menu-controller")?.GetComponent<TabInventoryController>();
            Assert.That(tabController, Is.Not.Null);

            var binderField = typeof(TabInventoryController).GetField("_viewBinder", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(binderField, Is.Not.Null);
            var viewBinder = binderField.GetValue(tabController) as TabInventoryViewBinder;
            Assert.That(viewBinder, Is.Not.Null);

            var backpackSlotsField = typeof(TabInventoryViewBinder).GetField("_backpackSlots", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backpackSlotsField, Is.Not.Null);
            var backpackSlots = backpackSlotsField.GetValue(viewBinder) as VisualElement[];
            Assert.That(backpackSlots, Is.Not.Null);
            Assert.That(backpackSlots.Length, Is.EqualTo(9));

            subscription.Dispose();
            UnityEngine.Object.DestroyImmediate(inventoryGo);
            UnityEngine.Object.DestroyImmediate(bridgeGo);
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

            UnityEngine.Object.DestroyImmediate(go);
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

            var visibilityEventCount = 0;
            var latestVisibility = false;
            GameEvents.OnTabInventoryVisibilityChanged += HandleVisibilityChanged;
            void HandleVisibilityChanged(bool isVisible)
            {
                visibilityEventCount++;
                latestVisibility = isVisible;
            }

            input.MenuTogglePressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(visibilityEventCount, Is.EqualTo(1));
            Assert.That(latestVisibility, Is.True);

            input.MenuTogglePressedThisFrame = true;
            controller.Tick();
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(visibilityEventCount, Is.EqualTo(2));
            Assert.That(latestVisibility, Is.False);

            GameEvents.OnTabInventoryVisibilityChanged -= HandleVisibilityChanged;
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_Tick_UsesInjectedUiStateEvents()
        {
            var go = new GameObject("TabInventoryControllerInjectedEvents");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var input = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            var uiStateEvents = new TestUiStateEvents();
            controller.Configure(uiStateEvents);
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(input);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var gameEventsCount = 0;
            GameEvents.OnTabInventoryVisibilityChanged += HandleGameEventsVisibilityChanged;
            void HandleGameEventsVisibilityChanged(bool _) => gameEventsCount++;

            try
            {
                input.MenuTogglePressedThisFrame = true;
                controller.Tick();

                Assert.That(uiStateEvents.TabInventoryVisibilityRaiseCount, Is.EqualTo(1));
                Assert.That(uiStateEvents.IsTabInventoryVisible, Is.True);
                Assert.That(gameEventsCount, Is.EqualTo(0));
            }
            finally
            {
                GameEvents.OnTabInventoryVisibilityChanged -= HandleGameEventsVisibilityChanged;
                UnityEngine.Object.DestroyImmediate(go);
            }
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
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_Tick_RebindsInput_WhenPreviousInputComponentIsDestroyed()
        {
            var go = new GameObject("TabInventoryController");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var firstInput = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(firstInput);
            controller.Configure(viewBinder, new TabInventoryDragController());

            var panel = root.Q<VisualElement>("inventory__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            firstInput.MenuTogglePressedThisFrame = true;
            controller.Tick();
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            UnityEngine.Object.DestroyImmediate(firstInput);
            var replacementInput = go.AddComponent<TestInputSource>();
            replacementInput.MenuTogglePressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ReloadingWorkbenchController_UsesInjectedUiStateEvents()
        {
            var go = new GameObject("ReloadingWorkbenchInjectedEvents");
            var root = new VisualElement { name = "reloading__root" };
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, operationCount: 3);

            var controller = go.AddComponent<ReloadingWorkbenchController>();
            var uiStateEvents = new TestUiStateEvents();
            controller.Configure(uiStateEvents);
            controller.SetViewBinder(binder);

            GameEvents.RaiseWorkbenchMenuVisibilityChanged(true);
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));

            uiStateEvents.RaiseWorkbenchMenuVisibilityChanged(true);
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            UnityEngine.Object.DestroyImmediate(go);
        }

        private static VisualElement BuildTabRoot(int backpackSlotCount = 2)
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

            for (var i = 0; i < backpackSlotCount; i++)
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
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

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

        private sealed class TestUiStateEvents : IUiStateEvents
        {
            public bool IsShopTradeMenuOpen { get; private set; }
            public bool IsWorkbenchMenuVisible { get; private set; }
            public bool IsTabInventoryVisible { get; private set; }
            public bool IsAnyMenuOpen => IsShopTradeMenuOpen || IsWorkbenchMenuVisible || IsTabInventoryVisible;
            public int TabInventoryVisibilityRaiseCount { get; private set; }

            public event Action<bool> OnWorkbenchMenuVisibilityChanged;
            public event Action<bool> OnTabInventoryVisibilityChanged;

            public void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
            {
                IsWorkbenchMenuVisible = isVisible;
                OnWorkbenchMenuVisibilityChanged?.Invoke(isVisible);
            }

            public void RaiseTabInventoryVisibilityChanged(bool isVisible)
            {
                IsTabInventoryVisible = isVisible;
                TabInventoryVisibilityRaiseCount++;
                OnTabInventoryVisibilityChanged?.Invoke(isVisible);
            }
        }
    }
}
