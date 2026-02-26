using NUnit.Framework;
using Reloader.Core.UI;
using Reloader.UI.Toolkit.BeltHud;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class BeltHudUiToolkitPlayModeTests
    {
        [Test]
        public void Render_AppliesOccupiedAndSelectedClasses()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            var state = BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, "item-a", true, false),
                new BeltHudUiState.SlotState(1, null, false, true),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            });

            binder.Render(state);

            var slot0 = root.Q<VisualElement>("belt-hud__slot-0");
            var slot1 = root.Q<VisualElement>("belt-hud__slot-1");

            Assert.That(slot0.ClassListContains("is-occupied"), Is.True);
            Assert.That(slot0.ClassListContains("is-selected"), Is.False);
            Assert.That(slot1.ClassListContains("is-selected"), Is.True);
        }

        [Test]
        public void Render_OccupiedSlot_RendersInCellItemVisual()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            var catalogProviderGo = new GameObject("ItemIconCatalogProvider");
            var catalogProvider = catalogProviderGo.AddComponent<ItemIconCatalogProvider>();
            var catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
            var texture = new Texture2D(2, 2);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            catalog.SetEntriesForTests(new[] { new ItemIconCatalog.Entry("rifle_alpha", sprite) });
            catalogProvider.SetCatalogForTests(catalog);

            binder.Render(BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, "rifle_alpha", true, false, 17),
                new BeltHudUiState.SlotState(1, null, false, false),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            }));

            var slot0 = root.Q<VisualElement>("belt-hud__slot-0");
            var slot1 = root.Q<VisualElement>("belt-hud__slot-1");
            var itemLabel0 = slot0?.Q<Label>("belt-hud__slot-item-name-0");
            var itemLabel1 = slot1?.Q<Label>("belt-hud__slot-item-name-1");
            var quantity = slot0?.Q<Label>("belt-hud__slot-item-quantity-0");
            var icon = slot0?.Q<VisualElement>("belt-hud__slot-item-icon-0");

            Assert.That(itemLabel0, Is.Not.Null);
            Assert.That(itemLabel0?.text, Is.EqualTo("Rifle Alpha"));
            Assert.That(itemLabel1, Is.Null);
            Assert.That(quantity?.text, Is.EqualTo("17"));
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon?.ClassListContains("is-missing"), Is.False);
            Assert.That(root.Q<Label>("belt-hud__slot-label-0")?.text, Is.EqualTo("1"));

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(catalogProviderGo);
        }

        [Test]
        public void Render_UpdatesSelectionWhenStateChanges()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            binder.Render(BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, null, false, true),
                new BeltHudUiState.SlotState(1, null, false, false),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            }));

            binder.Render(BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, null, false, false),
                new BeltHudUiState.SlotState(1, null, false, true),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            }));

            var slot0 = root.Q<VisualElement>("belt-hud__slot-0");
            var slot1 = root.Q<VisualElement>("belt-hud__slot-1");

            Assert.That(slot0.ClassListContains("is-selected"), Is.False);
            Assert.That(slot1.ClassListContains("is-selected"), Is.True);
        }

        [Test]
        public void TryRaiseSlotSelectIntent_EmitsIntentWithSlotIndex()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            var captured = -1;
            binder.IntentRaised += intent =>
            {
                if (intent.Payload is int index)
                {
                    captured = index;
                }
            };

            var raised = binder.TryRaiseSlotSelectIntent(3);

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo(3));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "belt-hud__root" };
            for (var i = 0; i < 5; i++)
            {
                var slot = new VisualElement { name = $"belt-hud__slot-{i}" };
                var label = new Label((i + 1).ToString()) { name = $"belt-hud__slot-label-{i}" };
                slot.Add(label);
                root.Add(slot);
            }

            return root;
        }
    }
}
