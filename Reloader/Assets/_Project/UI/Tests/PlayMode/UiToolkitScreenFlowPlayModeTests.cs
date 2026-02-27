using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.BeltHud;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.InteractionHint;
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
        public void UiToolkitScreenRuntimeBridge_BindInteractionHint_RendersRuntimePayloadAndClears()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), runtimeHub);

            var bridgeGo = new GameObject("UiBridgeInteractionHint");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var root = BuildInteractionHintRoot();

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindInteractionHint",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, "interaction-hint-controller" }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            var controller = bridgeGo.transform.Find("interaction-hint-controller")?.GetComponent<InteractionHintController>();
            Assert.That(controller, Is.Not.Null);

            var label = root.Q<Label>("interaction-hint__text");
            Assert.That(label, Is.Not.Null);

            try
            {
                runtimeHub.RaiseInteractionHintShown(new InteractionHintPayload("pickup", "Pick up", "Hodgdon Varget"));
                Assert.That(label.text, Is.EqualTo("Pick up Hodgdon Varget"));
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                runtimeHub.RaiseInteractionHintCleared();
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                subscription.Dispose();
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(bridgeGo);
            }
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
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), runtimeHub);

            var go = new GameObject("TradeController");
            var root = BuildTradeRoot();
            root.style.display = DisplayStyle.None;

            var binder = new TradeViewBinder();
            binder.Initialize(root);

            var controller = go.AddComponent<TradeController>();
            controller.SetViewBinder(binder);

            try
            {
                RuntimeKernelBootstrapper.ShopEvents.RaiseShopTradeOpened("vendor-1");
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                RuntimeKernelBootstrapper.ShopEvents.RaiseShopTradeClosed();
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TradeController_WithoutInjectedEvents_RebindsInboundCallbacksImmediatelyWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var go = new GameObject("TradeControllerRuntimeHubReconfigure");
            var root = BuildTradeRoot();
            root.style.display = DisplayStyle.None;

            var binder = new TradeViewBinder();
            binder.Initialize(root);

            var controller = go.AddComponent<TradeController>();
            controller.SetViewBinder(binder);

            try
            {
                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);
                replacementHub.RaiseShopTradeOpened("vendor-1");
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                replacementHub.RaiseShopTradeClosed();
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TradeController_WithoutInjectedEvents_WhenOpen_ReconcilesVisibilityImmediatelyAfterRuntimeKernelHubReconfigure()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var go = new GameObject("TradeControllerRuntimeHubReconfigureVisibility");
            var root = BuildTradeRoot();
            root.style.display = DisplayStyle.None;

            var binder = new TradeViewBinder();
            binder.Initialize(root);

            var controller = go.AddComponent<TradeController>();
            controller.SetViewBinder(binder);

            try
            {
                initialHub.RaiseShopTradeOpened("vendor-1");
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);

                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
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
            RuntimeKernelBootstrapper.UiStateEvents.OnTabInventoryVisibilityChanged += HandleVisibilityChanged;
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

            RuntimeKernelBootstrapper.UiStateEvents.OnTabInventoryVisibilityChanged -= HandleVisibilityChanged;
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TabInventoryController_Tick_UsesInjectedUiStateEvents()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

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
            RuntimeKernelBootstrapper.UiStateEvents.OnTabInventoryVisibilityChanged += HandleGameEventsVisibilityChanged;
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
                RuntimeKernelBootstrapper.UiStateEvents.OnTabInventoryVisibilityChanged -= HandleGameEventsVisibilityChanged;
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TabInventoryController_UsesInjectedInventoryEvents_InsteadOfStaticGameEvents()
        {
            var go = new GameObject("TabInventoryInjectedInventoryEvents");

            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildTabRoot();
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var controller = go.AddComponent<TabInventoryController>();
            var injectedInventoryEvents = new TestInventoryEvents();
            controller.Configure(injectedInventoryEvents);
            controller.SetInventoryController(inventoryController);
            controller.Configure(viewBinder, new TabInventoryDragController());

            try
            {
                runtime.BeltSlotItemIds[0] = null;
                injectedInventoryEvents.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Null);

                runtime.BeltSlotItemIds[0] = "item-injected";
                RuntimeKernelBootstrapper.InventoryEvents.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Null);

                injectedInventoryEvents.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TabInventoryController_WithoutInjectedInventoryEvents_RebindsWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var go = new GameObject("TabInventoryRuntimeHubReconfigure");

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

            try
            {
                runtime.BeltSlotItemIds[0] = null;
                initialHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Null);

                runtime.BeltSlotItemIds[0] = "item-initial";
                initialHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Not.Null);

                RuntimeKernelBootstrapper.Events = replacementHub;

                runtime.BeltSlotItemIds[0] = null;
                initialHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Not.Null);

                replacementHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("inventory__slot-item-belt-0"), Is.Null);
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TabInventoryController_WithoutInjectedUiStateEvents_WhenOpen_ReplaysVisibilityAfterRuntimeKernelHubReconfigure()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var go = new GameObject("TabInventoryRuntimeHubReconfigureUiState");
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

            try
            {
                input.MenuTogglePressedThisFrame = true;
                controller.Tick();

                Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(initialHub.IsTabInventoryVisible, Is.True);

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);

                Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(replacementHub.IsTabInventoryVisible, Is.True);
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BeltHudController_UsesInjectedInventoryEvents_InsteadOfStaticGameEvents()
        {
            var go = new GameObject("BeltHudInjectedInventoryEvents");

            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildBeltRoot();
            var viewBinder = new BeltHudViewBinder();
            viewBinder.Initialize(root, slotCount: PlayerInventoryRuntime.BeltSlotCount);

            var controller = go.AddComponent<BeltHudController>();
            var injectedInventoryEvents = new TestInventoryEvents();
            controller.Configure(injectedInventoryEvents);
            controller.SetInventoryController(inventoryController);
            controller.SetViewBinder(viewBinder);

            var staticSelectionChangedCount = 0;
            RuntimeKernelBootstrapper.InventoryEvents.OnBeltSelectionChanged += HandleSelectionChanged;
            void HandleSelectionChanged(int _) => staticSelectionChangedCount++;

            try
            {
                runtime.BeltSlotItemIds[0] = null;
                injectedInventoryEvents.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Null);

                runtime.BeltSlotItemIds[0] = "item-belt";
                RuntimeKernelBootstrapper.InventoryEvents.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Null);

                injectedInventoryEvents.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Not.Null);

                runtime.SelectBeltSlot(0);
                controller.HandleIntent(new UiIntent("belt.slot.select", 1));
                Assert.That(injectedInventoryEvents.BeltSelectionChangedRaiseCount, Is.EqualTo(1));
                Assert.That(staticSelectionChangedCount, Is.EqualTo(0));
            }
            finally
            {
                RuntimeKernelBootstrapper.InventoryEvents.OnBeltSelectionChanged -= HandleSelectionChanged;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BeltHudController_WithoutInjectedInventoryEvents_RebindsWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var go = new GameObject("BeltHudRuntimeHubReconfigure");

            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(null, null, runtime);

            var root = BuildBeltRoot();
            var viewBinder = new BeltHudViewBinder();
            viewBinder.Initialize(root, slotCount: PlayerInventoryRuntime.BeltSlotCount);

            var controller = go.AddComponent<BeltHudController>();
            controller.SetInventoryController(inventoryController);
            controller.SetViewBinder(viewBinder);

            var initialHubSelectionChangedCount = 0;
            var replacementHubSelectionChangedCount = 0;
            initialHub.OnBeltSelectionChanged += HandleInitialHubSelectionChanged;
            replacementHub.OnBeltSelectionChanged += HandleReplacementHubSelectionChanged;
            void HandleInitialHubSelectionChanged(int _) => initialHubSelectionChangedCount++;
            void HandleReplacementHubSelectionChanged(int _) => replacementHubSelectionChangedCount++;

            try
            {
                runtime.BeltSlotItemIds[0] = null;
                initialHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Null);

                runtime.BeltSlotItemIds[0] = "item-initial";
                initialHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Not.Null);

                RuntimeKernelBootstrapper.Events = replacementHub;

                runtime.BeltSlotItemIds[0] = null;
                initialHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Not.Null);

                replacementHub.RaiseInventoryChanged();
                Assert.That(root.Q<VisualElement>("belt-hud__slot-item-0"), Is.Null);

                runtime.SelectBeltSlot(0);
                controller.HandleIntent(new UiIntent("belt.slot.select", 2));
                Assert.That(initialHubSelectionChangedCount, Is.EqualTo(0));
                Assert.That(replacementHubSelectionChangedCount, Is.EqualTo(1));
            }
            finally
            {
                initialHub.OnBeltSelectionChanged -= HandleInitialHubSelectionChanged;
                replacementHub.OnBeltSelectionChanged -= HandleReplacementHubSelectionChanged;
                RuntimeKernelBootstrapper.Events = originalHub;
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

            RuntimeKernelBootstrapper.UiStateEvents.RaiseWorkbenchMenuVisibilityChanged(true);
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));

            uiStateEvents.RaiseWorkbenchMenuVisibilityChanged(true);
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ReloadingWorkbenchController_WithoutInjectedEvents_RebindsWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var go = new GameObject("ReloadingWorkbenchRuntimeHubReconfigure");
            var root = new VisualElement { name = "reloading__root" };
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, operationCount: 3);

            var controller = go.AddComponent<ReloadingWorkbenchController>();
            controller.SetViewBinder(binder);

            try
            {
                initialHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                initialHub.RaiseWorkbenchMenuVisibilityChanged(false);
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);

                initialHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));

                replacementHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            }
            finally
            {
                replacementHub.RaiseWorkbenchMenuVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ReloadingWorkbenchController_WithoutInjectedEvents_WhenVisible_ReconcilesVisibilityImmediatelyAfterRuntimeKernelHubReconfigure()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var go = new GameObject("ReloadingWorkbenchRuntimeHubReconfigureVisibility");
            var root = new VisualElement { name = "reloading__root" };
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, operationCount: 3);

            var controller = go.AddComponent<ReloadingWorkbenchController>();
            controller.SetViewBinder(binder);

            try
            {
                initialHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);

                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(go);
            }
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

        private static VisualElement BuildInteractionHintRoot()
        {
            var root = new VisualElement { name = "interaction-hint__root" };
            root.Add(new Label { name = "interaction-hint__text" });
            return root;
        }

        private static VisualElement BuildBeltRoot()
        {
            var root = new VisualElement { name = "belt-hud__root" };
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var slot = new VisualElement { name = $"belt-hud__slot-{i}" };
                var label = new Label((i + 1).ToString()) { name = $"belt-hud__slot-label-{i}" };
                slot.Add(label);
                root.Add(slot);
            }

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

        private sealed class TestInventoryEvents : IInventoryEvents
        {
            public int BeltSelectionChangedRaiseCount { get; private set; }

            public event Action OnSaveStarted;
            public event Action OnSaveCompleted;
            public event Action OnLoadStarted;
            public event Action OnLoadCompleted;
            public event Action<string> OnItemPickupRequested;
            public event Action<string, InventoryArea, int> OnItemStored;
            public event Action<string, PickupRejectReason> OnItemPickupRejected;
            public event Action<int> OnBeltSelectionChanged;
            public event Action OnInventoryChanged;
            public event Action<int> OnMoneyChanged;

            public void RaiseSaveStarted() => OnSaveStarted?.Invoke();
            public void RaiseSaveCompleted() => OnSaveCompleted?.Invoke();
            public void RaiseLoadStarted() => OnLoadStarted?.Invoke();
            public void RaiseLoadCompleted() => OnLoadCompleted?.Invoke();
            public void RaiseItemPickupRequested(string itemId) => OnItemPickupRequested?.Invoke(itemId);
            public void RaiseItemStored(string itemId, InventoryArea area, int index) => OnItemStored?.Invoke(itemId, area, index);
            public void RaiseItemPickupRejected(string itemId, PickupRejectReason reason) => OnItemPickupRejected?.Invoke(itemId, reason);

            public void RaiseBeltSelectionChanged(int selectedBeltIndex)
            {
                BeltSelectionChangedRaiseCount++;
                OnBeltSelectionChanged?.Invoke(selectedBeltIndex);
            }

            public void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
            public void RaiseMoneyChanged(int amount) => OnMoneyChanged?.Invoke(amount);
        }
    }
}
