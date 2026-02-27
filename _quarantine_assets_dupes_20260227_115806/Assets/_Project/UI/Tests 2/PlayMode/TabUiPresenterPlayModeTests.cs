using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.UI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Reloader.UI.Tests.PlayMode
{
    public class TabUiPresenterPlayModeTests
    {
        [Test]
        public void SetOpen_TogglesPanelRoot()
        {
            var (root, presenter, _, _, _, panelRoot, inventoryGo) = CreatePresenter();

            presenter.SetOpen(true);
            Assert.That(panelRoot.activeSelf, Is.True);

            presenter.SetOpen(false);
            Assert.That(panelRoot.activeSelf, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(inventoryGo);
        }

        [Test]
        public void TryDropDraggedItem_BackpackToBelt_MovesItem()
        {
            var (root, presenter, inventoryController, _, _, _, inventoryGo) = CreatePresenter();
            inventoryController.Runtime.SetBackpackCapacity(4);
            inventoryController.Runtime.BackpackItemIds.Add("pack-item");

            presenter.SetOpen(true);
            var began = presenter.TryBeginDrag(InventoryArea.Backpack, 0);
            var dropped = presenter.TryDropDraggedItem(InventoryArea.Belt, 1);
            presenter.EndDrag();

            Assert.That(began, Is.True);
            Assert.That(dropped, Is.True);
            Assert.That(inventoryController.Runtime.BeltSlotItemIds[1], Is.EqualTo("pack-item"));
            Assert.That(inventoryController.Runtime.BackpackItemIds.Count, Is.EqualTo(0));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(inventoryGo);
        }

        [Test]
        public void SetOpen_DisablesConfiguredLookOnly_AndRestoresOnClose()
        {
            var (root, presenter, _, _, _, _, inventoryGo) = CreatePresenter();
            var look = root.AddComponent<DummyLookBehaviour>();
            var move = root.AddComponent<DummyMoveBehaviour>();

            SetDisableBehaviours(presenter, new Behaviour[] { look });

            presenter.SetOpen(true);
            Assert.That(look.enabled, Is.False);
            Assert.That(move.enabled, Is.True);

            presenter.SetOpen(false);
            Assert.That(look.enabled, Is.True);
            Assert.That(move.enabled, Is.True);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(inventoryGo);
        }

        private static void SetDisableBehaviours(TabUiPresenter presenter, Behaviour[] behaviours)
        {
            var field = typeof(TabUiPresenter).GetField("_disableBehavioursWhileOpen", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(presenter, behaviours);
        }

        private static (GameObject root, TabUiPresenter presenter, PlayerInventoryController inventoryController, Image[] beltIcons, Image[] backpackIcons, GameObject panelRoot, GameObject inventoryGo) CreatePresenter()
        {
            var inventoryGo = new GameObject("InventoryController");
            var inventoryController = inventoryGo.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(null, null, new PlayerInventoryRuntime());

            var root = new GameObject("TabUiRoot");
            var presenter = root.AddComponent<TabUiPresenter>();

            var panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(root.transform, false);
            panelRoot.SetActive(false);

            const int backpackSlotCount = 8;
            var beltRoots = new RectTransform[PlayerInventoryRuntime.BeltSlotCount];
            var beltFrames = new Image[PlayerInventoryRuntime.BeltSlotCount];
            var beltIcons = new Image[PlayerInventoryRuntime.BeltSlotCount];
            var backpackRoots = new RectTransform[backpackSlotCount];
            var backpackFrames = new Image[backpackSlotCount];
            var backpackIcons = new Image[backpackSlotCount];

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var slotGo = new GameObject($"BeltSlot_{i}");
                slotGo.transform.SetParent(panelRoot.transform, false);
                beltRoots[i] = slotGo.AddComponent<RectTransform>();

                var frameGo = new GameObject($"BeltFrame_{i}");
                frameGo.transform.SetParent(slotGo.transform, false);
                beltFrames[i] = frameGo.AddComponent<Image>();

                var iconGo = new GameObject($"BeltIcon_{i}");
                iconGo.transform.SetParent(slotGo.transform, false);
                beltIcons[i] = iconGo.AddComponent<Image>();
            }

            for (var i = 0; i < backpackSlotCount; i++)
            {
                var slotGo = new GameObject($"BackpackSlot_{i}");
                slotGo.transform.SetParent(panelRoot.transform, false);
                backpackRoots[i] = slotGo.AddComponent<RectTransform>();

                var frameGo = new GameObject($"BackpackFrame_{i}");
                frameGo.transform.SetParent(slotGo.transform, false);
                backpackFrames[i] = frameGo.AddComponent<Image>();

                var iconGo = new GameObject($"BackpackIcon_{i}");
                iconGo.transform.SetParent(slotGo.transform, false);
                backpackIcons[i] = iconGo.AddComponent<Image>();
            }

            presenter.ConfigureGeneratedSlotViews(panelRoot, beltRoots, beltFrames, beltIcons, backpackRoots, backpackFrames, backpackIcons);
            presenter.SetInventoryController(inventoryController);

            return (root, presenter, inventoryController, beltIcons, backpackIcons, panelRoot, inventoryGo);
        }

        private sealed class DummyLookBehaviour : MonoBehaviour
        {
        }

        private sealed class DummyMoveBehaviour : MonoBehaviour
        {
        }
    }
}
