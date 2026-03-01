using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Items;
using Reloader.Core.Persistence;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using System.Reflection;
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
        public void Configure_WithoutInjectedInventoryEvents_RebindsInboundCallbacksImmediatelyWhenRuntimeKernelHubIsReconfigured()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();

            try
            {
                controller.Configure(input, resolver, new PlayerInventoryRuntime());

                RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), replacementHub);
                replacementHub.RaiseItemPickupRequested("item-inbound-after-swap");

                Assert.That(controller.Runtime.GetItemQuantity("item-inbound-after-swap"), Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DisabledBeforeOnEnable_DoesNotProcessRuntimePickupEventsUntilEnabled()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InventoryControllerRoot");
            root.SetActive(false);
            var controller = root.AddComponent<PlayerInventoryController>();
            controller.Configure(null, null, new PlayerInventoryRuntime());

            try
            {
                runtimeHub.RaiseItemPickupRequested("item-before-enable");
                Assert.That(controller.Runtime.GetItemQuantity("item-before-enable"), Is.EqualTo(0));

                root.SetActive(true);
                runtimeHub.RaiseItemPickupRequested("item-after-enable");
                Assert.That(controller.Runtime.GetItemQuantity("item-after-enable"), Is.EqualTo(1));
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
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            var selectedIndex = -1;
            void HandleSelection(int index) => selectedIndex = index;
            runtimeHub.OnBeltSelectionChanged += HandleSelection;
            try
            {
                input.BeltSlotPressed = 2;
                controller.Tick();
            }
            finally
            {
                runtimeHub.OnBeltSelectionChanged -= HandleSelection;
                RuntimeKernelBootstrapper.Events = originalHub;
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
        public void Tick_PickupPress_WorldIdentityTarget_MarksConsumedInStateStore()
        {
            GameObject root = null;
            GameObject pickupGo = null;

            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            try
            {
                root = new GameObject("InventoryControllerRoot");
                var controller = root.AddComponent<PlayerInventoryController>();
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                controller.Configure(input, resolver, new PlayerInventoryRuntime());

                pickupGo = new GameObject("WorldPickupTarget");
                var identity = pickupGo.AddComponent<WorldObjectIdentity>();
                SetObjectIdForTests(identity, "qa.pickup.consume");
                var target = pickupGo.AddComponent<TestWorldPickupTarget>();
                target.SetItemIdForTests("item-world-01");
                resolver.Target = target;

                input.PickupPressedThisFrame = true;
                controller.Tick();

                var scenePath = pickupGo.scene.path;
                var objectId = identity.ObjectId;
                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, objectId, out var record), Is.True);
                Assert.That(record.Consumed, Is.True);
                Assert.That(target.PickedUpCount, Is.EqualTo(1));
            }
            finally
            {
                if (pickupGo != null)
                {
                    Object.DestroyImmediate(pickupGo);
                }

                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                WorldObjectPersistenceRuntimeBridge.ResetForTests();
            }
        }

        [Test]
        public void Tick_PickupPress_WorldIdentityTarget_StoresConsumedUsingScenePathPlusObjectIdKey()
        {
            GameObject root = null;
            GameObject pickupGo = null;

            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            try
            {
                root = new GameObject("InventoryControllerRoot");
                var controller = root.AddComponent<PlayerInventoryController>();
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                controller.Configure(input, resolver, new PlayerInventoryRuntime());

                pickupGo = new GameObject("WorldPickupTarget");
                var identity = pickupGo.AddComponent<WorldObjectIdentity>();
                SetObjectIdForTests(identity, "qa.pickup.keyed");
                var target = pickupGo.AddComponent<TestWorldPickupTarget>();
                target.SetItemIdForTests("item-world-02");
                resolver.Target = target;

                input.PickupPressedThisFrame = true;
                controller.Tick();

                var scenePath = pickupGo.scene.path;
                var objectId = identity.ObjectId;
                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, objectId, out var record), Is.True);
                Assert.That(record.Consumed, Is.True);
                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath + ".alt", objectId, out _), Is.False);
                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, objectId + ".alt", out _), Is.False);
            }
            finally
            {
                if (pickupGo != null)
                {
                    Object.DestroyImmediate(pickupGo);
                }

                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                WorldObjectPersistenceRuntimeBridge.ResetForTests();
            }
        }

        [Test]
        public void Tick_PickupPress_WhenInventoryRejects_DoesNotMarkConsumedInStateStore()
        {
            GameObject root = null;
            GameObject pickupGo = null;

            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            try
            {
                root = new GameObject("InventoryControllerRoot");
                var controller = root.AddComponent<PlayerInventoryController>();
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                controller.Configure(input, resolver, new PlayerInventoryRuntime());

                for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount + controller.Runtime.BackpackCapacity; i++)
                {
                    var preloaded = controller.Runtime.TryStoreItem($"preload-item-{i}", out _, out _, out _);
                    Assert.That(preloaded, Is.True, $"Expected preload index {i} to fill available inventory slots.");
                }

                pickupGo = new GameObject("WorldPickupTarget");
                var identity = pickupGo.AddComponent<WorldObjectIdentity>();
                SetObjectIdForTests(identity, "qa.pickup.reject");
                var target = pickupGo.AddComponent<TestWorldPickupTarget>();
                target.SetItemIdForTests("item-rejected");
                resolver.Target = target;

                input.PickupPressedThisFrame = true;
                controller.Tick();

                var scenePath = pickupGo.scene.path;
                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, identity.ObjectId, out _), Is.False);
                Assert.That(target.PickedUpCount, Is.EqualTo(0));
            }
            finally
            {
                if (pickupGo != null)
                {
                    Object.DestroyImmediate(pickupGo);
                }

                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                WorldObjectPersistenceRuntimeBridge.ResetForTests();
            }
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
            controller.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

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
        public void TryConsumeSelectedBeltItem_WithDuplicateItemIds_ConsumesSelectedBeltSlot()
        {
            var root = new GameObject("InventoryControllerConsumeSelectedSlot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            controller.Configure(null, null, runtime);
            runtime.SetBackpackCapacity(0);

            Assert.That(runtime.TryStoreItem("attachment.rangefinder", out _, out var firstRangefinderIndex, out _), Is.True);
            Assert.That(runtime.TryStoreItem("filler-1", out _, out _, out _), Is.True);
            Assert.That(runtime.TryStoreItem("filler-2", out _, out _, out _), Is.True);
            Assert.That(runtime.TryStoreItem("attachment.rangefinder", out _, out var secondRangefinderIndex, out _), Is.True);
            Assert.That(firstRangefinderIndex, Is.EqualTo(0));
            Assert.That(secondRangefinderIndex, Is.EqualTo(3));

            runtime.SelectBeltSlot(secondRangefinderIndex);

            var consumed = controller.TryConsumeSelectedBeltItem(out var consumedItemId);

            Assert.That(consumed, Is.True);
            Assert.That(consumedItemId, Is.EqualTo("attachment.rangefinder"));
            Assert.That(runtime.BeltSlotItemIds[firstRangefinderIndex], Is.EqualTo("attachment.rangefinder"));
            Assert.That(runtime.BeltSlotItemIds[secondRangefinderIndex], Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void CanStoreItemWithBeltPriority_WhenOnlyStackMergeSpaceExists_ReturnsFalse()
        {
            var root = new GameObject("InventoryControllerCanStoreEmptySlotOnly");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            controller.Configure(null, null, runtime);
            runtime.SetBackpackCapacity(0);

            runtime.SetItemMaxStack("stack-item", 10);
            Assert.That(runtime.TryAddStackItem("stack-item", 1, out _, out _, out _), Is.True);
            Assert.That(runtime.TryStoreItem("filler-1", out _, out _, out _), Is.True);
            Assert.That(runtime.TryStoreItem("filler-2", out _, out _, out _), Is.True);
            Assert.That(runtime.TryStoreItem("filler-3", out _, out _, out _), Is.True);
            Assert.That(runtime.TryStoreItem("filler-4", out _, out _, out _), Is.True);
            Assert.That(runtime.BeltSlotItemIds, Has.None.Null);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                Assert.That(string.IsNullOrWhiteSpace(runtime.BeltSlotItemIds[i]), Is.False);
            }
            Assert.That(ReferenceEquals(controller.Runtime, runtime), Is.True);
            Assert.That(runtime.BackpackCapacity, Is.EqualTo(0));
            Assert.That(runtime.CanAcceptStackItem("stack-item"), Is.True);
            Assert.That(runtime.CanStoreItem("stack-item"), Is.False);

            Assert.That(controller.CanStoreItemWithBeltPriority("stack-item"), Is.False);

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
        public void Tick_TargetedPickup_EmitsHintWithActionAndDisplayName()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            var itemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDefinition.SetValuesForTests("powder-varget", ItemCategory.Powder, "Hodgdon Varget");
            var spawnDefinition = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
            spawnDefinition.SetValuesForTests(itemDefinition, quantity: 1);
            resolver.Target = new TestDefinitionPickupTarget(spawnDefinition);

            InteractionHintPayload hinted = default;
            runtimeHub.OnInteractionHintShown += payload => hinted = payload;

            try
            {
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(spawnDefinition);
                Object.DestroyImmediate(itemDefinition);
                Object.DestroyImmediate(root);
            }

            Assert.That(hinted.ContextId, Is.EqualTo("pickup"));
            Assert.That(hinted.ActionText, Is.EqualTo("Pick up"));
            Assert.That(hinted.SubjectText, Is.EqualTo("Hodgdon Varget"));
        }

        [Test]
        public void Tick_NoPickupTargetAfterHint_ClearsInteractionHint()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            resolver.Target = new TestPickupTarget("ammo-factory-308-147-fmj");

            var clearCount = 0;
            runtimeHub.OnInteractionHintCleared += () => clearCount++;

            try
            {
                controller.Tick();
                resolver.Target = null;
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }

            Assert.That(clearCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(runtimeHub.HasInteractionHint, Is.False);
        }

        [Test]
        public void Tick_PickupWithoutDefinitionDisplayName_UsesFormattedItemIdInHint()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            controller.Configure(input, resolver, new PlayerInventoryRuntime());

            resolver.Target = new TestPickupTarget("ammo-factory-308-147-fmj");

            InteractionHintPayload hinted = default;
            runtimeHub.OnInteractionHintShown += payload => hinted = payload;

            try
            {
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }

            Assert.That(hinted.SubjectText, Is.EqualTo("Ammo Factory 308 147 Fmj"));
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

        [Test]
        public void Tick_InputSourceBoundAfterFirstFailure_RecoversAndProcessesPickup()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var target = new TestPickupTarget("item-late-bind");
            resolver.Target = target;
            controller.Configure(null, resolver, new PlayerInventoryRuntime());

            LogAssert.Expect(LogType.Error, "PlayerInventoryController requires an IPlayerInputSource reference.");
            controller.Tick();

            var input = root.AddComponent<TestInputSource>();
            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(controller.Runtime.BeltSlotItemIds[0], Is.EqualTo("item-late-bind"));
            Assert.That(target.PickedUpCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryDropSelectedBeltItem_WhenNoSelection_DropsFirstOccupiedBeltSlot()
        {
            var root = new GameObject("InventoryControllerDropFallback");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            controller.Configure(null, null, runtime);

            var itemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDefinition.SetValuesForTests("item-drop-fallback", ItemCategory.Tool, "Drop Fallback");
            SetPrivateField(
                typeof(PlayerInventoryController),
                controller,
                "_itemDefinitionRegistry",
                new System.Collections.Generic.List<ItemDefinition> { itemDefinition });

            runtime.SetBackpackCapacity(0);
            Assert.That(runtime.TryStoreItem("item-drop-fallback", out var storedArea, out var storedIndex, out _), Is.True);
            Assert.That(storedArea, Is.EqualTo(InventoryArea.Belt));
            Assert.That(storedIndex, Is.EqualTo(0));
            Assert.That(runtime.SelectedBeltIndex, Is.EqualTo(-1));

            var dropped = controller.TryDropSelectedBeltItem();

            Assert.That(dropped, Is.True);
            Assert.That(runtime.BeltSlotItemIds[0], Is.Null);
            var dropRoot = GameObject.Find("drop-item-drop-fallback");
            Assert.That(dropRoot, Is.Not.Null);
            Assert.That(dropRoot.GetComponent<Rigidbody>(), Is.Not.Null);
            Assert.That(dropRoot.GetComponent<DefinitionPickupTarget>(), Is.Not.Null);

            if (dropRoot != null)
            {
                Object.DestroyImmediate(dropRoot);
            }

            Object.DestroyImmediate(itemDefinition);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryDropItemFromSlot_WhenDefinitionRegistryMissing_StillDropsPickupablePhysicsProp()
        {
            var root = new GameObject("InventoryControllerDropWithoutDefinition");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            controller.Configure(null, null, runtime);

            runtime.SetBackpackCapacity(0);
            Assert.That(runtime.TryStoreItem("item-drop-missing-def", out _, out var slotIndex, out _), Is.True);

            var dropped = controller.TryDropItemFromSlot(InventoryArea.Belt, slotIndex);

            Assert.That(dropped, Is.True);
            Assert.That(runtime.BeltSlotItemIds[slotIndex], Is.Null);

            var dropRoot = GameObject.Find("drop-item-drop-missing-def");
            Assert.That(dropRoot, Is.Not.Null);
            Assert.That(dropRoot.GetComponent<Rigidbody>(), Is.Not.Null);

            var pickupBehaviours = dropRoot.GetComponents<MonoBehaviour>();
            var hasStackPickupTarget = false;
            for (var i = 0; i < pickupBehaviours.Length; i++)
            {
                if (pickupBehaviours[i] is IInventoryStackPickupTarget)
                {
                    hasStackPickupTarget = true;
                    break;
                }
            }

            Assert.That(hasStackPickupTarget, Is.True);

            if (dropRoot != null)
            {
                Object.DestroyImmediate(dropRoot);
            }

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryDropItemFromSlot_WhenRegistryMissesButDefinitionIsLoaded_UsesDefinitionVisualDropPath()
        {
            var root = new GameObject("InventoryControllerDropGlobalDefinition");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            controller.Configure(null, null, runtime);

            var iconTemplate = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            iconTemplate.name = "DropIconTemplate";
            var itemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDefinition.SetValuesForTests(
                "item-drop-global-def",
                ItemCategory.Weapon,
                "Global Def Item",
                ItemStackPolicy.NonStackable,
                1,
                iconTemplate);

            runtime.SetBackpackCapacity(0);
            Assert.That(runtime.TryStoreItem("item-drop-global-def", out _, out var slotIndex, out _), Is.True);

            var dropped = controller.TryDropItemFromSlot(InventoryArea.Belt, slotIndex);

            Assert.That(dropped, Is.True);

            var dropRoot = GameObject.Find("drop-item-drop-global-def");
            Assert.That(dropRoot, Is.Not.Null);
            Assert.That(dropRoot.GetComponent<DefinitionPickupTarget>(), Is.Not.Null);
            Assert.That(dropRoot.GetComponent<RuntimeStackPickupTarget>(), Is.Null);

            if (dropRoot != null)
            {
                Object.DestroyImmediate(dropRoot);
            }

            Object.DestroyImmediate(itemDefinition);
            Object.DestroyImmediate(iconTemplate);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryDropItemFromSlot_RecordsSpawnedWorldObjectState_ForPersistence()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();

            var root = new GameObject("InventoryControllerDropPersistence");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            controller.Configure(null, null, runtime);

            runtime.SetBackpackCapacity(0);
            Assert.That(runtime.TryStoreItem("item-drop-persistent", out _, out var slotIndex, out _), Is.True);

            var dropped = controller.TryDropItemFromSlot(InventoryArea.Belt, slotIndex);

            Assert.That(dropped, Is.True);

            var dropRoot = GameObject.Find("drop-item-drop-persistent");
            Assert.That(dropRoot, Is.Not.Null);
            var identity = dropRoot.GetComponent<WorldObjectIdentity>();
            Assert.That(identity, Is.Not.Null);

            var scenePath = dropRoot.scene.path;
            Assert.That(string.IsNullOrWhiteSpace(scenePath), Is.False);
            Assert.That(
                WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, identity.ObjectId, out var record),
                Is.True,
                "Dropped runtime world objects should be persisted so they can be restored after travel.");
            Assert.That(record.HasTransformOverride, Is.True);
            Assert.That(record.ItemInstanceId, Is.EqualTo("item-drop-persistent"));

            if (dropRoot != null)
            {
                Object.DestroyImmediate(dropRoot);
            }

            Object.DestroyImmediate(root);
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
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

        private sealed class TestWorldPickupTarget : MonoBehaviour, IInventoryPickupTarget
        {
            [SerializeField] private string _itemId;

            public string ItemId => _itemId;
            public string ObjectId => GetComponent<WorldObjectIdentity>().ObjectId;
            public int PickedUpCount { get; private set; }

            public void SetItemIdForTests(string itemId)
            {
                _itemId = itemId;
            }

            public void OnPickedUp()
            {
                PickedUpCount++;
                gameObject.SetActive(false);
            }
        }

        private static void SetObjectIdForTests(WorldObjectIdentity identity, string objectId)
        {
            var objectIdField = typeof(WorldObjectIdentity).GetField("_objectId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(objectIdField, Is.Not.Null, "Expected private _objectId field on WorldObjectIdentity.");
            objectIdField.SetValue(identity, objectId);
            Assert.That(identity.ObjectId, Is.EqualTo(objectId));
        }

        private static void SetPrivateField(System.Type ownerType, object instance, string fieldName, object value)
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {ownerType.Name}.");
            field.SetValue(instance, value);
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
