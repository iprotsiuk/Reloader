using NUnit.Framework;
using Reloader.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerCameraDefaultsEditModeTests
    {
        [Test]
        public void ApplyDefaults_DoesNotRecoverViewmodelCameraFromChildName_WhenExplicitViewmodelCameraMissing()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            var legacyViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            legacyViewmodelCamera.transform.SetParent(cameraPivot, false);

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

            var originalCullingMask = mainCamera.cullingMask;

            defaults.ApplyDefaults();

            Assert.That(defaults.TryGetViewmodelCamera(out var resolvedViewmodelCamera), Is.False);
            Assert.That(resolvedViewmodelCamera, Is.Null);

            var viewmodelTransform = cameraPivot.Find("ViewmodelCamera");
            Assert.That(viewmodelTransform, Is.SameAs(legacyViewmodelCamera.transform),
                "Expected PlayerCameraDefaults to leave the legacy ViewmodelCamera child untouched when the explicit field is missing.");

            var mainCameraData = mainCamera.GetUniversalAdditionalCameraData();

            Assert.That(mainCameraData.renderType, Is.EqualTo(CameraRenderType.Base));
            Assert.That(root.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(2),
                "Expected PlayerCameraDefaults to avoid creating a replacement ViewmodelCamera when the explicit field is missing.");
            Assert.That(mainCamera.cullingMask, Is.EqualTo(originalCullingMask));

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
            var authoredViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            authoredViewmodelCamera.transform.SetParent(cameraPivot, false);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, authoredViewmodelCamera);

            defaults.ApplyDefaults();

            var viewmodelCamera = cameraPivot.Find("ViewmodelCamera")?.GetComponent<Camera>();
            Assert.That(viewmodelCamera, Is.SameAs(authoredViewmodelCamera));

            var updated = defaults.TrySetEffectiveFieldOfView(37f);

            Assert.That(updated, Is.True);
            Assert.That(viewmodelCamera!.fieldOfView, Is.EqualTo(37f).Within(0.001f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDefaults_PushesAuthoredClipPlanesIntoCinemachineLens()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var cinemachineCameraType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            Assert.That(cinemachineCameraType, Is.Not.Null);
            var cinemachineCamera = new GameObject("CinemachineCamera").AddComponent(cinemachineCameraType);
            cinemachineCamera.transform.SetParent(cameraPivot, false);
            var authoredViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            authoredViewmodelCamera.transform.SetParent(cameraPivot, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cinemachineCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cinemachineCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, authoredViewmodelCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            defaults.ApplyDefaults();

            var lens = cinemachineCameraType!.GetProperty("Lens")!.GetValue(cinemachineCamera);
            Assert.That((float)lens!.GetType().GetProperty("NearClipPlane")!.GetValue(lens), Is.EqualTo(0.001f).Within(0.0001f));
            Assert.That((float)lens.GetType().GetProperty("FarClipPlane")!.GetValue(lens), Is.EqualTo(2828f).Within(0.001f));
            Assert.That(authoredViewmodelCamera.nearClipPlane, Is.EqualTo(0.001f).Within(0.0001f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDefaults_DoesNotRecoverViewmodelCameraFromLegacyWorldCameraChild_WhenExplicitFieldMissing()
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

            Assert.That(explicitCamera, Is.Null);
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
                Is.SameAs(legacyViewmodelCamera));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetViewmodelCamera_DoesNotReparentCachedCamera_WhenExplicitParentMismatchExists()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var explicitParent = new GameObject("ExplicitViewmodelRoot").transform;
            explicitParent.SetParent(root.transform, false);
            var cachedViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            cachedViewmodelCamera.transform.SetParent(cameraPivot, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, explicitParent);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cachedViewmodelCamera);

            Assert.That(defaults.TryGetViewmodelCamera(out var resolvedViewmodelCamera), Is.False);
            Assert.That(resolvedViewmodelCamera, Is.Null);
            Assert.That(cachedViewmodelCamera.transform.parent, Is.EqualTo(cameraPivot),
                "Expected the cached viewmodel camera to stay put when the explicit parent mismatches.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetAuthoredMainCamera_DoesNotRecoverFromCameraMain_WhenExplicitMainCameraIsMissing()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var mainCamera = new GameObject("MainCamera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.transform.SetParent(cameraPivot, false);

            var defaults = root.AddComponent<PlayerCameraDefaults>();

            Assert.That(defaults.TryGetAuthoredMainCamera(out var resolvedMainCamera), Is.False);
            Assert.That(resolvedMainCamera, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetMainCamera_DoesNotRecoverFromCameraMain_WhenExplicitMainCameraIsMissing()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var mainCamera = new GameObject("MainCamera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.transform.SetParent(cameraPivot, false);

            var defaults = root.AddComponent<PlayerCameraDefaults>();

            Assert.That(defaults.TryGetMainCamera(out var resolvedMainCamera), Is.False);
            Assert.That(resolvedMainCamera, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetCameraLookTarget_DoesNotRecoverFromCameraFollowTarget_WhenExplicitCameraLookTargetIsMissing()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);

            Assert.That(defaults.TryGetCameraLookTarget(out var resolvedCameraLookTarget), Is.False);
            Assert.That(resolvedCameraLookTarget, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDefaults_UsesExistingViewmodelCameraUnderExplicitParent_WhenProvided()
        {
            var (root, cameraPivot, mainCamera) = CreateRigRoot();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var explicitParent = new GameObject("ExplicitViewmodelRoot").transform;
            explicitParent.SetParent(root.transform, false);
            var authoredViewmodelCamera = new GameObject("ViewmodelCamera").AddComponent<Camera>();
            authoredViewmodelCamera.transform.SetParent(explicitParent, false);

            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
            typeof(PlayerCameraDefaults)
                .GetField("_cameraFollowTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, cameraPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCameraParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, explicitParent);
            typeof(PlayerCameraDefaults)
                .GetField("_viewmodelCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, authoredViewmodelCamera);

            defaults.ApplyDefaults();

            var viewmodelTransform = explicitParent.Find("ViewmodelCamera");
            Assert.That(viewmodelTransform, Is.SameAs(authoredViewmodelCamera.transform));
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
            var mainCameraGo = new GameObject("MainCamera");
            mainCameraGo.transform.SetParent(root.transform, false);
            var mainCamera = mainCameraGo.AddComponent<Camera>();
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
                .GetField("_cameraLookTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, presentationPivot);
            typeof(PlayerCameraDefaults)
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, mainCamera);
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
            Assert.That(defaults.TryGetAuthoredMainCamera(out var resolvedMainCamera), Is.True);
            Assert.That(defaults.TryGetCameraLookTarget(out var resolvedCameraLookTarget), Is.True);
            Assert.That(defaults.TryGetPlayerArmsRoot(out var resolvedPlayerArmsRoot), Is.True);
            Assert.That(defaults.TryGetPlayerArmsAnimator(out var resolvedPlayerArmsAnimator), Is.True);
            Assert.That(defaults.TryGetWeaponPresentationRoot(out var resolvedWeaponPresentationRoot), Is.True);

            Assert.That(resolvedPivot, Is.SameAs(presentationPivot));
            Assert.That(resolvedMainCamera, Is.Not.Null);
            Assert.That(resolvedCameraLookTarget, Is.SameAs(presentationPivot));
            Assert.That(resolvedPlayerArmsRoot, Is.SameAs(playerArmsRoot));
            Assert.That(resolvedPlayerArmsAnimator, Is.SameAs(playerArmsAnimator));
            Assert.That(resolvedWeaponPresentationRoot, Is.SameAs(weaponPresentationRoot));
            Assert.That(presentationPivot.Find("WeaponPresentationRoot"), Is.Null,
                "Explicit PlayerCameraDefaults ownership should not create the legacy CameraPivot/WeaponPresentationRoot fallback.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryGetPresentationCamera_DoesNotRecoverFromCameraMain_WhenPresentationCameraIsMissing()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var mainCamera = new GameObject("MainCamera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.transform.SetParent(cameraPivot, false);

            var defaults = root.AddComponent<PlayerCameraDefaults>();

            Assert.That(defaults.TryGetPresentationCamera(out var resolvedPresentationCamera), Is.False);
            Assert.That(resolvedPresentationCamera, Is.Null);

            Object.DestroyImmediate(root);
        }

        public static void VerifySlice()
        {
            var suite = new PlayerCameraDefaultsEditModeTests();
            suite.TryGetMainCamera_DoesNotRecoverFromCameraMain_WhenExplicitMainCameraIsMissing();
            suite.TryGetAuthoredMainCamera_DoesNotRecoverFromCameraMain_WhenExplicitMainCameraIsMissing();
            suite.TryGetPresentationCamera_DoesNotRecoverFromCameraMain_WhenPresentationCameraIsMissing();
            suite.TryGetCameraLookTarget_DoesNotRecoverFromCameraFollowTarget_WhenExplicitCameraLookTargetIsMissing();
            suite.TryGetViewmodelCamera_DoesNotReparentCachedCamera_WhenExplicitParentMismatchExists();
            suite.ApplyDefaults_DoesNotRecoverViewmodelCameraFromChildName_WhenExplicitViewmodelCameraMissing();
            suite.ExplicitOwnershipContract_UsesSerializedRoots_AndDoesNotCreateLegacyPresentationFallback();
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
