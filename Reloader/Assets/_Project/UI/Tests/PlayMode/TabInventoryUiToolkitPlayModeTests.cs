using NUnit.Framework;
using Reloader.UI.Toolkit.TabInventory;
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
