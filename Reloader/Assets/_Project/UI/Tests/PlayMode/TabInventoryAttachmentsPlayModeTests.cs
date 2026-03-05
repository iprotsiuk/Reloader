using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI;
using Reloader.UI.Toolkit.TabInventory;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

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
            runtime.TryStoreItem("weapon-rifle-01", out _, out var weaponSlot, out _);
            runtime.TryStoreItem("att-optic-4x", out _, out _, out _);
            runtime.TryStoreItem("att-muzzle-a", out _, out _, out _);
            inventoryController.Configure(null, null, runtime);

            var registryOwner = new GameObject("TabInventoryWeaponRegistry");
            var registry = registryOwner.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                itemId: "weapon-rifle-01",
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
                new TabInventoryAttachmentContextIntentPayload("belt", weaponSlot, "weapon-rifle-01")));

            var attachmentsSection = root.Q<VisualElement>("inventory__section-attachments");
            var slotDropdown = root.Q<DropdownField>("inventory__attachments-slot-dropdown");
            var attachmentDropdown = root.Q<DropdownField>("inventory__attachments-item-dropdown");
            var weaponLabel = root.Q<Label>("inventory__attachments-weapon-name");

            Assert.That(attachmentsSection.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(slotDropdown.choices, Is.EquivalentTo(new[] { "Scope", "Muzzle" }));
            Assert.That(slotDropdown.value, Is.EqualTo("Scope"));
            Assert.That(attachmentDropdown.choices, Is.EquivalentTo(new[] { "att-optic-4x" }));
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

            var inventorySection = root.Q<VisualElement>("inventory__section-inventory");
            var attachmentsSection = root.Q<VisualElement>("inventory__section-attachments");
            Assert.That(inventorySection.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(attachmentsSection.style.display.value, Is.EqualTo(DisplayStyle.None));

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
