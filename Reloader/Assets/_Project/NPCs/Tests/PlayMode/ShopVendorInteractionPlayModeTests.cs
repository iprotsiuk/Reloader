using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.NPCs.World;
using Reloader.Player;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class ShopVendorInteractionPlayModeTests
    {
        [Test]
        public void PickupPressOnVendor_RaisesOpenRequested()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            var resolver = root.AddComponent<TestVendorResolver>();
            controller.Configure(input, resolver);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            string openedVendorId = null;
            void Handler(string vendorId) => openedVendorId = vendorId;
            runtimeHub.OnShopTradeOpenRequested += Handler;
            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                runtimeHub.OnShopTradeOpenRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(openedVendorId, Is.EqualTo("vendor-reloading-store"));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void PickupPress_WhenInventoryHasNoTarget_AllowsVendorControllerToConsumeInSameFrame()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();

            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var emptyInventoryResolver = root.AddComponent<EmptyPickupResolver>();
            inventoryController.Configure(input, emptyInventoryResolver, new PlayerInventoryRuntime());

            var vendorController = root.AddComponent<PlayerShopVendorController>();
            var vendorResolver = root.AddComponent<TestVendorResolver>();
            vendorController.Configure(input, vendorResolver);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            vendorResolver.Target = vendor;

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var openRequestedCount = 0;
            void Handler(string _) => openRequestedCount++;
            runtimeHub.OnShopTradeOpenRequested += Handler;
            try
            {
                input.PickupPressedThisFrame = true;
                inventoryController.Tick();
                vendorController.Tick();
            }
            finally
            {
                runtimeHub.OnShopTradeOpenRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(openRequestedCount, Is.EqualTo(1));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void TradeOpen_WhenVendorTargetLost_RaisesTradeClosed()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            var resolver = root.AddComponent<TestVendorResolver>();
            controller.Configure(input, resolver);
            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var closeCount = 0;
            void ClosedHandler() => closeCount++;
            runtimeHub.OnShopTradeClosed += ClosedHandler;
            try
            {
                runtimeHub.RaiseShopTradeOpened("vendor-reloading-store");
                resolver.Target = null;
                controller.Tick();
            }
            finally
            {
                runtimeHub.OnShopTradeClosed -= ClosedHandler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(closeCount, Is.EqualTo(1));
        }

        [Test]
        public void Configure_InjectedShopEvents_UsesInjectedDependencyForTradeFlow()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            var resolver = root.AddComponent<TestVendorResolver>();
            var injectedEvents = new FakeShopEvents();
            controller.Configure(input, resolver, injectedEvents);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
                Assert.That(injectedEvents.TradeOpenRequestedCount, Is.EqualTo(1));
                Assert.That(injectedEvents.LastTradeOpenRequestedVendorId, Is.EqualTo("vendor-reloading-store"));

                injectedEvents.RaiseShopTradeOpened("vendor-reloading-store");
                resolver.Target = null;
                controller.Tick();
                Assert.That(injectedEvents.TradeClosedCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void Configure_WithoutInjectedShopEvents_RebindsInboundCallbacksImmediatelyWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            var resolver = root.AddComponent<TestVendorResolver>();
            controller.Configure(input, resolver);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            var closedCount = 0;
            replacementHub.OnShopTradeClosed += () => closedCount++;

            try
            {
                RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), replacementHub);
                replacementHub.RaiseShopTradeOpened("vendor-reloading-store");

                resolver.Target = null;
                controller.Tick();

                Assert.That(closedCount, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }
        }

        [Test]
        public void TradeOpened_ForDifferentVendorId_DoesNotBlockCurrentVendorOpenRequest()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            var resolver = root.AddComponent<TestVendorResolver>();
            var injectedEvents = new FakeShopEvents();
            controller.Configure(input, resolver, injectedEvents);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            try
            {
                injectedEvents.RaiseShopTradeOpened("vendor-other");
                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(injectedEvents.TradeOpenRequestedCount, Is.EqualTo(1));
            Assert.That(injectedEvents.LastTradeOpenRequestedVendorId, Is.EqualTo("vendor-reloading-store"));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void OnDisable_UnsubscribesFromShopEvents()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            var resolver = root.AddComponent<TestVendorResolver>();
            var injectedEvents = new FakeShopEvents();
            controller.Configure(input, resolver, injectedEvents);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            try
            {
                controller.enabled = false;
                injectedEvents.RaiseShopTradeOpened("vendor-reloading-store");

                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(injectedEvents.TradeOpenRequestedCount, Is.EqualTo(1));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void DisabledBeforeEnable_DoesNotProcessShopEventsUntilEnabled()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), runtimeEvents);

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerShopVendorController>();
            controller.enabled = false;
            var resolver = root.AddComponent<TestVendorResolver>();
            controller.Configure(input, resolver);

            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;
            var openRequestedCount = 0;
            runtimeEvents.OnShopTradeOpenRequested += _ => openRequestedCount++;

            try
            {
                InvokePrivate(controller, "ResolveReferences");
                runtimeEvents.RaiseShopTradeOpened("vendor-reloading-store");

                controller.enabled = true;
                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(openRequestedCount, Is.EqualTo(1));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_MissingDependencies_LogsErrorOnce_AndDoesNotOpenTrade()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerShopVendorController>();

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var openRequestedCount = 0;
            void Handler(string _) => openRequestedCount++;
            runtimeHub.OnShopTradeOpenRequested += Handler;
            try
            {
                LogAssert.Expect(LogType.Error, "PlayerShopVendorController requires both input source and vendor resolver references.");
                controller.Tick();
                controller.Tick();
            }
            finally
            {
                runtimeHub.OnShopTradeOpenRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }

            Assert.That(openRequestedCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_UnwiredDependenciesOnChildHierarchy_ResolvesAndOpensTrade()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerShopVendorController>();

            var child = new GameObject("InteractionRig");
            child.transform.SetParent(root.transform, false);
            var input = child.AddComponent<TestInputSource>();
            var resolver = child.AddComponent<TestVendorResolver>();
            var vendor = new GameObject("Vendor").AddComponent<ShopVendorTarget>();
            resolver.Target = vendor;

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            var openRequestedCount = 0;
            void Handler(string _) => openRequestedCount++;
            runtimeHub.OnShopTradeOpenRequested += Handler;
            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                runtimeHub.OnShopTradeOpenRequested -= Handler;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(openRequestedCount, Is.EqualTo(1));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void PlayerShopVendorResolver_WhenNonVendorColliderIsInFront_ResolvesVendorBehindIt()
        {
            var cameraGo = new GameObject("PlayerCamera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.transform.position = Vector3.zero;
            cameraGo.transform.rotation = Quaternion.identity;

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Blocker";
            blocker.transform.position = new Vector3(0f, 0f, 1.5f);
            blocker.transform.localScale = new Vector3(2f, 2f, 0.2f);

            var vendorRoot = new GameObject("VendorRoot");
            vendorRoot.AddComponent<ShopVendorTarget>();
            var vendorBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vendorBody.name = "VendorBody";
            vendorBody.transform.SetParent(vendorRoot.transform, false);
            vendorRoot.transform.position = new Vector3(0f, 0f, 2.5f);

            var resolverGo = new GameObject("Resolver");
            var resolver = resolverGo.AddComponent<PlayerShopVendorResolver>();
            resolver.SetCameraForTests(camera);

            try
            {
                var resolved = resolver.TryResolveVendorTarget(out var target);

                Assert.That(resolved, Is.True);
                Assert.That(target, Is.Not.Null);
                Assert.That(target.VendorId, Is.EqualTo("vendor-reloading-store"));
            }
            finally
            {
                Object.DestroyImmediate(resolverGo);
                Object.DestroyImmediate(vendorRoot);
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(cameraGo);
            }
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressedThisFrame;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }
        }

        private sealed class TestVendorResolver : MonoBehaviour, IPlayerShopVendorResolver
        {
            public IShopVendorTarget Target { get; set; }

            public bool TryResolveVendorTarget(out IShopVendorTarget target)
            {
                target = Target;
                return target != null;
            }
        }

        private sealed class EmptyPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = null;
                return false;
            }
        }

        private sealed class FakeShopEvents : IShopEvents
        {
            public int TradeOpenRequestedCount { get; private set; }
            public int TradeClosedCount { get; private set; }
            public string LastTradeOpenRequestedVendorId { get; private set; }

            public event System.Action<string> OnShopTradeOpenRequested;
            public event System.Action<string> OnShopTradeOpened;
            public event System.Action OnShopTradeClosed;
            public event System.Action<string, int> OnShopBuyRequested;
            public event System.Action<string, int> OnShopSellRequested;
            public event System.Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested;
            public event System.Action<ShopCheckoutRequest> OnShopSellCheckoutRequested;
            public event System.Action<string, int, bool, bool, string> OnShopTradeResult;

            public void RaiseShopTradeOpenRequested(string vendorId)
            {
                TradeOpenRequestedCount++;
                LastTradeOpenRequestedVendorId = vendorId;
                OnShopTradeOpenRequested?.Invoke(vendorId);
            }

            public void RaiseShopTradeOpened(string vendorId) => OnShopTradeOpened?.Invoke(vendorId);

            public void RaiseShopTradeClosed()
            {
                TradeClosedCount++;
                OnShopTradeClosed?.Invoke();
            }

            public void RaiseShopBuyRequested(string itemId, int quantity) => OnShopBuyRequested?.Invoke(itemId, quantity);
            public void RaiseShopSellRequested(string itemId, int quantity) => OnShopSellRequested?.Invoke(itemId, quantity);
            public void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request) => OnShopBuyCheckoutRequested?.Invoke(request);
            public void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request) => OnShopSellCheckoutRequested?.Invoke(request);
            public void RaiseShopTradeResult(string itemId, int quantity, bool isBuy, bool success, string failureReason)
                => OnShopTradeResult?.Invoke(itemId, quantity, isBuy, success, failureReason);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
            method.Invoke(target, null);
        }
    }
}
