using NUnit.Framework;
using Reloader.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerCameraDefaultsEditModeTests
    {
        [Test]
        public void ApplyDefaults_CreatesViewmodelCameraOverlayStack_WhenMissing()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var mainCamera = root.AddComponent<Camera>();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");

            Assert.That(viewmodelLayer, Is.GreaterThanOrEqualTo(0), "Expected project Viewmodel layer to exist.");

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);

            Assert.That(root.transform.Find("ViewmodelCamera"), Is.Null);

            defaults.ApplyDefaults();

            var viewmodelTransform = root.transform.Find("ViewmodelCamera");
            Assert.That(viewmodelTransform, Is.Not.Null, "Expected PlayerCameraDefaults to create a ViewmodelCamera child when missing.");

            var viewmodelCamera = viewmodelTransform.GetComponent<Camera>();
            var mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
            var viewmodelCameraData = viewmodelCamera.GetUniversalAdditionalCameraData();
            var viewmodelMask = 1 << viewmodelLayer;

            Assert.That(viewmodelCamera, Is.Not.Null);
            Assert.That(mainCameraData.renderType, Is.EqualTo(CameraRenderType.Base));
            Assert.That(viewmodelCameraData.renderType, Is.EqualTo(CameraRenderType.Overlay));
            Assert.That(viewmodelCamera.cullingMask, Is.EqualTo(viewmodelMask));
            Assert.That(mainCamera.cullingMask & viewmodelMask, Is.EqualTo(0));
            Assert.That(mainCameraData.cameraStack.Contains(viewmodelCamera), Is.True);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TrySetEffectiveFieldOfView_KeepsViewmodelCameraLensInSync()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var mainCamera = root.AddComponent<Camera>();
            var defaults = root.AddComponent<PlayerCameraDefaults>();

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);

            defaults.ApplyDefaults();

            var viewmodelCamera = root.transform.Find("ViewmodelCamera")?.GetComponent<Camera>();
            Assert.That(viewmodelCamera, Is.Not.Null);

            var updated = defaults.TrySetEffectiveFieldOfView(37f);

            Assert.That(updated, Is.True);
            Assert.That(viewmodelCamera!.fieldOfView, Is.EqualTo(37f).Within(0.001f));

            Object.DestroyImmediate(root);
        }
    }
}
