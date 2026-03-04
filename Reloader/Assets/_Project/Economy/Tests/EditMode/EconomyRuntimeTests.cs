using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Economy.Tests.EditMode
{
    public class EconomyRuntimeTests
    {
        [Test]
        public void StartsWithDefaultMoney_500()
        {
            var runtime = new EconomyRuntime();
            Assert.That(runtime.Money, Is.EqualTo(500));
        }

        [Test]
        public void BuyAndSell_MutatesMoneyAndStock()
        {
            var runtime = new EconomyRuntime(500);
            var catalog = BuildCatalog("powder-varget", 20, 10000);
            runtime.OpenVendor("vendor-1", catalog);

            var bought = runtime.TryBuy("powder-varget", 5, out var buyCost, out var buyReason);
            var sold = runtime.TrySell("powder-varget", 2, out var sellIncome, out var sellReason);
            runtime.TryGetStock("powder-varget", out var stock);

            Assert.That(bought, Is.True);
            Assert.That(sold, Is.True);
            Assert.That(buyReason, Is.EqualTo(TradeFailureReason.None));
            Assert.That(sellReason, Is.EqualTo(TradeFailureReason.None));
            Assert.That(buyCost, Is.EqualTo(100));
            Assert.That(sellIncome, Is.EqualTo(40));
            Assert.That(runtime.Money, Is.EqualTo(440));
            Assert.That(stock, Is.EqualTo(9997));
        }

        [Test]
        public void TryBuy_WhenInsufficientFunds_ReturnsFailureWithoutMutation()
        {
            var runtime = new EconomyRuntime(10);
            var catalog = BuildCatalog("press-lee", 100, 10000);
            runtime.OpenVendor("vendor-1", catalog);
            runtime.TryGetStock("press-lee", out var stockBefore);

            var bought = runtime.TryBuy("press-lee", 1, out _, out var reason);
            runtime.TryGetStock("press-lee", out var stockAfter);

            Assert.That(bought, Is.False);
            Assert.That(reason, Is.EqualTo(TradeFailureReason.InsufficientFunds));
            Assert.That(runtime.Money, Is.EqualTo(10));
            Assert.That(stockAfter, Is.EqualTo(stockBefore));
        }

        [Test]
        public void TryBuyBatch_WithDeliveryFee_DeductsMoneyAndStock()
        {
            var runtime = new EconomyRuntime(1000);
            var catalog = BuildCatalog(new[]
            {
                ("ammo-22lr", 2, 300),
                ("powder-varget", 40, 30)
            });
            runtime.OpenVendor("vendor-1", catalog);

            var lines = new List<(string itemId, int quantity)>
            {
                ("ammo-22lr", 100),
                ("powder-varget", 5)
            };

            var bought = runtime.TryBuyBatch(lines, 50, out var totalPrice, out var reason);
            runtime.TryGetStock("ammo-22lr", out var ammoStock);
            runtime.TryGetStock("powder-varget", out var powderStock);

            Assert.That(bought, Is.True);
            Assert.That(reason, Is.EqualTo(TradeFailureReason.None));
            Assert.That(totalPrice, Is.EqualTo(450));
            Assert.That(runtime.Money, Is.EqualTo(550));
            Assert.That(ammoStock, Is.EqualTo(200));
            Assert.That(powderStock, Is.EqualTo(25));
        }

        [Test]
        public void TryBuyBatch_WhenAnyLineFails_RollsBackAllMutations()
        {
            var runtime = new EconomyRuntime(200);
            var catalog = BuildCatalog(new[]
            {
                ("ammo-22lr", 2, 300),
                ("powder-varget", 40, 30)
            });
            runtime.OpenVendor("vendor-1", catalog);
            runtime.TryGetStock("ammo-22lr", out var ammoBefore);
            runtime.TryGetStock("powder-varget", out var powderBefore);

            var lines = new List<(string itemId, int quantity)>
            {
                ("ammo-22lr", 50),
                ("powder-varget", 5)
            };

            var bought = runtime.TryBuyBatch(lines, 50, out _, out var reason);
            runtime.TryGetStock("ammo-22lr", out var ammoAfter);
            runtime.TryGetStock("powder-varget", out var powderAfter);

            Assert.That(bought, Is.False);
            Assert.That(reason, Is.EqualTo(TradeFailureReason.InsufficientFunds));
            Assert.That(runtime.Money, Is.EqualTo(200));
            Assert.That(ammoAfter, Is.EqualTo(ammoBefore));
            Assert.That(powderAfter, Is.EqualTo(powderBefore));
        }

        [Test]
        public void TryBuyBatch_WhenDeliveryFeeCannotBePaid_RollsBackAllMutations()
        {
            var runtime = new EconomyRuntime(130);
            var catalog = BuildCatalog(new[]
            {
                ("ammo-22lr", 2, 300),
                ("powder-varget", 40, 30)
            });
            runtime.OpenVendor("vendor-1", catalog);
            runtime.TryGetStock("ammo-22lr", out var ammoBefore);
            runtime.TryGetStock("powder-varget", out var powderBefore);

            var lines = new List<(string itemId, int quantity)>
            {
                ("ammo-22lr", 50)
            };

            var bought = runtime.TryBuyBatch(lines, 50, out _, out var reason);
            runtime.TryGetStock("ammo-22lr", out var ammoAfter);
            runtime.TryGetStock("powder-varget", out var powderAfter);

            Assert.That(bought, Is.False);
            Assert.That(reason, Is.EqualTo(TradeFailureReason.InsufficientFunds));
            Assert.That(runtime.Money, Is.EqualTo(130));
            Assert.That(ammoAfter, Is.EqualTo(ammoBefore));
            Assert.That(powderAfter, Is.EqualTo(powderBefore));
        }

        private static ShopCatalogDefinition BuildCatalog(string itemId, int unitPrice, int stock)
        {
            return BuildCatalog(new[] { (itemId, unitPrice, stock) });
        }

        private static ShopCatalogDefinition BuildCatalog((string itemId, int unitPrice, int stock)[] items)
        {
            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            var itemJson = new System.Text.StringBuilder();
            itemJson.Append("{\"_items\":[");
            for (var i = 0; i < items.Length; i++)
            {
                if (i > 0)
                {
                    itemJson.Append(",");
                }

                itemJson.Append("{\"_itemId\":\"")
                    .Append(items[i].itemId)
                    .Append("\",\"_displayName\":\"Item\",\"_category\":\"powder\",\"_unitPrice\":")
                    .Append(items[i].unitPrice)
                    .Append(",\"_startingStock\":")
                    .Append(items[i].stock)
                    .Append("}");
            }

            itemJson.Append("]}");
            var json = itemJson.ToString();
            JsonUtility.FromJsonOverwrite(json, catalog);
            return catalog;
        }
    }
}
