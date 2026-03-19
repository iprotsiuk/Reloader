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
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");

            Assert.That(viewmodelLayer, Is.GreaterThanOrEqualTo(0), "Expected project Viewmodel layer to exist.");

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            Assert.That(root.transform.Find("CameraPivot/ViewmodelCamera"), Is.Null);

            defaults.ApplyDefaults();

            var viewmodelTransform = cameraPivot.Find("ViewmodelCamera");
            Assert.That(viewmodelTransform, Is.Not.Null, "Expected PlayerCameraDefaults to create a ViewmodelCamera under CameraPivot when missing.");
            Assert.That(viewmodelTransform.parent, Is.EqualTo(cameraPivot));

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
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            defaults.ApplyDefaults();

            var viewmodelCamera = cameraPivot.Find("ViewmodelCamera")?.GetComponent<Camera>();
            Assert.That(viewmodelCamera, Is.Not.Null);

            var updated = defaults.TrySetEffectiveFieldOfView(37f);

            Assert.That(updated, Is.True);
            Assert.That(viewmodelCamera!.fieldOfView, Is.EqualTo(37f).Within(0.001f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDefaults_MigratesLegacyViewmodelCameraChildToCameraPivot_WhenPresent()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var legacyCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            legacyCamera.transform.SetParent(mainCamera.transform, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            defaults.ApplyDefaults();

            var migratedCamera = cameraPivot.Find("ViewmodelCamera")?.GetComponent<Camera>();

            Assert.That(migratedCamera, Is.SameAs(legacyCamera));
            Assert.That(legacyCamera.transform.parent, Is.EqualTo(cameraPivot));
            Assert.That(mainCamera.transform.Find("ViewmodelCamera"), Is.Null);

            Object.DestroyImmediate(root);
        }

        private static (GameObject Root, Transform CameraPivot, Camera MainCamera) CreateRigRoot()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var mainCameraGo = new GameObject("MainCamera");
            mainCameraGo.transform.SetParent(cameraPivot, false);
            var mainCamera = mainCameraGo.AddComponent<Camera>();
            return (root, cameraPivot, mainCamera);
        }
    }
}
