using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
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
        public IEnumerator Configure_InjectedShopAndInventoryEvents_UsesInjectedInventoryChannelWithoutStaticMoneyOrInventoryEvents()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            economyGo.SetActive(false);
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var runtimeHubMoneyChangedCount = 0;
            var runtimeHubInventoryChangedCount = 0;
            void HandleRuntimeHubMoneyChanged(int _) => runtimeHubMoneyChangedCount++;
            void HandleRuntimeHubInventoryChanged() => runtimeHubInventoryChangedCount++;

            var injectedShopEvents = new FakeShopEvents();
            var injectedInventoryEvents = new FakeInventoryEvents();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            runtimeHub.OnMoneyChanged += HandleRuntimeHubMoneyChanged;
            runtimeHub.OnInventoryChanged += HandleRuntimeHubInventoryChanged;
            try
            {
                controller.Configure(injectedShopEvents, injectedInventoryEvents);
                economyGo.SetActive(true);

                yield return null;

                injectedShopEvents.RaiseShopTradeOpenRequested("vendor-1");
                injectedShopEvents.RaiseShopBuyCheckoutRequested(
                    new ShopCheckoutRequest(
                        new[] { new ShopCheckoutLine("ammo-22lr", 100) },
                        "delivery-standard",
                        10));

                Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(100));
                Assert.That(controller.Runtime.Money, Is.EqualTo(290));
                Assert.That(injectedInventoryEvents.LastMoneyChangedAmount, Is.EqualTo(290));
                Assert.That(injectedInventoryEvents.MoneyChangedCount, Is.GreaterThanOrEqualTo(2));
                Assert.That(injectedInventoryEvents.InventoryChangedCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(runtimeHubMoneyChangedCount, Is.EqualTo(0));
                Assert.That(runtimeHubInventoryChangedCount, Is.EqualTo(0));
            }
            finally
            {
                runtimeHub.OnMoneyChanged -= HandleRuntimeHubMoneyChanged;
                runtimeHub.OnInventoryChanged -= HandleRuntimeHubInventoryChanged;
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator Configure_InjectedShopEvents_UsesInjectedChannelInsteadOfRuntimeHub()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var injectedEvents = new FakeShopEvents();
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            try
            {
                controller.Configure(injectedEvents);

                yield return null;

                injectedEvents.RaiseShopTradeOpenRequested("vendor-1");
                injectedEvents.RaiseShopBuyCheckoutRequested(
                    new ShopCheckoutRequest(
                        new[] { new ShopCheckoutLine("ammo-22lr", 100) },
                        "delivery-standard",
                        10));

                Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(100));
                Assert.That(controller.Runtime.Money, Is.EqualTo(290));

                runtimeHub.RaiseShopBuyCheckoutRequested(
                    new ShopCheckoutRequest(
                        new[] { new ShopCheckoutLine("ammo-22lr", 10) },
                        "delivery-standard",
                        0));

                Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(100));
                Assert.That(controller.Runtime.Money, Is.EqualTo(290));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator BuyCheckoutRequest_MutatesMoneyStockAndInventory()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            try
            {
                // Let Unity run Awake/OnEnable subscription cycle.
                yield return null;

                runtimeHub.RaiseShopTradeOpenRequested("vendor-1");

                var request = new ShopCheckoutRequest(
                    new[] { new ShopCheckoutLine("ammo-22lr", 100) },
                    "delivery-standard",
                    10);
                runtimeHub.RaiseShopBuyCheckoutRequested(request);

                Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(100));
                Assert.That(controller.Runtime.Money, Is.EqualTo(290));
                Assert.That(controller.Runtime.TryGetStock("ammo-22lr", out var stock), Is.True);
                Assert.That(stock, Is.EqualTo(400));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator Configure_WithoutInjectedShopEvents_RebindsInboundCallbacksImmediatelyWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var openedCount = 0;
            replacementHub.OnShopTradeOpened += _ => openedCount++;

            try
            {
                yield return null;

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);
                replacementHub.RaiseShopTradeOpenRequested("vendor-1");

                Assert.That(openedCount, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator Configure_WithoutInjectedShopEvents_RebindsInboundCallbacksWhenRuntimeKernelHubIsReplacedViaSetter()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var openedCount = 0;
            replacementHub.OnShopTradeOpened += _ => openedCount++;

            try
            {
                yield return null;

                RuntimeKernelBootstrapper.Events = replacementHub;
                initialHub.RaiseShopTradeOpenRequested("vendor-1");
                replacementHub.RaiseShopTradeOpenRequested("vendor-1");

                Assert.That(openedCount, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator BuyCheckoutRequest_WhenLaterLineCannotStore_RollsBackInventoryMoneyAndStock()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, null, runtime);
            runtime.SetBackpackCapacity(0);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 1000);
            SetPrivateField(controller, "_runtime", new EconomyRuntime(1000));

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[" +
                "{\"_itemId\":\"item-1\",\"_displayName\":\"Item 1\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-2\",\"_displayName\":\"Item 2\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-3\",\"_displayName\":\"Item 3\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-4\",\"_displayName\":\"Item 4\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-5\",\"_displayName\":\"Item 5\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-6\",\"_displayName\":\"Item 6\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}" +
                "]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            try
            {
                yield return null;

                runtimeHub.RaiseShopTradeOpenRequested("vendor-1");
                var request = new ShopCheckoutRequest(
                    new[]
                    {
                        new ShopCheckoutLine("item-1", 1),
                        new ShopCheckoutLine("item-2", 1),
                        new ShopCheckoutLine("item-3", 1),
                        new ShopCheckoutLine("item-4", 1),
                        new ShopCheckoutLine("item-5", 1),
                        new ShopCheckoutLine("item-6", 1)
                    },
                    "delivery-standard",
                    0);

                runtimeHub.RaiseShopBuyCheckoutRequested(request);

                Assert.That(runtime.GetItemQuantity("item-1"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-2"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-3"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-4"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-5"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-6"), Is.EqualTo(0));
                Assert.That(controller.Runtime.Money, Is.EqualTo(1000));

                Assert.That(controller.Runtime.TryGetStock("item-1", out var stock1), Is.True);
                Assert.That(stock1, Is.EqualTo(10));
                Assert.That(controller.Runtime.TryGetStock("item-6", out var stock6), Is.True);
                Assert.That(stock6, Is.EqualTo(10));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator BuyCheckoutRequest_WhenLaterLineCannotStore_WithDeliveryFee_RollsBackMoneyAndStockToPreTransactionState()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, null, runtime);
            runtime.SetBackpackCapacity(0);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 1000);
            SetPrivateField(controller, "_runtime", new EconomyRuntime(1000));

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[" +
                "{\"_itemId\":\"item-1\",\"_displayName\":\"Item 1\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-2\",\"_displayName\":\"Item 2\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-3\",\"_displayName\":\"Item 3\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-4\",\"_displayName\":\"Item 4\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-5\",\"_displayName\":\"Item 5\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}," +
                "{\"_itemId\":\"item-6\",\"_displayName\":\"Item 6\",\"_category\":\"misc\",\"_unitPrice\":1,\"_startingStock\":10}" +
                "]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            try
            {
                yield return null;

                runtimeHub.RaiseShopTradeOpenRequested("vendor-1");
                var request = new ShopCheckoutRequest(
                    new[]
                    {
                        new ShopCheckoutLine("item-1", 1),
                        new ShopCheckoutLine("item-2", 1),
                        new ShopCheckoutLine("item-3", 1),
                        new ShopCheckoutLine("item-4", 1),
                        new ShopCheckoutLine("item-5", 1),
                        new ShopCheckoutLine("item-6", 1)
                    },
                    "delivery-standard",
                    50);

                runtimeHub.RaiseShopBuyCheckoutRequested(request);

                Assert.That(runtime.GetItemQuantity("item-1"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-2"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-3"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-4"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-5"), Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("item-6"), Is.EqualTo(0));
                Assert.That(controller.Runtime.Money, Is.EqualTo(1000));

                Assert.That(controller.Runtime.TryGetStock("item-1", out var stock1), Is.True);
                Assert.That(stock1, Is.EqualTo(10));
                Assert.That(controller.Runtime.TryGetStock("item-6", out var stock6), Is.True);
                Assert.That(stock6, Is.EqualTo(10));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator SellCheckoutRequest_WhenRemovalFailsMidBatch_RollsBackInventoryMoneyAndStock()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            inventoryController.Configure(input, null, runtime);
            Assert.That(runtime.TryAddStackItem("ammo-22lr", 3, out _, out _, out _), Is.True);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            try
            {
                yield return null;

                runtimeHub.RaiseShopTradeOpenRequested("vendor-1");
                var request = new ShopCheckoutRequest(
                    new[]
                    {
                        new ShopCheckoutLine("ammo-22lr", 2),
                        new ShopCheckoutLine("ammo-22lr", 2)
                    },
                    "delivery-standard",
                    0);

                runtimeHub.RaiseShopSellCheckoutRequested(request);

                Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(3));
                Assert.That(controller.Runtime.Money, Is.EqualTo(500));
                Assert.That(controller.Runtime.TryGetStock("ammo-22lr", out var stock), Is.True);
                Assert.That(stock, Is.EqualTo(500));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator MissingInventoryController_LogsErrorAndBuyRequestFailsGracefully()
        {
            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();

            var failureReason = ShopTradeFailureReason.None;
            void Handler(ShopTradeResultPayload payload)
            {
                if (!payload.Success)
                {
                    failureReason = payload.FailureReason;
                }
            }

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            runtimeHub.OnShopTradeResultReceived += Handler;
            try
            {
                LogAssert.Expect(LogType.Error, "EconomyController requires a PlayerInventoryController reference.");
                yield return null;

                runtimeHub.RaiseShopBuyRequested("ammo-22lr", 1);
                Assert.That(failureReason, Is.EqualTo(ShopTradeFailureReason.InventoryFull));
            }
            finally
            {
                runtimeHub.OnShopTradeResultReceived -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(economyGo);
            }

            Assert.That(controller.Runtime, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DisabledController_DoesNotProcessShopEvents()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            SetVendorBindings(controller, "vendor-1", catalog);

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            try
            {
                yield return null;
                controller.enabled = false;

                runtimeHub.RaiseShopTradeOpenRequested("vendor-1");
                runtimeHub.RaiseShopBuyCheckoutRequested(
                    new ShopCheckoutRequest(
                        new[] { new ShopCheckoutLine("ammo-22lr", 100) },
                        "delivery-standard",
                        10));

                Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(0));
                Assert.That(controller.Runtime.Money, Is.EqualTo(500));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
        }

        [UnityTest]
        public IEnumerator TradeOpenRequested_WhenVendorIdMissingInBindings_UsesDefaultCatalogFallback()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var input = inventoryGo.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            inventoryController.Configure(input, null, runtime);

            var economyGo = new GameObject("EconomyController");
            var controller = economyGo.AddComponent<EconomyController>();
            SetPrivateField(controller, "_inventoryControllerBehaviour", inventoryController);
            SetPrivateField(controller, "_startingMoney", 500);

            var catalog = ScriptableObject.CreateInstance<ShopCatalogDefinition>();
            JsonUtility.FromJsonOverwrite(
                "{\"_items\":[{\"_itemId\":\"ammo-22lr\",\"_displayName\":\"22LR\",\"_category\":\"ammo\",\"_unitPrice\":2,\"_startingStock\":500}]}",
                catalog);
            var bindingType = typeof(EconomyController).GetNestedType("VendorCatalogBinding", BindingFlags.NonPublic);
            var emptyListType = typeof(List<>).MakeGenericType(bindingType);
            var emptyVendors = Activator.CreateInstance(emptyListType);
            SetPrivateField(controller, "_vendors", emptyVendors);
            SetPrivateField(controller, "_defaultVendorCatalog", catalog);
            SetPrivateField(controller, "_defaultVendorId", "vendor-reloading-store");

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var openedCount = 0;
            runtimeHub.OnShopTradeOpened += _ => openedCount++;
            try
            {
                yield return null;
                runtimeHub.RaiseShopTradeOpenRequested("vendor-weapon-store");
                Assert.That(openedCount, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(economyGo);
                UnityEngine.Object.DestroyImmediate(inventoryGo);
            }
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

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
        }

        private sealed class FakeShopEvents : IShopEvents
        {
            public event Action<string> OnShopTradeOpenRequested;
            public event Action<string> OnShopTradeOpened;
            public event Action OnShopTradeClosed;
            public event Action<string, int> OnShopBuyRequested;
            public event Action<string, int> OnShopSellRequested;
            public event Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested;
            public event Action<ShopCheckoutRequest> OnShopSellCheckoutRequested;
            public event Action<ShopTradeResultPayload> OnShopTradeResultReceived;

            public void RaiseShopTradeOpenRequested(string vendorId) => OnShopTradeOpenRequested?.Invoke(vendorId);
            public void RaiseShopTradeOpened(string vendorId) => OnShopTradeOpened?.Invoke(vendorId);
            public void RaiseShopTradeClosed() => OnShopTradeClosed?.Invoke();
            public void RaiseShopBuyRequested(string itemId, int quantity) => OnShopBuyRequested?.Invoke(itemId, quantity);
            public void RaiseShopSellRequested(string itemId, int quantity) => OnShopSellRequested?.Invoke(itemId, quantity);
            public void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request) => OnShopBuyCheckoutRequested?.Invoke(request);
            public void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request) => OnShopSellCheckoutRequested?.Invoke(request);

            public void RaiseShopTradeResult(ShopTradeResultPayload payload)
            {
                OnShopTradeResultReceived?.Invoke(payload);
            }
        }

        private sealed class FakeInventoryEvents : IInventoryEvents
        {
            public int InventoryChangedCount { get; private set; }
            public int MoneyChangedCount { get; private set; }
            public int LastMoneyChangedAmount { get; private set; }

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
            public void RaiseBeltSelectionChanged(int selectedBeltIndex) => OnBeltSelectionChanged?.Invoke(selectedBeltIndex);

            public void RaiseInventoryChanged()
            {
                InventoryChangedCount++;
                OnInventoryChanged?.Invoke();
            }

            public void RaiseMoneyChanged(int amount)
            {
                MoneyChangedCount++;
                LastMoneyChangedAmount = amount;
                OnMoneyChanged?.Invoke(amount);
            }
        }
    }
}
