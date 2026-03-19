using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PlayerWeaponControllerViewmodelCameraResolutionTests
    {
        [Test]
        public void ResolveViewmodelCamera_PrefersCameraPivotChild_WhenSharedBasisLayoutExists()
        {
            var rig = CreateRig(includeSharedBasisViewmodel: true, includeLegacyViewmodel: true);
            try
            {
                var resolved = ResolveViewmodelCamera(rig.WorldCamera);

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
        public void ResolveViewmodelCamera_FallsBackToWorldCameraChild_WhenSharedBasisLayoutIsAbsent()
        {
            var rig = CreateRig(includeSharedBasisViewmodel: false, includeLegacyViewmodel: true);
            try
            {
                var resolved = ResolveViewmodelCamera(rig.WorldCamera);

                Assert.That(resolved, Is.SameAs(rig.LegacyViewmodelCamera));
                Assert.That(resolved.transform.parent, Is.EqualTo(rig.WorldCamera.transform));
            }
            finally
            {
                Object.DestroyImmediate(rig.Root);
            }
        }

        private static Camera ResolveViewmodelCamera(Camera worldCamera)
        {
            var method = typeof(PlayerWeaponController).GetMethod(
                "ResolveViewmodelCamera",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected PlayerWeaponController private viewmodel-camera resolver to exist.");

            return (Camera)method!.Invoke(null, new object[] { worldCamera });
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
