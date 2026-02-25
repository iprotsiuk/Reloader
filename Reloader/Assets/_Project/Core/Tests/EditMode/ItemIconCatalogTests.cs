using NUnit.Framework;
using Reloader.Core.UI;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class ItemIconCatalogTests
    {
        [Test]
        public void TryGetIcon_KnownItemId_ReturnsSprite()
        {
            var catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
            var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f));

            catalog.SetEntriesForTests(new[]
            {
                new ItemIconCatalog.Entry("item-1", sprite)
            });

            var found = catalog.TryGetIcon("item-1", out var resolved);

            Assert.That(found, Is.True);
            Assert.That(resolved, Is.SameAs(sprite));

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void TryGetIcon_UnknownItemId_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();

            var found = catalog.TryGetIcon("missing-item", out var resolved);

            Assert.That(found, Is.False);
            Assert.That(resolved, Is.Null);

            Object.DestroyImmediate(catalog);
        }
    }
}
