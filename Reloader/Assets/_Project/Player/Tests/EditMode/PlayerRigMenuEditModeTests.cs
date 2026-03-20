using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            playerArmsVisual.AddComponent<Animator>();

            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.transform.SetParent(cameraPivot, false);

            try
            {
                var playerRigMenuType = System.Type.GetType("Reloader.Player.Editor.PlayerRigMenu, Reloader.Player.Editor");
                Assert.That(playerRigMenuType, Is.Not.Null, "Expected PlayerRigMenu type to exist.");

                var method = playerRigMenuType!.GetMethod(
                    "TryRebuildFpsArmsViewmodel",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null, "Expected PlayerRigMenu.TryRebuildFpsArmsViewmodel to exist.");

                var result = (bool)method!.Invoke(null, new object[] { cameraPivot, mainCamera })!;

                Assert.That(result, Is.False);
                Assert.That(cameraPivot.Find("WeaponPresentationRoot"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RepairSelectedFpsRig_DoesNotBackfillWeaponPresentationRoot_WhenMissing()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            playerArmsVisual.AddComponent<Animator>();

            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.transform.SetParent(cameraPivot, false);

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

                method!.Invoke(null, null);

                Assert.That(root.transform.Find("WeaponPresentationRoot"), Is.Null);
                Assert.That(root.GetComponent<CharacterController>(), Is.Null);
            }
            finally
            {
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

                method!.Invoke(null, null);

                createdRoot = Selection.activeGameObject;
                Assert.That(createdRoot, Is.Not.Null);
                Assert.That(createdRoot.transform.Find("WeaponPresentationRoot"), Is.Null);
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
    }
}
