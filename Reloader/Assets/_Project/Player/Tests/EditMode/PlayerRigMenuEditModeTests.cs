using NUnit.Framework;
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
    }
}
