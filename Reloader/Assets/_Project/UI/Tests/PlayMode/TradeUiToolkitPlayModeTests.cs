using NUnit.Framework;
using Reloader.Core.Events;
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
        public void HandleIntent_ConfirmBuy_WithoutPayload_DoesNotRaiseBuyCheckoutEvent()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var raised = 0;

            void Handler(ShopCheckoutRequest request)
            {
                raised++;
            }

            GameEvents.OnShopBuyCheckoutRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.confirm.buy"));

                Assert.That(raised, Is.EqualTo(0));
            }
            finally
            {
                GameEvents.OnShopBuyCheckoutRequested -= Handler;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleIntent_ConfirmBuy_WithPayload_RaisesBuyCheckoutEvent()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
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

            GameEvents.OnShopBuyCheckoutRequested += Handler;
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
                GameEvents.OnShopBuyCheckoutRequested -= Handler;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleIntent_ConfirmSell_WithoutPayload_DoesNotRaiseSellCheckoutEvent()
        {
            var go = new GameObject("trade-controller");
            var controller = go.AddComponent<TradeController>();
            var raised = 0;

            void Handler(ShopCheckoutRequest request)
            {
                raised++;
            }

            GameEvents.OnShopSellCheckoutRequested += Handler;
            try
            {
                controller.HandleIntent(new UiIntent("trade.confirm.sell"));

                Assert.That(raised, Is.EqualTo(0));
            }
            finally
            {
                GameEvents.OnShopSellCheckoutRequested -= Handler;
                Object.DestroyImmediate(go);
            }
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "trade__root" };
            root.Add(new VisualElement { name = "trade__buy-panel" });
            root.Add(new VisualElement { name = "trade__sell-panel" });
            root.Add(new VisualElement { name = "trade__order-panel" });
            root.Add(new Label { name = "trade__cart-total" });
            return root;
        }
    }
}
