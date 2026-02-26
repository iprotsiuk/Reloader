using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Items;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Player.Tests.PlayMode
{
    public class PlayerInventoryControllerPlayModeTests
    {
        [Test]
        public void Configure_InjectedInventoryEvents_RaisesAndHandlesEventsThroughInjectedDependency()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var target = new TestPickupTarget("item-77");
            resolver.Target = target;

            var injectedEvents = new FakeInventoryEvents();
            controller.Configure(input, resolver, new PlayerInventoryRuntime(), injectedEvents);

            input.BeltSlotPressed = 2;
            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(injectedEvents.BeltSelectionChangedCount, Is.EqualTo(1));
            Assert.That(injectedEvents.PickupRequestedCount, Is.EqualTo(1));
            Assert.That(injectedEvents.ItemStoredCount, Is.EqualTo(1));
            Assert.That(injectedEvents.InventoryChangedCount, Is.EqualTo(1));
            Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("item-77"));
            Assert.That(target.PickedUpCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Awake_DefaultBackpackCapacity_IsNineSlots()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();

            Assert.That(controller.Runtime, Is.Not.Null);
            Assert.That(controller.Runtime.BackpackCapacity, Is.EqualTo(9));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Configure_WithoutInjectedInventoryEvents_UsesRuntimeKernelInventoryEvents()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var target = new TestPickupTarget("item-default-fallback");
            resolver.Target = target;

            var beltSelectionChangedCount = 0;
            var itemStoredCount = 0;
            runtimeHub.OnBeltSelectionChanged += _ => beltSelectionChangedCount++;
            runtimeHub.OnItemStored += (_, _, _) => itemStoredCount++;

            try
            {
                controller.Configure(input, resolver, new PlayerInventoryRuntime());

                input.BeltSlotPressed = 1;
                input.PickupPressedThisFrame = true;
                controller.Tick();

                Assert.That(beltSelectionChangedCount, Is.EqualTo(1));
                Assert.That(itemStoredCount, Is.EqualTo(1));
                Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("item-default-fallback"));
                Assert.That(target.PickedUpCount, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Configure_WithoutInjectedInventoryEvents_RebindsWhenRuntimeKernelHubIsReplaced()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var target = new TestPickupTarget("item-hub-swap");
            resolver.Target = target;

            var initialHubPickupRequestedCount = 0;
            var replacementHubPickupRequestedCount = 0;
            initialHub.OnItemPickupRequested += _ => initialHubPickupRequestedCount++;
            replacementHub.OnItemPickupRequested += _ => replacementHubPickupRequestedCount++;

            try
            {
                controller.Configure(input, resolver, new PlayerInventoryRuntime());

                RuntimeKernelBootstrapper.Events = replacementHub;
                input.PickupPressedThisFrame = true;
                controller.Tick();

                Assert.That(initialHubPickupRequestedCount, Is.EqualTo(0));
                Assert.That(replacementHubPickupRequestedCount, Is.EqualTo(1));
                Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("item-hub-swap"));
                Assert.That(target.PickedUpCount, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Tick_BeltKeyPress_UpdatesSelectedSlot()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            var selectedIndex = -1;
            void HandleSelection(int index) => selectedIndex = index;
            GameEvents.OnBeltSelectionChanged += HandleSelection;
            try
            {
                input.BeltSlotPressed = 2;
                controller.Tick();
            }
            finally
            {
                GameEvents.OnBeltSelectionChanged -= HandleSelection;
                Object.DestroyImmediate(root);
            }

            Assert.That(controller.Runtime.SelectedBeltIndex, Is.EqualTo(2));
            Assert.That(selectedIndex, Is.EqualTo(2));
            Assert.That(controller.SelectedBeltIndexDebug, Is.EqualTo(2));
        }

        [Test]
        public void Tick_PickupPress_StoresResolvedItemAndMarksTargetPicked()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            var target = new TestPickupTarget("item-42");
            resolver.Target = target;

            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("item-42"));
            Assert.That(target.PickedUpCount, Is.EqualTo(1));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Tick_PickupPressWithoutTarget_ConsumesInputAndDoesNotAutoPickupWhenTargetAppearsLater()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            input.PickupPressedThisFrame = true;
            controller.Tick();

            var target = new TestPickupTarget("item-99");
            resolver.Target = target;
            controller.Tick();

            Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.Null);
            Assert.That(target.PickedUpCount, Is.EqualTo(0));

            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("item-99"));
            Assert.That(target.PickedUpCount, Is.EqualTo(1));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Tick_PickupPress_StoresStackPickupQuantity()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            var target = new TestStackPickupTarget("ammo-factory-308-147-fmj", 100);
            resolver.Target = target;

            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(controller.Runtime.GetItemQuantity("ammo-factory-308-147-fmj"), Is.EqualTo(100));
            Assert.That(target.PickedUpCount, Is.EqualTo(1));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Tick_PickupPress_DefinitionPickupTarget_UsesDefinitionItemIdAndQuantity()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            var itemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDefinition.SetValuesForTests(
                "powder-varget",
                ItemCategory.Powder,
                "Hodgdon Varget",
                ItemStackPolicy.StackByDefinition,
                maxStack: 500);
            var spawnDefinition = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
            spawnDefinition.SetValuesForTests(itemDefinition, quantity: 120);

            var target = new TestDefinitionPickupTarget(spawnDefinition);
            resolver.Target = target;

            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("powder-varget"));
            Assert.That(controller.Runtime.GetItemQuantity("powder-varget"), Is.EqualTo(120));
            Assert.That(target.PickedUpCount, Is.EqualTo(1));

            Object.DestroyImmediate(spawnDefinition);
            Object.DestroyImmediate(itemDefinition);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void InputSource_ExposesFireAndReloadConsumeMethods()
        {
            var root = new GameObject("InputSourceRoot");
            var input = root.AddComponent<TestInputSource>();
            IPlayerInputSource source = input;

            input.FirePressedThisFrame = true;
            input.ReloadPressedThisFrame = true;

            Assert.That(source.ConsumeFirePressed(), Is.True);
            Assert.That(source.ConsumeReloadPressed(), Is.True);
            Assert.That(source.ConsumeFirePressed(), Is.False);
            Assert.That(source.ConsumeReloadPressed(), Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void InputSource_ExposesAimHeldProperty()
        {
            var root = new GameObject("InputSourceRoot");
            var input = root.AddComponent<TestInputSource>();
            IPlayerInputSource source = input;
            input.AimHeldValue = true;

            Assert.That(source.AimHeld, Is.True);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Tick_MissingInputSource_LogsErrorOnce_AndReturns()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(null, resolver, new PlayerInventoryRuntime());

            LogAssert.Expect(LogType.Error, "PlayerInventoryController requires an IPlayerInputSource reference.");
            controller.Tick();
            controller.Tick();

            Object.DestroyImmediate(root);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressedThisFrame;
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public bool AimHeldValue;
            public int BeltSlotPressed = -1;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => AimHeldValue;
            public bool ConsumeJumpPressed() => false;

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }

            public int ConsumeBeltSelectPressed()
            {
                var pressed = BeltSlotPressed;
                BeltSlotPressed = -1;
                return pressed;
            }

            public bool ConsumeMenuTogglePressed()
            {
                return false;
            }

            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

            public bool ConsumeFirePressed()
            {
                if (!FirePressedThisFrame)
                {
                    return false;
                }

                FirePressedThisFrame = false;
                return true;
            }

            public bool ConsumeReloadPressed()
            {
                if (!ReloadPressedThisFrame)
                {
                    return false;
                }

                ReloadPressedThisFrame = false;
                return true;
            }
        }

        private sealed class TestPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public IInventoryPickupTarget Target { get; set; }

            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = Target;
                return target != null;
            }
        }

        private sealed class TestPickupTarget : IInventoryPickupTarget
        {
            public TestPickupTarget(string itemId)
            {
                ItemId = itemId;
            }

            public string ItemId { get; }
            public int PickedUpCount { get; private set; }

            public void OnPickedUp()
            {
                PickedUpCount++;
            }
        }

        private sealed class TestStackPickupTarget : IInventoryStackPickupTarget
        {
            public TestStackPickupTarget(string itemId, int quantity)
            {
                ItemId = itemId;
                Quantity = quantity;
            }

            public string ItemId { get; }
            public int Quantity { get; }
            public int PickedUpCount { get; private set; }

            public void OnPickedUp()
            {
                PickedUpCount++;
            }
        }

        private sealed class TestDefinitionPickupTarget : IInventoryDefinitionPickupTarget, IInventoryStackPickupTarget
        {
            public TestDefinitionPickupTarget(ItemSpawnDefinition spawnDefinition)
            {
                SpawnDefinition = spawnDefinition;
            }

            public string ItemId => SpawnDefinition != null ? SpawnDefinition.ItemId : null;
            public ItemSpawnDefinition SpawnDefinition { get; }
            public int Quantity => SpawnDefinition != null ? SpawnDefinition.Quantity : 1;
            public int PickedUpCount { get; private set; }

            public void OnPickedUp()
            {
                PickedUpCount++;
            }
        }

        private sealed class FakeInventoryEvents : IInventoryEvents
        {
            public int PickupRequestedCount { get; private set; }
            public int ItemStoredCount { get; private set; }
            public int BeltSelectionChangedCount { get; private set; }
            public int InventoryChangedCount { get; private set; }

            public event System.Action OnSaveStarted;
            public event System.Action OnSaveCompleted;
            public event System.Action OnLoadStarted;
            public event System.Action OnLoadCompleted;
            public event System.Action<string> OnItemPickupRequested;
            public event System.Action<string, InventoryArea, int> OnItemStored;
            public event System.Action<string, PickupRejectReason> OnItemPickupRejected;
            public event System.Action<int> OnBeltSelectionChanged;
            public event System.Action OnInventoryChanged;
            public event System.Action<int> OnMoneyChanged;

            public void RaiseSaveStarted() => OnSaveStarted?.Invoke();
            public void RaiseSaveCompleted() => OnSaveCompleted?.Invoke();
            public void RaiseLoadStarted() => OnLoadStarted?.Invoke();
            public void RaiseLoadCompleted() => OnLoadCompleted?.Invoke();

            public void RaiseItemPickupRequested(string itemId)
            {
                PickupRequestedCount++;
                OnItemPickupRequested?.Invoke(itemId);
            }

            public void RaiseItemStored(string itemId, InventoryArea area, int index)
            {
                ItemStoredCount++;
                OnItemStored?.Invoke(itemId, area, index);
            }

            public void RaiseItemPickupRejected(string itemId, PickupRejectReason reason)
            {
                OnItemPickupRejected?.Invoke(itemId, reason);
            }

            public void RaiseBeltSelectionChanged(int selectedBeltIndex)
            {
                BeltSelectionChangedCount++;
                OnBeltSelectionChanged?.Invoke(selectedBeltIndex);
            }

            public void RaiseInventoryChanged()
            {
                InventoryChangedCount++;
                OnInventoryChanged?.Invoke();
            }

            public void RaiseMoneyChanged(int amount) => OnMoneyChanged?.Invoke(amount);
        }
    }
}
