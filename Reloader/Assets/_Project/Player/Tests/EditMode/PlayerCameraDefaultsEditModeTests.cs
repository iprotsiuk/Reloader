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
                .GetField("_cameraPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
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
                .GetField("_cameraPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
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
        public void ApplyDefaults_CreatesViewmodelCameraUnderExplicitParent_AndLeavesLegacyWorldCameraChildUntouched()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var legacyCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            legacyCamera.transform.SetParent(mainCamera.transform, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            defaults.ApplyDefaults();

            var explicitCamera = cameraPivot.Find("ViewmodelCamera")?.GetComponent<Camera>();

            Assert.That(explicitCamera, Is.Not.Null);
            Assert.That(explicitCamera, Is.Not.SameAs(legacyCamera));
            Assert.That(legacyCamera.transform.parent, Is.EqualTo(mainCamera.transform));
            Assert.That(mainCamera.transform.Find("ViewmodelCamera"), Is.SameAs(legacyCamera.transform));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetCameraPivot_DoesNotRecoverFromCameraFollowTarget_WhenExplicitCameraPivotIsMissing()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            Assert.That(defaults.TryGetCameraPivot(out var resolvedPivot), Is.False);
            Assert.That(resolvedPivot, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetCameraPivot_DoesNotRecoverFromViewmodelCameraParent_WhenExplicitCameraPivotIsMissing()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            Assert.That(defaults.TryGetCameraPivot(out var resolvedPivot), Is.False);
            Assert.That(resolvedPivot, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetPlayerArmsRoot_DoesNotRecoverFromAnimatorParent_WhenExplicitPlayerArmsRootIsMissing()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            var animator = playerArmsVisual.AddComponent<Animator>();

            var defaults = root.AddComponent<PlayerCameraDefaults>();
            typeof(PlayerCameraDefaults)
                .GetField("_playerArmsAnimator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, animator);

            Assert.That(defaults.TryGetPlayerArmsRoot(out var resolvedPlayerArmsRoot), Is.False);
            Assert.That(resolvedPlayerArmsRoot, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetPlayerArmsAnimator_DoesNotRecoverFromPlayerArmsRoot_WhenExplicitAnimatorIsMissing()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            playerArmsVisual.AddComponent<Animator>();

            var defaults = root.AddComponent<PlayerCameraDefaults>();
            typeof(PlayerCameraDefaults)
                .GetField("_playerArmsRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, playerArmsRoot);

            Assert.That(defaults.TryGetPlayerArmsAnimator(out var resolvedPlayerArmsAnimator), Is.False);
            Assert.That(resolvedPlayerArmsAnimator, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetViewmodelCamera_DoesNotRecoverFromCameraPivot_WhenExplicitViewmodelCameraParentIsMissing()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var legacyViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            legacyViewmodelCamera.transform.SetParent(cameraPivot, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            Assert.That(defaults.TryGetViewmodelCamera(out var resolvedViewmodelCamera), Is.False);
            Assert.That(resolvedViewmodelCamera, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetViewmodelCamera_DoesNotReturnCachedCamera_WhenExplicitParentIsMissing()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var legacyViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            legacyViewmodelCamera.transform.SetParent(cameraPivot, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, legacyViewmodelCamera);

            Assert.That(defaults.TryGetViewmodelCamera(out var resolvedViewmodelCamera), Is.False);
            Assert.That(resolvedViewmodelCamera, Is.Null);
            Assert.That(
                typeof(PlayerCameraDefaults)
                    .GetField("_viewmodelCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(defaults),
                Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDefaults_UsesExplicitViewmodelCameraParent_WhenProvided()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var explicitParent = new GameObject("ExplicitViewmodelRoot").transform;
            explicitParent.SetParent(root.transform, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, explicitParent);

            defaults.ApplyDefaults();

            var viewmodelTransform = explicitParent.Find("ViewmodelCamera");
            Assert.That(viewmodelTransform, Is.Not.Null);
            Assert.That(viewmodelTransform.parent, Is.EqualTo(explicitParent));
            Assert.That(cameraPivot.Find("ViewmodelCamera"), Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ExplicitOwnershipContract_UsesSerializedRoots_AndDoesNotCreateLegacyPresentationFallback()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(root.transform, false);
            var weaponPresentationRoot = new GameObject("WeaponMount").transform;
            weaponPresentationRoot.SetParent(presentationPivot, false);
            var playerArmsRoot = new GameObject("ArmsBranch").transform;
            playerArmsRoot.SetParent(presentationPivot, false);
            var playerArmsVisual = new GameObject("ViewArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            var playerArmsAnimator = playerArmsVisual.AddComponent<Animator>();

            var defaults = root.AddComponent<PlayerCameraDefaults>();
            typeof(PlayerCameraDefaults)
                .GetField("_cameraPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, presentationPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, presentationPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, presentationPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_playerArmsRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, playerArmsRoot);
            typeof(PlayerCameraDefaults)
                .GetField("_playerArmsAnimator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, playerArmsAnimator);
            typeof(PlayerCameraDefaults)
                .GetField("_weaponPresentationRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, weaponPresentationRoot);

            Assert.That(defaults.TryGetCameraPivot(out var resolvedPivot), Is.True);
            Assert.That(defaults.TryGetPlayerArmsRoot(out var resolvedPlayerArmsRoot), Is.True);
            Assert.That(defaults.TryGetPlayerArmsAnimator(out var resolvedPlayerArmsAnimator), Is.True);
            Assert.That(defaults.TryGetWeaponPresentationRoot(out var resolvedWeaponPresentationRoot), Is.True);

            Assert.That(resolvedPivot, Is.SameAs(presentationPivot));
            Assert.That(resolvedPlayerArmsRoot, Is.SameAs(playerArmsRoot));
            Assert.That(resolvedPlayerArmsAnimator, Is.SameAs(playerArmsAnimator));
            Assert.That(resolvedWeaponPresentationRoot, Is.SameAs(weaponPresentationRoot));
            Assert.That(presentationPivot.Find("WeaponPresentationRoot"), Is.Null,
                "Explicit PlayerCameraDefaults ownership should not create the legacy CameraPivot/WeaponPresentationRoot fallback.");

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
