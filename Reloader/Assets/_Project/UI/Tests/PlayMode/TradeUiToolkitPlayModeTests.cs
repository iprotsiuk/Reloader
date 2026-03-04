using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Trade;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class TradeUiToolkitPlayModeTests
    {
        [Test]
        public void Render_UpdatesTabAndCartTotal()
        {
            var root = BuildRoot();
            var binder = new TradeViewBinder();
            binder.Initialize(root);

            binder.Render(new TradeUiState(TradeUiTab.Buy, false, "$120", true, false));

            var buyPanel = root.Q<VisualElement>("trade__buy-panel");
            var sellPanel = root.Q<VisualElement>("trade__sell-panel");
            var totalLabel = root.Q<Label>("trade__cart-total");

            Assert.That(buyPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(sellPanel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(totalLabel.text, Is.EqualTo("$120"));
        }

        [Test]
        public void Render_WhenOrderScreenShown_TogglesOrderPanelVisibility()
        {
            var root = BuildRoot();
            var binder = new TradeViewBinder();
            binder.Initialize(root);

            binder.Render(new TradeUiState(TradeUiTab.Buy, true, "$64", true, false));

            var orderPanel = root.Q<VisualElement>("trade__order-panel");
            Assert.That(orderPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void RaiseConfirmBuyIntent_EmitsTradeConfirmBuyKey()
        {
            var root = BuildRoot();
            var binder = new TradeViewBinder();
            binder.Initialize(root);

            string captured = null;
            binder.IntentRaised += i => captured = i.Key;

            var raised = binder.TryRaiseConfirmBuyIntent();

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo("trade.confirm.buy"));
        }

        [Test]
        public void RaiseConfirmSellIntent_EmitsTradeConfirmSellKey()
        {
            var root = BuildRoot();
            var binder = new TradeViewBinder();
            binder.Initialize(root);

            string captured = null;
            binder.IntentRaised += i => captured = i.Key;

            var raised = binder.TryRaiseConfirmSellIntent();

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo("trade.confirm.sell"));
        }

        [Test]
        public void Render_WithBuySlots_PopulatesGrid()
        {
            var root = BuildRoot(withBuySlots: true);
            var binder = new TradeViewBinder();
            binder.Initialize(root);

            var slots = new[]
            {
                new TradeUiSlotViewModel("item-a", "Item A", true, false),
                new TradeUiSlotViewModel("item-b", "Item B", true, true),
                null,
                null,
                null,
                null
            };

            binder.Render(new TradeUiState(TradeUiTab.Buy, false, "$0", true, false, slots, null));

            var buyPanel = root.Q<VisualElement>("trade__buy-panel");
            var renderedSlots = buyPanel.Query<VisualElement>(className: "trade__cell").ToList();
            Assert.That(renderedSlots.Count, Is.EqualTo(6));

            var firstLabel = renderedSlots[0].Q<Label>("trade__cell-label");
            var firstIcon = renderedSlots[0].Q<VisualElement>("trade__cell-icon");
            Assert.That(firstLabel, Is.Null);
            Assert.That(firstIcon, Is.Not.Null);
        }

        [Test]
        public void HandleIntent_ConfirmBuy_WithoutPayload_DoesNotRaiseBuyCheckoutEvent()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var raised = 0;

            void Handler(ShopCheckoutRequest request)
            {
                raised++;
            }

            runtimeHub.OnShopBuyCheckoutRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.confirm.buy"));

                Assert.That(raised, Is.EqualTo(0));
            }
            finally
            {
                runtimeHub.OnShopBuyCheckoutRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleIntent_ConfirmBuy_WithPayload_RaisesBuyCheckoutEvent()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var raised = 0;
            ShopCheckoutRequest captured = null;
            var payload = new ShopCheckoutRequest(
                new[] { new ShopCheckoutLine("item-a", 2) },
                "inventory",
                0);

            void Handler(ShopCheckoutRequest request)
            {
                raised++;
                captured = request;
            }

            runtimeHub.OnShopBuyCheckoutRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.confirm.buy", payload));

                Assert.That(raised, Is.EqualTo(1));
                Assert.That(captured, Is.Not.Null);
                Assert.That(captured.Lines, Is.Not.Null);
                Assert.That(captured.Lines.Length, Is.EqualTo(1));
                Assert.That(captured.Lines[0].ItemId, Is.EqualTo("item-a"));
                Assert.That(captured.Lines[0].Quantity, Is.EqualTo(2));
            }
            finally
            {
                runtimeHub.OnShopBuyCheckoutRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleIntent_ConfirmSell_WithoutPayload_DoesNotRaiseSellCheckoutEvent()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var raised = 0;

            void Handler(ShopCheckoutRequest request)
            {
                raised++;
            }

            runtimeHub.OnShopSellCheckoutRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.confirm.sell"));

                Assert.That(raised, Is.EqualTo(0));
            }
            finally
            {
                runtimeHub.OnShopSellCheckoutRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleIntent_ConfirmSell_WithSelectedSlotWithoutPayload_RaisesSellRequestedQuantityOne()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var raised = 0;
            string capturedItemId = null;
            var capturedQuantity = 0;

            void Handler(string itemId, int quantity)
            {
                raised++;
                capturedItemId = itemId;
                capturedQuantity = quantity;
            }

            runtimeHub.OnShopSellRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.sell.slot", "item-a"));
                controller.HandleIntent(new UiIntent("trade.confirm.sell"));

                Assert.That(raised, Is.EqualTo(1));
                Assert.That(capturedItemId, Is.EqualTo("item-a"));
                Assert.That(capturedQuantity, Is.EqualTo(1));
            }
            finally
            {
                runtimeHub.OnShopSellRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Configure_InjectedShopEvents_UsesInjectedDependencyForIntentAndVisibility()
        {
            var go = new GameObject("trade-controller");
            var root = BuildRoot();
            root.style.display = DisplayStyle.None;
            var binder = new TradeViewBinder();
            binder.Initialize(root);
            var controller = go.AddComponent<TradeController>();
            controller.SetViewBinder(binder);

            var injectedEvents = new FakeShopEvents();
            controller.Configure(injectedEvents);

            var payload = new ShopCheckoutRequest(
                new[] { new ShopCheckoutLine("item-a", 2) },
                "inventory",
                0);

            try
            {
                controller.HandleIntent(new UiIntent("trade.confirm.buy", payload));
                Assert.That(injectedEvents.BuyCheckoutRequestedCount, Is.EqualTo(1));
                Assert.That(injectedEvents.LastBuyCheckoutRequest, Is.SameAs(payload));

                injectedEvents.RaiseShopTradeOpened("vendor-1");
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                injectedEvents.RaiseShopTradeClosed();
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleIntent_ConfirmBuy_WithSelectedSlotWithoutPayload_RaisesBuyRequestedQuantityOne()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var raised = 0;
            string capturedItemId = null;
            var capturedQuantity = 0;

            void Handler(string itemId, int quantity)
            {
                raised++;
                capturedItemId = itemId;
                capturedQuantity = quantity;
            }

            runtimeHub.OnShopBuyRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.buy.slot", "item-a"));
                controller.HandleIntent(new UiIntent("trade.confirm.buy"));

                Assert.That(raised, Is.EqualTo(1));
                Assert.That(capturedItemId, Is.EqualTo("item-a"));
                Assert.That(capturedQuantity, Is.EqualTo(1));
            }
            finally
            {
                runtimeHub.OnShopBuyRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnDisable_UnsubscribesFromShopEvents()
        {
            var go = new GameObject("trade-controller");
            var root = BuildRoot();
            root.style.display = DisplayStyle.None;
            var binder = new TradeViewBinder();
            binder.Initialize(root);
            var controller = go.AddComponent<TradeController>();
            controller.SetViewBinder(binder);

            var injectedEvents = new FakeShopEvents();
            controller.Configure(injectedEvents);
            controller.enabled = false;

            try
            {
                injectedEvents.RaiseShopTradeOpened("vendor-1");
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static VisualElement BuildRoot(bool withBuySlots = false, bool withSellSlots = false)
        {
            var root = new VisualElement { name = "trade__root" };
            var buyPanel = new VisualElement { name = "trade__buy-panel" };
            if (withBuySlots)
            {
                for (var i = 0; i < 6; i++)
                {
                    var slot = new VisualElement { name = $"slot-{i}" };
                    slot.AddToClassList("trade__cell");
                    buyPanel.Add(slot);
                }
            }

            root.Add(buyPanel);
            var sellPanel = new VisualElement { name = "trade__sell-panel" };
            if (withSellSlots)
            {
                for (var i = 0; i < 6; i++)
                {
                    var slot = new VisualElement { name = $"sell-slot-{i}" };
                    slot.AddToClassList("trade__cell");
                    sellPanel.Add(slot);
                }
            }

            root.Add(sellPanel);
            root.Add(new VisualElement { name = "trade__order-panel" });
            root.Add(new Label { name = "trade__cart-total" });
            return root;
        }

        private sealed class FakeShopEvents : IShopEvents
        {
            public int BuyCheckoutRequestedCount { get; private set; }
            public ShopCheckoutRequest LastBuyCheckoutRequest { get; private set; }

            public event System.Action<string> OnShopTradeOpenRequested;
            public event System.Action<string> OnShopTradeOpened;
            public event System.Action OnShopTradeClosed;
            public event System.Action<string, int> OnShopBuyRequested;
            public event System.Action<string, int> OnShopSellRequested;
            public event System.Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested;
            public event System.Action<ShopCheckoutRequest> OnShopSellCheckoutRequested;
            public event System.Action<ShopTradeResultPayload> OnShopTradeResultReceived;

#pragma warning disable CS0618
            public event System.Action<string, int, bool, bool, string> OnShopTradeResult;
#pragma warning restore CS0618

            public void RaiseShopTradeOpenRequested(string vendorId) => OnShopTradeOpenRequested?.Invoke(vendorId);
            public void RaiseShopTradeOpened(string vendorId) => OnShopTradeOpened?.Invoke(vendorId);
            public void RaiseShopTradeClosed() => OnShopTradeClosed?.Invoke();
            public void RaiseShopBuyRequested(string itemId, int quantity) => OnShopBuyRequested?.Invoke(itemId, quantity);
            public void RaiseShopSellRequested(string itemId, int quantity) => OnShopSellRequested?.Invoke(itemId, quantity);

            public void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request)
            {
                BuyCheckoutRequestedCount++;
                LastBuyCheckoutRequest = request;
                OnShopBuyCheckoutRequested?.Invoke(request);
            }

            public void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request) => OnShopSellCheckoutRequested?.Invoke(request);

            public void RaiseShopTradeResult(ShopTradeResultPayload payload)
            {
                OnShopTradeResultReceived?.Invoke(payload);
#pragma warning disable CS0618
                OnShopTradeResult?.Invoke(
                    payload.ItemId,
                    payload.Quantity,
                    payload.IsBuy,
                    payload.Success,
                    payload.Success ? string.Empty : payload.FailureReason.ToString());
#pragma warning restore CS0618
            }

#pragma warning disable CS0618
            public void RaiseShopTradeResult(string itemId, int quantity, bool isBuy, bool success, string failureReason)
            {
                RaiseShopTradeResult(new ShopTradeResultPayload(
                    itemId,
                    quantity,
                    isBuy,
                    success,
                    ShopTradeResultPayload.ParseLegacyFailureReason(failureReason, success)));
            }
#pragma warning restore CS0618
        }
    }
}
