using System.Reflection;
using NUnit.Framework;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class TabInventoryResponsiveLayoutEditModeTests
    {
        [Test]
        public void ApplyResponsiveLayout_HidesDetailPane_WhenPanelWidthDropsBelowMinimum()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var panel = root.Q<VisualElement>("inventory__panel");
            var workspace = root.Q<VisualElement>("inventory__workspace");
            var detailPane = root.Q<VisualElement>("inventory__detail-pane");
            Assert.That(panel, Is.Not.Null);
            Assert.That(workspace, Is.Not.Null);
            Assert.That(detailPane, Is.Not.Null);

            detailPane.style.width = 220f;
            detailPane.style.minWidth = 184f;
            panel.style.width = 640f;

            InvokeApplyResponsiveLayout(binder);

            Assert.That(detailPane.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            panel.style.width = 320f;
            InvokeApplyResponsiveLayout(binder);

            Assert.That(detailPane.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(workspace.style.marginRight.value.value, Is.EqualTo(0f));
        }

        [Test]
        public void ApplyResponsiveLayout_ClampsIconRailTabsToCompactWidth_WhenTabBarStaysNarrow()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var tabBar = root.Q<VisualElement>("inventory__tabbar");
            var inventoryTab = root.Q<Button>("inventory__tab-inventory");
            Assert.That(tabBar, Is.Not.Null);
            Assert.That(inventoryTab, Is.Not.Null);

            tabBar.style.width = 72f;

            InvokeApplyResponsiveLayout(binder);

            Assert.That(inventoryTab.style.width.value.value, Is.EqualTo(60f));
            Assert.That(inventoryTab.style.fontSize.value.value, Is.EqualTo(0f));
            Assert.That(inventoryTab.style.height.value.value, Is.InRange(38f, 46f));
        }

        private static void InvokeApplyResponsiveLayout(TabInventoryViewBinder binder)
        {
            var method = typeof(TabInventoryViewBinder).GetMethod("ApplyResponsiveLayout", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method!.Invoke(binder, null);
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            var shell = new VisualElement { name = "inventory__shell" };
            var rail = new VisualElement { name = "inventory__rail" };
            var workspace = new VisualElement { name = "inventory__workspace" };
            var detailPane = new VisualElement { name = "inventory__detail-pane" };
            var tabBar = new VisualElement { name = "inventory__tabbar" };

            root.Add(panel);
            panel.Add(shell);
            shell.Add(rail);
            rail.Add(tabBar);
            shell.Add(workspace);
            shell.Add(detailPane);

            panel.Add(new Button { name = "inventory__tab-inventory", text = "Inventory" });
            panel.Add(new Button { name = "inventory__tab-quests", text = "Quests" });
            panel.Add(new Button { name = "inventory__tab-journal", text = "Journal" });
            panel.Add(new Button { name = "inventory__tab-calendar", text = "Calendar" });
            panel.Add(new Button { name = "inventory__tab-device", text = "Device" });

            workspace.Add(new VisualElement { name = "inventory__section-inventory" });
            workspace.Add(new VisualElement { name = "inventory__section-quests" });
            workspace.Add(new VisualElement { name = "inventory__section-journal" });
            workspace.Add(new VisualElement { name = "inventory__section-calendar" });
            workspace.Add(new VisualElement { name = "inventory__section-device" });
            workspace.Add(new VisualElement { name = "inventory__section-attachments" });
            workspace.Add(new VisualElement { name = "inventory__grid-area" });
            workspace.Add(new VisualElement { name = "inventory__backpack-grid" });
            workspace.Add(new VisualElement { name = "inventory__grid-row--belt" });
            workspace.Add(new VisualElement { name = "inventory__device-notes" });
            workspace.Add(new VisualElement { name = "inventory__device-session-history" });

            panel.Add(new Label { name = "inventory__device-selected-target-value" });
            panel.Add(new Label { name = "inventory__device-shot-count-value" });
            panel.Add(new Label { name = "inventory__device-spread-value" });
            panel.Add(new Label { name = "inventory__device-moa-value" });
            panel.Add(new Label { name = "inventory__device-saved-groups-value" });
            panel.Add(new Label { name = "inventory__device-install-feedback-text" });
            panel.Add(new Button { name = "inventory__device-choose-target" });
            panel.Add(new Button { name = "inventory__device-save-group" });
            panel.Add(new Button { name = "inventory__device-clear-group" });
            panel.Add(new Button { name = "inventory__device-install-hooks" });
            panel.Add(new Button { name = "inventory__device-uninstall-hooks" });

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            panel.Add(tooltip);

            return root;
        }
    }
}
