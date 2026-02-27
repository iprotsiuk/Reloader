using NUnit.Framework;
using TMPro;
using Reloader.Inventory;
using Reloader.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Reloader.UI.Tests.PlayMode
{
    public class BeltHudPresenterPlayModeTests
    {
        [Test]
        public void Refresh_SelectedSlot_AppliesSelectedScale()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SelectBeltSlot(2);
            inventoryController.Configure(null, null, runtime);

            var (presenterGo, presenter, slotRoots, _, _) = CreateHudPresenter();
            presenter.SetInventoryController(inventoryController);

            presenter.Refresh();

            Assert.That(slotRoots[2].localScale.x, Is.GreaterThan(1f));
            Assert.That(slotRoots[1].localScale.x, Is.EqualTo(1f).Within(0.001f));

            Object.DestroyImmediate(presenterGo);
            Object.DestroyImmediate(inventoryGo);
        }

        [Test]
        public void Refresh_OccupiedSlot_HidesPlaceholderIconByDefault()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.BeltSlotItemIds[0] = "item-1";
            inventoryController.Configure(null, null, runtime);

            var (presenterGo, presenter, _, slotIcons, _) = CreateHudPresenter();
            presenter.SetInventoryController(inventoryController);

            presenter.Refresh();

            Assert.That(slotIcons[0].enabled, Is.False);
            Assert.That(slotIcons[1].enabled, Is.False);

            Object.DestroyImmediate(presenterGo);
            Object.DestroyImmediate(inventoryGo);
        }

        private static (GameObject root, BeltHudPresenter presenter, RectTransform[] roots, Image[] slotIcons, TMP_Text[] labels) CreateHudPresenter()
        {
            var root = new GameObject("BeltHudPresenterRoot");
            var presenter = root.AddComponent<BeltHudPresenter>();
            var slotRoots = new RectTransform[PlayerInventoryRuntime.BeltSlotCount];
            var slotIcons = new Image[PlayerInventoryRuntime.BeltSlotCount];
            var labels = new TMP_Text[PlayerInventoryRuntime.BeltSlotCount];
            var slotFrames = new Image[PlayerInventoryRuntime.BeltSlotCount];

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var slotGo = new GameObject($"Slot_{i}");
                slotGo.transform.SetParent(root.transform, false);
                var slotRoot = slotGo.AddComponent<RectTransform>();
                slotRoots[i] = slotRoot;

                var frameGo = new GameObject($"Frame_{i}");
                frameGo.transform.SetParent(slotGo.transform, false);
                var frameImage = frameGo.AddComponent<Image>();
                slotFrames[i] = frameImage;

                var iconGo = new GameObject($"Icon_{i}");
                iconGo.transform.SetParent(slotGo.transform, false);
                var iconImage = iconGo.AddComponent<Image>();
                slotIcons[i] = iconImage;

                var labelGo = new GameObject($"Label_{i}");
                labelGo.transform.SetParent(slotGo.transform, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                labels[i] = label;
            }
            presenter.ConfigureGeneratedSlotViews(slotRoots, slotFrames, slotIcons, labels);

            return (root, presenter, slotRoots, slotIcons, labels);
        }
    }
}
