using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.TabInventory;
using Reloader.Weapons.Data;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

namespace Reloader.UI.Tests.PlayMode
{
    public class TabInventoryAttachmentsPlayModeTests
    {
        [Test]
        public void Controller_AttachmentsIntent_OpensPanelAndFiltersOwnedCompatibleAttachments()
        {
            var owner = new GameObject("TabInventoryAttachmentsController");
            var inventoryController = owner.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(6);
            runtime.TryStoreItem("weapon-kar98k", out _, out var weaponSlot, out _);
            runtime.TryStoreItem("att-optic-4x", out _, out _, out _);
            runtime.TryStoreItem("att-muzzle-a", out _, out _, out _);
            inventoryController.Configure(null, null, runtime);

            var registryOwner = new GameObject("TabInventoryWeaponRegistry");
            var registry = registryOwner.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                itemId: "weapon-kar98k",
                displayName: "Rifle 01",
                magazineCapacity: 5,
                fireIntervalSeconds: 0.2f,
                projectileSpeed: 900f,
                projectileGravityMultiplier: 1f,
                baseDamage: 40f,
                maxRangeMeters: 400f);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x", "att-optic-8x" }),
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-a" })
            });
            registry.SetDefinitionsForTests(new[] { definition });

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 0);

            var inputSource = owner.AddComponent<TestInputSource>();
            var controller = owner.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent(
                "tab.inventory.item.context.attachments",
                new TabInventoryAttachmentContextIntentPayload("belt", weaponSlot, "weapon-kar98k")));

            var attachmentsSection = root.Q<VisualElement>("inventory__section-attachments");
            var slotDropdown = root.Q<DropdownField>("inventory__attachments-slot-dropdown");
            var attachmentDropdown = root.Q<DropdownField>("inventory__attachments-item-dropdown");
            var weaponLabel = root.Q<Label>("inventory__attachments-weapon-name");

            Assert.That(attachmentsSection.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(slotDropdown.choices, Is.EquivalentTo(new[] { "Scope", "Muzzle" }));
            Assert.That(slotDropdown.value, Is.EqualTo("Scope"));
            Assert.That(attachmentDropdown.choices, Is.EquivalentTo(new[] { "Remove", "att-optic-4x" }));
            Assert.That(weaponLabel.text, Is.EqualTo("Rifle 01"));

            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(registryOwner);
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void Controller_AttachmentsIntent_NonWeaponItem_DoesNotOpenAttachmentsSection()
        {
            var owner = new GameObject("TabInventoryAttachmentsNonWeapon");
            var inventoryController = owner.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            runtime.TryStoreItem("consumable-bandage", out _, out var slotIndex, out _);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 0);

            var inputSource = owner.AddComponent<TestInputSource>();
            var controller = owner.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent(
                "tab.inventory.item.context.attachments",
                new TabInventoryAttachmentContextIntentPayload("belt", slotIndex, "consumable-bandage")));

            var attachmentsSection = root.Q<VisualElement>("inventory__section-attachments");
            Assert.That(attachmentsSection.style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void Controller_AttachmentsIntent_FallsBackToAnotherWeaponRegistry_WhenPrimaryRegistryLacksDefinition()
        {
            var owner = new GameObject("TabInventoryAttachmentsRegistryFallbackController");
            var inventoryController = owner.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(8);
            runtime.BeltSlotItemIds[0] = "weapon-pistol-01";
            runtime.SelectBeltSlot(0);
            runtime.TryStoreItem("weapon-pistol-01", out _, out _, out _);
            runtime.TryStoreItem("att-pistol-optic", out _, out _, out _);
            inventoryController.Configure(null, null, runtime);

            var primaryRegistry = owner.AddComponent<WeaponRegistry>();
            var primaryDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();
            primaryDefinition.SetRuntimeValuesForTests(
                itemId: "weapon-kar98k",
                displayName: "Kar98k",
                magazineCapacity: 5,
                fireIntervalSeconds: 0.2f,
                projectileSpeed: 900f,
                projectileGravityMultiplier: 1f,
                baseDamage: 40f,
                maxRangeMeters: 400f);
            primaryRegistry.SetDefinitionsForTests(new[] { primaryDefinition });

            var fallbackRegistryOwner = new GameObject("TabInventoryAttachmentsRegistryFallback");
            var fallbackRegistry = fallbackRegistryOwner.AddComponent<WeaponRegistry>();
            var fallbackDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();
            fallbackDefinition.SetRuntimeValuesForTests(
                itemId: "weapon-pistol-01",
                displayName: "Pistol 01",
                magazineCapacity: 12,
                fireIntervalSeconds: 0.15f,
                projectileSpeed: 420f,
                projectileGravityMultiplier: 1f,
                baseDamage: 20f,
                maxRangeMeters: 80f);
            fallbackDefinition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-pistol-optic" })
            });
            fallbackRegistry.SetDefinitionsForTests(new[] { fallbackDefinition });

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 0);

            var inputSource = owner.AddComponent<TestInputSource>();
            var controller = owner.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(binder, new TabInventoryDragController());

            var resolveMethod = typeof(TabInventoryController).GetMethod(
                "TryResolveWeaponDefinition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);
            var args = new object[] { "weapon-pistol-01", null };
            var resolved = (bool)resolveMethod.Invoke(controller, args);
            Assert.That(resolved, Is.True, "Expected fallback registry lookup to resolve pistol definition.");

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent(
                "tab.inventory.item.context.attachments",
                new TabInventoryAttachmentContextIntentPayload("belt", 0, "weapon-pistol-01")));

            var resolvedDefinition = args[1] as WeaponDefinition;
            Assert.That(resolvedDefinition, Is.Not.Null);
            Assert.That(resolvedDefinition.ItemId, Is.EqualTo("weapon-pistol-01"));

            UnityEngine.Object.DestroyImmediate(primaryDefinition);
            UnityEngine.Object.DestroyImmediate(fallbackDefinition);
            UnityEngine.Object.DestroyImmediate(fallbackRegistryOwner);
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [UnityTest]
        public System.Collections.IEnumerator Controller_AttachmentsIntent_ShowsRemoveAndCanDetachEquippedAttachment()
        {
            var owner = new GameObject("TabInventoryAttachmentsRemoveFlow");
            var inventoryController = owner.AddComponent<PlayerInventoryController>();
            var inventoryInputSource = owner.AddComponent<TestInputSource>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(8);
            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryStoreItem("weapon-kar98k", out _, out _, out _);
            inventoryController.Configure(inventoryInputSource, null, runtime);

            var registryOwner = new GameObject("TabInventoryWeaponRegistryRemoveFlow");
            var registry = registryOwner.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                itemId: "weapon-kar98k",
                displayName: "Kar98k",
                magazineCapacity: 5,
                fireIntervalSeconds: 0.2f,
                projectileSpeed: 900f,
                projectileGravityMultiplier: 1f,
                baseDamage: 40f,
                maxRangeMeters: 400f);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" }),
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-a" })
            });
            registry.SetDefinitionsForTests(new[] { definition });

            var weaponController = owner.AddComponent<PlayerWeaponController>();
            yield return null;

            var seeded = weaponController.ApplyRuntimeState("weapon-kar98k", 2, 5, true);
            Assert.That(seeded, Is.True);
            var appliedAttachments = weaponController.ApplyRuntimeAttachments(
                "weapon-kar98k",
                new Dictionary<WeaponAttachmentSlotType, string>
                {
                    { WeaponAttachmentSlotType.Scope, "att-optic-4x" }
                });
            Assert.That(appliedAttachments, Is.True);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 0);

            var inputSource = owner.AddComponent<TestInputSource>();
            var controller = owner.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent(
                "tab.inventory.item.context.attachments",
                new TabInventoryAttachmentContextIntentPayload("belt", 0, "weapon-kar98k")));

            var slotDropdown = root.Q<DropdownField>("inventory__attachments-slot-dropdown");
            var attachmentDropdown = root.Q<DropdownField>("inventory__attachments-item-dropdown");
            var statusLabel = root.Q<Label>("inventory__attachments-status");

            Assert.That(slotDropdown.value, Is.EqualTo("Scope"));
            Assert.That(attachmentDropdown.choices, Does.Contain("Remove"));

            controller.HandleIntent(new UiIntent("tab.inventory.attachments.item-selected", "Remove"));
            controller.HandleIntent(new UiIntent("tab.inventory.attachments.apply"));

            Assert.That(statusLabel.text, Is.EqualTo("Attachment removed."));
            Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1), "Removed scope should return to inventory.");
            Assert.That(weaponController.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));

            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(registryOwner);
            UnityEngine.Object.DestroyImmediate(owner);
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            panel.Add(new VisualElement { name = "inventory__tabbar" });
            panel.Add(new Button { name = "inventory__tab-inventory" });
            panel.Add(new Button { name = "inventory__tab-quests" });
            panel.Add(new Button { name = "inventory__tab-journal" });
            panel.Add(new Button { name = "inventory__tab-calendar" });
            panel.Add(new Button { name = "inventory__tab-device" });

            panel.Add(new VisualElement { name = "inventory__section-inventory" });
            panel.Add(new VisualElement { name = "inventory__section-quests" });
            panel.Add(new VisualElement { name = "inventory__section-journal" });
            panel.Add(new VisualElement { name = "inventory__section-calendar" });
            panel.Add(new VisualElement { name = "inventory__section-device" });
            panel.Add(new VisualElement { name = "inventory__section-attachments" });

            panel.Add(new Label { name = "inventory__attachments-weapon-name" });
            panel.Add(new DropdownField("Slot", new List<string>(), 0) { name = "inventory__attachments-slot-dropdown" });
            panel.Add(new DropdownField("Attachment", new List<string>(), 0) { name = "inventory__attachments-item-dropdown" });
            panel.Add(new Button { name = "inventory__attachments-apply" });
            panel.Add(new Button { name = "inventory__attachments-back" });
            panel.Add(new Label { name = "inventory__attachments-status" });

            panel.Add(new VisualElement { name = "inventory__device-notes" });
            panel.Add(new Label { name = "inventory__device-selected-target-value" });
            panel.Add(new Label { name = "inventory__device-shot-count-value" });
            panel.Add(new Label { name = "inventory__device-spread-value" });
            panel.Add(new Label { name = "inventory__device-moa-value" });
            panel.Add(new Label { name = "inventory__device-saved-groups-value" });
            panel.Add(new Label { name = "inventory__device-install-feedback-text" });
            panel.Add(new VisualElement { name = "inventory__device-session-history" });
            panel.Add(new Button { name = "inventory__device-choose-target" });
            panel.Add(new Button { name = "inventory__device-save-group" });
            panel.Add(new Button { name = "inventory__device-clear-group" });
            panel.Add(new Button { name = "inventory__device-install-hooks" });
            panel.Add(new Button { name = "inventory__device-uninstall-hooks" });

            panel.Add(new VisualElement { name = "inventory__backpack-grid" });
            panel.Add(new VisualElement { name = "inventory__grid-row--belt" });
            for (var i = 0; i < 5; i++)
            {
                panel.Add(new VisualElement { name = $"inventory__belt-slot-{i}" });
            }

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            panel.Add(tooltip);

            return root;
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool MenuTogglePressedThisFrame { get; set; }
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed()
            {
                var result = MenuTogglePressedThisFrame;
                MenuTogglePressedThisFrame = false;
                return result;
            }
        }
    }
}
