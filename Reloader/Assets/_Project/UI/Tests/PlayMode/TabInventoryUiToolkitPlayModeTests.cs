using NUnit.Framework;
using Reloader.Core.UI;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class TabInventoryUiToolkitPlayModeTests
    {
        [Test]
        public void Render_UpdatesOpenStateAndSlotOccupancy()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            binder.Render(TabInventoryUiState.Create(
                isOpen: true,
                beltSlots: new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                backpackSlots: new[]
                {
                    new TabInventoryUiState.SlotState(0, null, false),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                tooltipTitle: "Item A",
                tooltipVisible: true));

            var panel = root.Q<VisualElement>("inventory__panel");
            var belt0 = root.Q<VisualElement>("inventory__belt-slot-0");

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(belt0.ClassListContains("is-occupied"), Is.True);

            binder.Render(TabInventoryUiState.Create(false, new TabInventoryUiState.SlotState[5], new TabInventoryUiState.SlotState[2], null, false));
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Render_OccupiedSlots_RenderInCellItemVisuals()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            var catalogProviderGo = new GameObject("ItemIconCatalogProvider");
            var catalogProvider = catalogProviderGo.AddComponent<ItemIconCatalogProvider>();
            var catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
            var texture = new Texture2D(2, 2);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            catalog.SetEntriesForTests(new[] { new ItemIconCatalog.Entry("rifle_alpha", sprite) });
            catalogProvider.SetCatalogForTests(catalog);

            binder.Render(TabInventoryUiState.Create(
                isOpen: true,
                beltSlots: new[]
                {
                    new TabInventoryUiState.SlotState(0, "rifle_alpha", true, 1),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                backpackSlots: new[]
                {
                    new TabInventoryUiState.SlotState(0, "ammo_308", true, 42),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                tooltipTitle: string.Empty,
                tooltipVisible: false));

            var beltLabel = root.Q<VisualElement>("inventory__belt-slot-0")?.Q<Label>("inventory__slot-item-name-belt-0");
            var emptyBeltLabel = root.Q<VisualElement>("inventory__belt-slot-1")?.Q<Label>("inventory__slot-item-name-belt-1");
            var backpackLabel = root.Q<VisualElement>("inventory__backpack-slot-0")?.Q<Label>("inventory__slot-item-name-backpack-0");
            var emptyBackpackLabel = root.Q<VisualElement>("inventory__backpack-slot-1")?.Q<Label>("inventory__slot-item-name-backpack-1");
            var beltIcon = root.Q<VisualElement>("inventory__belt-slot-0")?.Q<VisualElement>("inventory__slot-item-icon-belt-0");
            var backpackIcon = root.Q<VisualElement>("inventory__backpack-slot-0")?.Q<VisualElement>("inventory__slot-item-icon-backpack-0");
            var backpackQuantity = root.Q<VisualElement>("inventory__backpack-slot-0")?.Q<Label>("inventory__slot-item-quantity-backpack-0");

            Assert.That(beltLabel, Is.Null);
            Assert.That(backpackLabel, Is.Null);
            Assert.That(beltIcon?.ClassListContains("is-missing"), Is.False);
            Assert.That(backpackIcon?.ClassListContains("is-missing"), Is.False);
            Assert.That(backpackQuantity?.text, Is.EqualTo("42"));
            Assert.That(emptyBeltLabel, Is.Null);
            Assert.That(emptyBackpackLabel, Is.Null);

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(catalogProviderGo);
        }

        [Test]
        public void DragController_DropOnSameItem_EmitsMergeIntent()
        {
            var drag = new TabInventoryDragController();
            string captured = null;
            drag.IntentRaised += i => captured = i.Key;

            drag.BeginDrag("belt", 0, "item-a");
            var resolved = drag.TryDrop("backpack", 1, "item-a");

            Assert.That(resolved, Is.True);
            Assert.That(captured, Is.EqualTo("inventory.drag.merge"));
        }

        [Test]
        public void DragController_DropOnDifferentItem_EmitsSwapIntent()
        {
            var drag = new TabInventoryDragController();
            string captured = null;
            drag.IntentRaised += i => captured = i.Key;

            drag.BeginDrag("belt", 0, "item-a");
            var resolved = drag.TryDrop("backpack", 1, "item-b");

            Assert.That(resolved, Is.True);
            Assert.That(captured, Is.EqualTo("inventory.drag.swap"));
        }

        [Test]
        public void Render_UpdatesTooltipTextAndVisibility()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);

            binder.Render(TabInventoryUiState.Create(
                true,
                new TabInventoryUiState.SlotState[5],
                new TabInventoryUiState.SlotState[2],
                "Tooltip Title",
                true));

            var tooltip = root.Q<VisualElement>("inventory__tooltip");
            var title = root.Q<Label>("inventory__tooltip-title");

            Assert.That(tooltip.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(title.text, Is.EqualTo("Tooltip Title"));
        }

        [Test]
        public void SlotInteraction_EmitsDragSwapIntent()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);
            binder.Render(TabInventoryUiState.Create(
                true,
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-b", true),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                string.Empty,
                false));

            string capturedKey = null;
            TabInventoryDragController.DragIntentPayload? capturedPayload = null;
            binder.IntentRaised += intent =>
            {
                capturedKey = intent.Key;
                if (intent.Payload is TabInventoryDragController.DragIntentPayload payload)
                {
                    capturedPayload = payload;
                }
            };

            var started = binder.TryInteractSlotForTests("belt", 0);
            var dropped = binder.TryInteractSlotForTests("backpack", 0);

            Assert.That(started, Is.True);
            Assert.That(dropped, Is.True);
            Assert.That(capturedKey, Is.EqualTo("inventory.drag.swap"));
            Assert.That(capturedPayload.HasValue, Is.True);
            Assert.That(capturedPayload.Value.SourceContainer, Is.EqualTo("belt"));
            Assert.That(capturedPayload.Value.SourceIndex, Is.EqualTo(0));
            Assert.That(capturedPayload.Value.TargetContainer, Is.EqualTo("backpack"));
            Assert.That(capturedPayload.Value.TargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void PointerDrag_BeltToBackpack_EmitsSwapIntent()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);
            binder.Render(TabInventoryUiState.Create(
                true,
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-b", true),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                string.Empty,
                false));

            string capturedKey = null;
            TabInventoryDragController.DragIntentPayload? capturedPayload = null;
            binder.IntentRaised += intent =>
            {
                capturedKey = intent.Key;
                if (intent.Payload is TabInventoryDragController.DragIntentPayload payload)
                {
                    capturedPayload = payload;
                }
            };

            var started = binder.TryPointerDownForTests("belt", 0);
            var hovered = binder.TryPointerEnterForTests("backpack", 0);
            var dropped = binder.TryPointerUpForTests("backpack", 0);

            Assert.That(started, Is.True);
            Assert.That(hovered, Is.True);
            Assert.That(dropped, Is.True);
            Assert.That(capturedKey, Is.EqualTo("inventory.drag.swap"));
            Assert.That(capturedPayload.HasValue, Is.True);
            Assert.That(capturedPayload.Value.SourceContainer, Is.EqualTo("belt"));
            Assert.That(capturedPayload.Value.SourceIndex, Is.EqualTo(0));
            Assert.That(capturedPayload.Value.TargetContainer, Is.EqualTo("backpack"));
            Assert.That(capturedPayload.Value.TargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void PointerDrag_DropOnSameItem_EmitsMergeIntent()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);
            binder.Render(TabInventoryUiState.Create(
                true,
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                string.Empty,
                false));

            string capturedKey = null;
            binder.IntentRaised += intent => capturedKey = intent.Key;

            var started = binder.TryPointerDownForTests("belt", 0);
            var hovered = binder.TryPointerEnterForTests("backpack", 0);
            var dropped = binder.TryPointerUpForTests("backpack", 0);

            Assert.That(started, Is.True);
            Assert.That(hovered, Is.True);
            Assert.That(dropped, Is.True);
            Assert.That(capturedKey, Is.EqualTo("inventory.drag.merge"));
        }

        [Test]
        public void PointerDrag_SameContainerDrop_EmitsSwapIntent()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);
            binder.Render(TabInventoryUiState.Create(
                true,
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, "item-b", true),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                new[]
                {
                    new TabInventoryUiState.SlotState(0, null, false),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                string.Empty,
                false));

            string capturedKey = null;
            TabInventoryDragController.DragIntentPayload? capturedPayload = null;
            binder.IntentRaised += intent =>
            {
                capturedKey = intent.Key;
                if (intent.Payload is TabInventoryDragController.DragIntentPayload payload)
                {
                    capturedPayload = payload;
                }
            };

            var started = binder.TryPointerDownForTests("belt", 0);
            var hovered = binder.TryPointerEnterForTests("belt", 1);
            var dropped = binder.TryPointerUpForTests("belt", 1);

            Assert.That(started, Is.True);
            Assert.That(hovered, Is.True);
            Assert.That(dropped, Is.True);
            Assert.That(capturedKey, Is.EqualTo("inventory.drag.swap"));
            Assert.That(capturedPayload.HasValue, Is.True);
            Assert.That(capturedPayload.Value.SourceContainer, Is.EqualTo("belt"));
            Assert.That(capturedPayload.Value.SourceIndex, Is.EqualTo(0));
            Assert.That(capturedPayload.Value.TargetContainer, Is.EqualTo("belt"));
            Assert.That(capturedPayload.Value.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void PointerDrag_DropOutsideGrid_EmitsDropIntent()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);
            binder.Render(TabInventoryUiState.Create(
                true,
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "item-a", true),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                new[]
                {
                    new TabInventoryUiState.SlotState(0, null, false),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                string.Empty,
                false));

            string capturedKey = null;
            TabInventoryDragController.DragIntentPayload? capturedPayload = null;
            binder.IntentRaised += intent =>
            {
                capturedKey = intent.Key;
                if (intent.Payload is TabInventoryDragController.DragIntentPayload payload)
                {
                    capturedPayload = payload;
                }
            };

            var started = binder.TryPointerDownForTests("belt", 0);
            var droppedOutside = binder.TryPointerUpOutsideForTests();

            Assert.That(started, Is.True);
            Assert.That(droppedOutside, Is.True);
            Assert.That(capturedKey, Is.EqualTo("inventory.drag.drop"));
            Assert.That(capturedPayload.HasValue, Is.True);
            Assert.That(capturedPayload.Value.SourceContainer, Is.EqualTo("belt"));
            Assert.That(capturedPayload.Value.SourceIndex, Is.EqualTo(0));
            Assert.That(capturedPayload.Value.TargetIndex, Is.EqualTo(-1));
        }

        [Test]
        public void RightClickOccupiedSlot_EmitsAttachmentsContextIntent()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 5, backpackSlotCount: 2);
            binder.Render(TabInventoryUiState.Create(
                true,
                new[]
                {
                    new TabInventoryUiState.SlotState(0, "weapon-rifle-01", true),
                    new TabInventoryUiState.SlotState(1, null, false),
                    new TabInventoryUiState.SlotState(2, null, false),
                    new TabInventoryUiState.SlotState(3, null, false),
                    new TabInventoryUiState.SlotState(4, null, false)
                },
                new[]
                {
                    new TabInventoryUiState.SlotState(0, null, false),
                    new TabInventoryUiState.SlotState(1, null, false)
                },
                string.Empty,
                false));

            UiIntent captured = default;
            binder.IntentRaised += intent => captured = intent;

            var emitted = binder.TryRightClickSlotForTests("belt", 0);

            Assert.That(emitted, Is.True);
            Assert.That(captured.Key, Is.EqualTo("tab.inventory.item.context.attachments"));
            Assert.That(captured.Payload, Is.TypeOf<TabInventoryAttachmentContextIntentPayload>());
            var payload = (TabInventoryAttachmentContextIntentPayload)captured.Payload;
            Assert.That(payload.Container, Is.EqualTo("belt"));
            Assert.That(payload.SlotIndex, Is.EqualTo(0));
            Assert.That(payload.ItemId, Is.EqualTo("weapon-rifle-01"));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            for (var i = 0; i < 5; i++)
            {
                panel.Add(new VisualElement { name = $"inventory__belt-slot-{i}" });
            }

            for (var i = 0; i < 2; i++)
            {
                panel.Add(new VisualElement { name = $"inventory__backpack-slot-{i}" });
            }

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            panel.Add(tooltip);

            return root;
        }
    }
}
