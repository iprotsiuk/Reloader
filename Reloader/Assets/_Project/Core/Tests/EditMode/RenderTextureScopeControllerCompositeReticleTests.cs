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

        [Test]
        public void TryResolveLensViewportRectNormalized_UsesAuthoredLensSurfaceBounds()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            var scopeLensDisplayType = System.Type.GetType("Reloader.Game.Weapons.ScopeLensDisplay, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null, "RenderTextureScopeController type should exist.");
            Assert.That(scopeLensDisplayType, Is.Not.Null, "ScopeLensDisplay type should exist.");

            var method = controllerType!.GetMethod(
                "TryResolveLensViewportRectNormalized",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected a private lens-aperture viewport helper for the scoped blur contract.");

            var cameraGo = new GameObject("ViewmodelCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.aspect = 1f;

            var lensGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lensGo.transform.position = Vector3.zero;
            lensGo.transform.localScale = new Vector3(0.6f, 0.4f, 1f);
            var renderer = lensGo.GetComponent<Renderer>();
            var lensDisplay = lensGo.AddComponent(scopeLensDisplayType!);
            var targetRendererField = scopeLensDisplayType.GetField("_targetRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(targetRendererField, Is.Not.Null);
            targetRendererField!.SetValue(lensDisplay, renderer);

            try
            {
                var args = new object[] { camera, lensDisplay, null };
                var resolved = (bool)method!.Invoke(null, args);
                Assert.That(resolved, Is.True, "Expected authored lens-display bounds to produce a normalized blur aperture.");

                var viewportRect = (Rect)args[2]!;
                Assert.That(viewportRect.center.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(viewportRect.center.y, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(viewportRect.width, Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(viewportRect.height, Is.EqualTo(0.2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(lensGo);
                Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void PeripheralScopeBlurRendererFeature_ShouldEnqueueForOverlayGameplayCamera()
        {
            var featureType = System.Type.GetType("Reloader.Game.Weapons.Rendering.PeripheralScopeBlurRendererFeature, Reloader.Game.Weapons");
            Assert.That(featureType, Is.Not.Null, "PeripheralScopeBlurRendererFeature type should exist.");

            var method = featureType!.GetMethod(
                "ShouldEnqueueForCamera",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private camera-filter helper on PeripheralScopeBlurRendererFeature.");

            var cameraGo = new GameObject("OverlayGameplayCamera");
            var camera = cameraGo.AddComponent<Camera>();

            try
            {
                var result = (bool)method!.Invoke(null, new object[] { camera, CameraType.Game, false });
                Assert.That(result, Is.True,
                    "Peripheral blur should run for overlay gameplay cameras too so the composed player view blurs outside the authored optic lens.");
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
            }
        }
    }
}
