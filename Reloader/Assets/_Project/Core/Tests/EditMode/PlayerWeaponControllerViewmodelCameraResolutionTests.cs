using System.Reflection;
using NUnit.Framework;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PlayerWeaponControllerViewmodelCameraResolutionTests
    {
        [Test]
        public void ResolveRuntimeViewmodelCamera_UsesExplicitPlayerCameraDefaultsViewmodelCamera()
        {
            var rig = CreateRig(includeSharedBasisViewmodel: true, includeLegacyViewmodel: true);
            try
            {
                var defaults = rig.Root.AddComponent<PlayerCameraDefaults>();
                SetField(defaults, "_mainCamera", rig.WorldCamera);
                SetField(defaults, "_cameraPivot", rig.CameraPivot);
                SetField(defaults, "_viewmodelCameraParent", rig.CameraPivot);

                var controller = rig.Root.AddComponent<PlayerWeaponController>();
                SetField(controller, "_cameraDefaults", defaults);

                var resolved = ResolveRuntimeViewmodelCamera(controller, rig.WorldCamera);

                Assert.That(resolved, Is.SameAs(rig.SharedBasisViewmodelCamera));
                Assert.That(resolved, Is.Not.SameAs(rig.LegacyViewmodelCamera));
                Assert.That(resolved.transform.parent, Is.EqualTo(rig.CameraPivot));
            }
            finally
            {
                Object.DestroyImmediate(rig.Root);
            }
        }

        [Test]
        public void ResolveRuntimeViewmodelCamera_WithoutExplicitPlayerCameraDefaultsContract_DoesNotFallBackToLegacyWorldCameraChild()
        {
            var rig = CreateRig(includeSharedBasisViewmodel: false, includeLegacyViewmodel: true);
            try
            {
                var controller = rig.Root.AddComponent<PlayerWeaponController>();
                var resolved = ResolveRuntimeViewmodelCamera(controller, rig.WorldCamera);

                Assert.That(resolved, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(rig.Root);
            }
        }

        [Test]
        public void ResolveAdsCamera_DoesNotRecoverFromCameraMain_WhenExplicitMainCameraIsMissing()
        {
            var rigRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(rigRoot.transform, false);

            var mainCameraGo = new GameObject("MainCamera");
            mainCameraGo.tag = "MainCamera";
            mainCameraGo.transform.SetParent(cameraPivot, false);
            mainCameraGo.AddComponent<Camera>();

            var controller = rigRoot.AddComponent<PlayerWeaponController>();

            try
            {
                var resolved = ResolveAdsCamera(controller);

                Assert.That(resolved, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(rigRoot);
            }
        }

        private static Camera ResolveRuntimeViewmodelCamera(PlayerWeaponController controller, Camera worldCamera)
        {
            var method = typeof(PlayerWeaponController).GetMethod("ResolveRuntimeViewmodelCamera", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected PlayerWeaponController private runtime viewmodel-camera resolver to exist.");

            return (Camera)method!.Invoke(controller, new object[] { worldCamera });
        }

        private static Camera ResolveAdsCamera(PlayerWeaponController controller)
        {
            var method = typeof(PlayerWeaponController).GetMethod("ResolveAdsCamera", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected PlayerWeaponController private ADS camera resolver to exist.");

            return (Camera)method!.Invoke(controller, null);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
        }

        private static TestRig CreateRig(bool includeSharedBasisViewmodel, bool includeLegacyViewmodel)
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var worldCameraGo = new GameObject("WorldCamera");
            worldCameraGo.transform.SetParent(cameraPivot, false);
            var worldCamera = worldCameraGo.AddComponent<Camera>();

            Camera sharedBasisViewmodelCamera = null;
            if (includeSharedBasisViewmodel)
            {
                var sharedBasisGo = new GameObject("ViewmodelCamera");
                sharedBasisGo.transform.SetParent(cameraPivot, false);
                sharedBasisViewmodelCamera = sharedBasisGo.AddComponent<Camera>();
            }

            Camera legacyViewmodelCamera = null;
            if (includeLegacyViewmodel)
            {
                var legacyGo = new GameObject("ViewmodelCamera");
                legacyGo.transform.SetParent(worldCamera.transform, false);
                legacyViewmodelCamera = legacyGo.AddComponent<Camera>();
            }

            return new TestRig(root, cameraPivot, worldCamera, sharedBasisViewmodelCamera, legacyViewmodelCamera);
        }

        private readonly struct TestRig
        {
            public TestRig(GameObject root, Transform cameraPivot, Camera worldCamera, Camera sharedBasisViewmodelCamera, Camera legacyViewmodelCamera)
            {
                Root = root;
                CameraPivot = cameraPivot;
                WorldCamera = worldCamera;
                SharedBasisViewmodelCamera = sharedBasisViewmodelCamera;
                LegacyViewmodelCamera = legacyViewmodelCamera;
            }

            public GameObject Root { get; }
            public Transform CameraPivot { get; }
            public Camera WorldCamera { get; }
            public Camera SharedBasisViewmodelCamera { get; }
            public Camera LegacyViewmodelCamera { get; }
        }
    }
}
