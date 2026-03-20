using NUnit.Framework;
using Reloader.Player.Viewmodel;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerRigMenuEditModeTests
    {
        [Test]
        public void TryRebuildFpsArmsViewmodel_DoesNotCreateWeaponPresentationRoot_WhenMissing()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.transform.SetParent(cameraPivot, false);

            var previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = root;
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "ConfigureFpsArmsViewmodelOnSelectedRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.ConfigureFpsArmsViewmodelOnSelectedRig to exist.");

                LogAssert.Expect(LogType.Warning, "Selected rig must already have an authored main camera reference.");
                method!.Invoke(null, null);

                Assert.That(cameraPivot.Find("PlayerArms"), Is.Null);
                Assert.That(cameraPivot.Find("PlayerArmsVisual"), Is.Null);
                Assert.That(cameraPivot.Find("WeaponPresentationRoot"), Is.Null);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RepairSelectedFpsRig_DoesNotBackfillWeaponPresentationRoot_WhenMissing()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.transform.SetParent(cameraPivot, false);

            var previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = root;
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "RepairSelectedFpsRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.RepairSelectedFpsRig to exist.");

                method!.Invoke(null, null);

                Assert.That(root.transform.Find("PlayerArms"), Is.Null);
                Assert.That(root.transform.Find("PlayerArmsVisual"), Is.Null);
                Assert.That(root.transform.Find("WeaponPresentationRoot"), Is.Null);
                Assert.That(root.GetComponent<CharacterController>(), Is.Null);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CreateFpsRig_DoesNotCreateWeaponPresentationRoot_AsLegacyBackfill()
        {
            var previousSelection = Selection.activeGameObject;
            GameObject createdRoot = null;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "CreateFpsRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.CreateFpsRig to exist.");

                LogAssert.Expect(LogType.Warning, "Create FPS Rig requires an authored first-person presentation hierarchy.");
                method!.Invoke(null, null);

                createdRoot = Selection.activeGameObject;
                Assert.That(createdRoot, Is.Not.Null);
                Assert.That(createdRoot.transform.Find("CameraPivot"), Is.Null);
                Assert.That(createdRoot.transform.Find("CM_PlayerCamera"), Is.Null);
                Assert.That(createdRoot.transform.Find("Main Camera"), Is.Null);
                Assert.That(createdRoot.transform.Find("PlayerArms"), Is.Null);
                Assert.That(createdRoot.transform.Find("PlayerArmsVisual"), Is.Null);
                Assert.That(createdRoot.transform.Find("WeaponPresentationRoot"), Is.Null);
                Assert.That(createdRoot.GetComponent<PlayerCameraDefaults>(), Is.Null);
            }
            finally
            {
                if (createdRoot != null && createdRoot != previousSelection)
                {
                    Object.DestroyImmediate(createdRoot);
                }

                Selection.activeGameObject = previousSelection;
            }
        }

        [Test]
        public void RepairSelectedFpsRig_DoesNotCreateCameraPivot_WhenAuthoredPresentationContractMissing()
        {
            var root = new GameObject("PlayerRoot");
            var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(root.transform, false);

            var previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = root;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "RepairSelectedFpsRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.RepairSelectedFpsRig to exist.");

                LogAssert.Expect(LogType.Warning, "Selected rig must already have an authored first-person presentation hierarchy.");
                method!.Invoke(null, null);

                Assert.That(root.transform.Find("CameraPivot"), Is.Null);
                Assert.That(root.transform.Find("CM_PlayerCamera"), Is.Null);
                Assert.That(root.transform.Find("Main Camera"), Is.Null);
                Assert.That(root.transform.Find("PlayerArms"), Is.Null);
                Assert.That(root.transform.Find("PlayerArmsVisual"), Is.Null);
                Assert.That(root.transform.Find("WeaponPresentationRoot"), Is.SameAs(weaponPresentationRoot));
                Assert.That(root.GetComponent<PlayerCameraDefaults>(), Is.Null);
            }
            finally
            {
                Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConfigureFpsArmsViewmodelOnSelectedRig_DoesNotFallBackToScenePlayerRoot_WhenNothingIsSelected()
        {
            var root = new GameObject("PlayerRoot");
            root.AddComponent<PlayerInputReader>();
            var previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = null;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "ConfigureFpsArmsViewmodelOnSelectedRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.ConfigureFpsArmsViewmodelOnSelectedRig to exist.");

                LogAssert.Expect(LogType.Warning, "Select a player root GameObject first.");
                method!.Invoke(null, null);
            }
            finally
            {
                Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConfigureFpsArmsViewmodelOnSelectedRig_UsesExplicitDefaultsWithoutLegacyChildNames()
        {
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            var root = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(root.transform, false);

            var weaponMount = new GameObject("WeaponMount").transform;
            weaponMount.SetParent(presentationPivot, false);

            var armsRoot = new GameObject("ArmsBranch").transform;
            armsRoot.SetParent(presentationPivot, false);
            var armsVisual = new GameObject("VisualRoot").transform;
            armsVisual.SetParent(armsRoot, false);
            var animator = armsVisual.gameObject.AddComponent<Animator>();

            var worldCamera = new GameObject("WorldCamera").AddComponent<Camera>();
            worldCamera.transform.SetParent(presentationPivot, false);

            var viewmodelCamera = new GameObject("ExplicitViewCamera").AddComponent<Camera>();
            viewmodelCamera.transform.SetParent(presentationPivot, false);

            var lookTarget = new GameObject("LookTarget").transform;
            lookTarget.SetParent(presentationPivot, false);

            AddCinemachineCamera(root.transform);

            var cameraDefaults = root.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_mainCamera", worldCamera);
            SetField(cameraDefaults, "_cameraPivot", presentationPivot);
            SetField(cameraDefaults, "_cameraLookTarget", lookTarget);
            SetField(cameraDefaults, "_viewmodelCameraParent", presentationPivot);
            SetField(cameraDefaults, "_viewmodelCamera", viewmodelCamera);
            SetField(cameraDefaults, "_playerArmsRoot", armsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", animator);
            SetField(cameraDefaults, "_weaponPresentationRoot", weaponMount);

            root.AddComponent<FpsViewmodelAnimatorDriver>();
            root.AddComponent<ViewmodelAnimationAdapter>();
            var handRigControllerType = System.Type.GetType("Reloader.Player.Viewmodel.WeaponHandRigController, Reloader.Weapons");
            Assert.That(handRigControllerType, Is.Not.Null, "Expected WeaponHandRigController type to exist.");
            var handRigController = root.AddComponent(handRigControllerType!);
            var handTargetRoot = new GameObject("WeaponHandRigTargets").transform;
            handTargetRoot.SetParent(presentationPivot, false);
            SetField(handRigController, "_handTargetRoot", handTargetRoot);

            var previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = root;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "ConfigureFpsArmsViewmodelOnSelectedRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.ConfigureFpsArmsViewmodelOnSelectedRig to exist.");

                LogAssert.Expect(LogType.Log, "FPS arms viewmodel configured on selected rig.");
                method!.Invoke(null, null);

                Assert.That(weaponMount.parent, Is.SameAs(presentationPivot));
                Assert.That(armsRoot.parent, Is.SameAs(presentationPivot));
                Assert.That(viewmodelCamera.transform.parent, Is.SameAs(presentationPivot));
                Assert.That(root.transform.Find("CameraPivot"), Is.Null);
                Assert.That(presentationPivot.Find("PlayerArms"), Is.Null);
                Assert.That(presentationPivot.Find("WeaponPresentationRoot"), Is.Null);
                Assert.That(presentationPivot.Find("ViewmodelCamera"), Is.Null);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConfigureFpsArmsViewmodelOnSelectedRig_DoesNotAddMissingRuntimeHelpers()
        {
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            var root = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(root.transform, false);

            var weaponMount = new GameObject("WeaponMount").transform;
            weaponMount.SetParent(presentationPivot, false);

            var armsRoot = new GameObject("ArmsBranch").transform;
            armsRoot.SetParent(presentationPivot, false);
            var armsVisual = new GameObject("VisualRoot").transform;
            armsVisual.SetParent(armsRoot, false);
            var animator = armsVisual.gameObject.AddComponent<Animator>();

            var worldCamera = new GameObject("WorldCamera").AddComponent<Camera>();
            worldCamera.transform.SetParent(presentationPivot, false);

            var viewmodelCamera = new GameObject("ExplicitViewCamera").AddComponent<Camera>();
            viewmodelCamera.transform.SetParent(presentationPivot, false);

            var lookTarget = new GameObject("LookTarget").transform;
            lookTarget.SetParent(presentationPivot, false);

            AddCinemachineCamera(root.transform);

            var cameraDefaults = root.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_mainCamera", worldCamera);
            SetField(cameraDefaults, "_cameraPivot", presentationPivot);
            SetField(cameraDefaults, "_cameraLookTarget", lookTarget);
            SetField(cameraDefaults, "_viewmodelCameraParent", presentationPivot);
            SetField(cameraDefaults, "_viewmodelCamera", viewmodelCamera);
            SetField(cameraDefaults, "_playerArmsRoot", armsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", animator);
            SetField(cameraDefaults, "_weaponPresentationRoot", weaponMount);

            var previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = root;

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "ConfigureFpsArmsViewmodelOnSelectedRig",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.ConfigureFpsArmsViewmodelOnSelectedRig to exist.");

                LogAssert.Expect(LogType.Warning,
                    "Selected rig must already have authored FpsViewmodelAnimatorDriver, ViewmodelAnimationAdapter, and WeaponHandRigController components.");
                method!.Invoke(null, null);

                Assert.That(root.GetComponent<FpsViewmodelAnimatorDriver>(), Is.Null);
                Assert.That(root.GetComponent<ViewmodelAnimationAdapter>(), Is.Null);
                Assert.That(root.GetComponent("WeaponHandRigController"), Is.Null);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
        }

        private static void AddCinemachineCamera(Transform parent)
        {
            var cinemachineCameraType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            Assert.That(cinemachineCameraType, Is.Not.Null, "Expected CinemachineCamera type to exist.");

            var cinemachineCamera = new GameObject("PresentationCinemachine");
            cinemachineCamera.transform.SetParent(parent, false);
            cinemachineCamera.AddComponent(cinemachineCameraType!);
        }
    }
}
