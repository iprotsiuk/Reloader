using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class RenderTextureScopeControllerCompositeReticleTests
    {
        [Test]
        public void ResolveCompositeReticleDrawScale_NearSquareSprite_NormalizesToSquareScale()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null, "RenderTextureScopeController type should exist.");

            var method = controllerType!.GetMethod(
                "ResolveCompositeReticleDrawScale",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private composite-reticle draw-scale helper to exist.");

            var texture = new Texture2D(1855, 1858, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1855f, 1858f), new Vector2(0.5f, 0.5f), 100f);

            try
            {
                var drawScale = (Vector2)method!.Invoke(null, new object[] { sprite, 1.2f });
                Assert.That(drawScale.x, Is.EqualTo(drawScale.y).Within(0.0001f),
                    "Expected near-square PiP reticles to render with square draw scale so tiny source-image aspect error does not show up as scope distortion.");
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ResolveCompositeReticleDestination_SnapsDestinationToIntegerPixels()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null, "RenderTextureScopeController type should exist.");

            var method = controllerType!.GetMethod(
                "ResolveCompositeReticleDestination",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private composite-reticle destination helper to exist.");

            var destination = (Rect)method!.Invoke(null, new object[]
            {
                1024,
                new Vector2(1.2f, 1.2f),
                new Vector2(0.0022f, 0f)
            });

            Assert.That(destination.x, Is.EqualTo(Mathf.Round(destination.x)).Within(0.0001f));
            Assert.That(destination.y, Is.EqualTo(Mathf.Round(destination.y)).Within(0.0001f));
            Assert.That(destination.width, Is.EqualTo(Mathf.Round(destination.width)).Within(0.0001f));
            Assert.That(destination.height, Is.EqualTo(Mathf.Round(destination.height)).Within(0.0001f));
        }
    }
}
