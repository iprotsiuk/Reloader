using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Economy.Tests.PlayMode
{
    public class EconomyControllerCheckoutPlayModeTests
    {
        [UnityTest]
        public IEnumerator BuyCheckoutRequest_MutatesMoneyStockAndInventory()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(null, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            // Let Unity run Awake/OnEnable subscription cycle.
            yield return null;

            GameEvents.RaiseShopTradeOpenRequested("vendor-1");

            var request = new ShopCheckoutRequest(
                new[] { new ShopCheckoutLine("ammo-22lr", 100) },
                "delivery-standard",
                10);
            GameEvents.RaiseShopBuyCheckoutRequested(request);

            Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(100));
            Assert.That(controller.Runtime.Money, Is.EqualTo(290));
            Assert.That(controller.Runtime.TryGetStock("ammo-22lr", out var stock), Is.True);
            Assert.That(stock, Is.EqualTo(400));

            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(economyGo);
            UnityEngine.Object.DestroyImmediate(inventoryGo);
        }

        private static void SetVendorBindings(EconomyController controller, string vendorId, ShopCatalogDefinition catalog)
        {
            var bindingType = typeof(EconomyController).GetNestedType("VendorCatalogBinding", BindingFlags.NonPublic);
            var binding = Activator.CreateInstance(bindingType);
            var vendorIdField = bindingType.GetField("_vendorId", BindingFlags.NonPublic | BindingFlags.Instance);
            var catalogField = bindingType.GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance);
            vendorIdField.SetValue(binding, vendorId);
            catalogField.SetValue(binding, catalog);

            var listType = typeof(List<>).MakeGenericType(bindingType);
            var list = (IList)Activator.CreateInstance(listType);
            list.Add(binding);

            SetPrivateField(controller, "_vendors", list);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }
}
