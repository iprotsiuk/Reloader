using NUnit.Framework;
using Reloader.Core.UI;
using Reloader.UI.Toolkit.ChestInventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class ChestInventoryUiToolkitPlayModeTests
    {
        [Test]
        public void Render_OccupiedSlots_RenderIconVisualsFromCatalog()
        {
            var root = BuildRoot();
            var binder = new ChestInventoryViewBinder();
            binder.Initialize(root, chestSlotCount: 1, playerSlotCount: 1);

            var catalogProviderGo = new GameObject("ItemIconCatalogProvider");
            var catalogProvider = catalogProviderGo.AddComponent<ItemIconCatalogProvider>();
            var catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
            var texture = new Texture2D(2, 2);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            catalog.SetEntriesForTests(new[] { new ItemIconCatalog.Entry("rifle_alpha", sprite) });
            catalogProvider.SetCatalogForTests(catalog);

            binder.Render(ChestInventoryUiState.Create(
                isOpen: true,
                chestSlots: new[] { new ChestInventoryUiState.SlotState(0, "rifle_alpha", true) },
                playerSlots: new[] { new ChestInventoryUiState.SlotState(0, "ammo_308", true) }));

            var chestIcon = root.Q<VisualElement>("chest__slot-container-0")?.Q<VisualElement>("chest__slot-item-icon-container-0");
            var backpackIcon = root.Q<VisualElement>("chest__slot-backpack-0")?.Q<VisualElement>("chest__slot-item-icon-backpack-0");

            Assert.That(chestIcon, Is.Not.Null);
            Assert.That(chestIcon?.ClassListContains("is-missing"), Is.False);
            Assert.That(backpackIcon, Is.Not.Null);
            Assert.That(backpackIcon?.ClassListContains("is-missing"), Is.True);

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(catalogProviderGo);
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement();
            var panel = new VisualElement { name = "chest__panel" };
            var chestGrid = new VisualElement { name = "chest__left-grid" };
            var playerGrid = new VisualElement { name = "chest__right-grid" };

            panel.Add(chestGrid);
            panel.Add(playerGrid);
            root.Add(panel);
            return root;
        }
    }
}
