using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.NPCs.World;
using Reloader.Player;
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

            string openedVendorId = null;
            void Handler(string vendorId) => openedVendorId = vendorId;
            GameEvents.OnShopTradeOpenRequested += Handler;
            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                GameEvents.OnShopTradeOpenRequested -= Handler;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(vendor.gameObject);
            }

            Assert.That(openedVendorId, Is.EqualTo("vendor-reloading-store"));
            Assert.That(vendor.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void PickupPress_WhenInventoryConsumesFirstAndHasNoTarget_StillOpensVendorTrade()
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

            var openRequestedCount = 0;
            void Handler(string _) => openRequestedCount++;
            GameEvents.OnShopTradeOpenRequested += Handler;
            try
            {
                input.PickupPressedThisFrame = true;
                inventoryController.Tick();
                vendorController.Tick();
            }
            finally
            {
                GameEvents.OnShopTradeOpenRequested -= Handler;
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

            var closeCount = 0;
            void ClosedHandler() => closeCount++;
            GameEvents.OnShopTradeClosed += ClosedHandler;
            try
            {
                GameEvents.RaiseShopTradeOpened("vendor-reloading-store");
                resolver.Target = null;
                controller.Tick();
            }
            finally
            {
                GameEvents.OnShopTradeClosed -= ClosedHandler;
                Object.DestroyImmediate(root);
            }

            Assert.That(closeCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_MissingDependencies_LogsErrorOnce_AndDoesNotOpenTrade()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerShopVendorController>();

            var openRequestedCount = 0;
            void Handler(string _) => openRequestedCount++;
            GameEvents.OnShopTradeOpenRequested += Handler;
            try
            {
                LogAssert.Expect(LogType.Error, "PlayerShopVendorController requires both input source and vendor resolver references.");
                controller.Tick();
                controller.Tick();
            }
            finally
            {
                GameEvents.OnShopTradeOpenRequested -= Handler;
                Object.DestroyImmediate(root);
            }

            Assert.That(openRequestedCount, Is.EqualTo(0));
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
    }
}
