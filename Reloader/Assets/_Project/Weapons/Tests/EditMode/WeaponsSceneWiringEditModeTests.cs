using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Ballistics;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.EditMode
{
    public sealed class WeaponsSceneWiringEditModeTests
    {
        private const string StarterRiflePath = "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset";
        private const string StarterPistolPath = "Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset";
        private const string ProjectilePrefabPath = "Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab";

        [Test]
        public void WireScene_FailsClosed_WhenWeaponPresentationRootIsMissing()
        {
            var rig = CreateRig(includeCameraPivot: true, includeWeaponPresentationRoot: false);

            try
            {
                var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
                var starterPistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterPistolPath);
                var projectilePrefab = LoadProjectilePrefab();

                Assert.That(starterRifle, Is.Not.Null, "Expected starter rifle asset.");
                Assert.That(starterPistol, Is.Not.Null, "Expected starter pistol asset.");
                Assert.That(projectilePrefab, Is.Not.Null, "Expected weapon projectile prefab.");

                LogAssert.Expect(LogType.Error,
                    "Weapons scene wiring failed: PlayerRoot must already have authored PlayerCameraDefaults with CameraPivot and WeaponPresentationRoot.");

                var resolved = InvokeWireScene(starterRifle, starterPistol, projectilePrefab);

                Assert.That(resolved, Is.False);
                Assert.That(rig.PlayerRoot.transform.Find("CameraPivot"), Is.SameAs(rig.CameraPivot));
                Assert.That(rig.CameraPivot.Find("WeaponPresentationRoot"), Is.Null,
                    "WeaponsSceneWiring should not synthesize WeaponPresentationRoot when the authored root is missing.");
                Assert.That(rig.PlayerRoot.GetComponent<PlayerWeaponController>(), Is.Null,
                    "WeaponsSceneWiring should fail closed before creating runtime components when the authored contract is incomplete.");
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void WireScene_FailsClosed_WhenCameraPivotIsMissing()
        {
            var rig = CreateRig(includeCameraPivot: false, includeWeaponPresentationRoot: false);

            try
            {
                var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
                var starterPistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterPistolPath);
                var projectilePrefab = LoadProjectilePrefab();

                Assert.That(starterRifle, Is.Not.Null, "Expected starter rifle asset.");
                Assert.That(starterPistol, Is.Not.Null, "Expected starter pistol asset.");
                Assert.That(projectilePrefab, Is.Not.Null, "Expected weapon projectile prefab.");

                LogAssert.Expect(LogType.Error,
                    "Weapons scene wiring failed: PlayerRoot must already have authored PlayerCameraDefaults with CameraPivot and WeaponPresentationRoot.");

                var resolved = InvokeWireScene(starterRifle, starterPistol, projectilePrefab);

                Assert.That(resolved, Is.False);
                Assert.That(rig.PlayerRoot.transform.Find("CameraPivot"), Is.Null,
                    "WeaponsSceneWiring should not recreate CameraPivot when the authored pivot is missing.");
                Assert.That(rig.PlayerRoot.GetComponent<PlayerWeaponController>(), Is.Null,
                    "WeaponsSceneWiring should fail closed before creating runtime components when CameraPivot is missing.");
            }
            finally
            {
                rig.Dispose();
            }
        }

        private static bool InvokeWireScene(
            WeaponDefinition starterRifle,
            WeaponDefinition starterPistol,
            WeaponProjectile projectilePrefab)
        {
            var wiringType = FindType("Reloader.Weapons.Editor.WeaponsSceneWiring");
            Assert.That(wiringType, Is.Not.Null, "Expected WeaponsSceneWiring to be present.");

            var method = wiringType!.GetMethod("WireScene", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected WeaponsSceneWiring.WireScene to exist.");

            return (bool)method!.Invoke(null, new object[] { "TestScene", starterRifle, starterPistol, projectilePrefab })!;
        }

        private static WeaponProjectile LoadProjectilePrefab()
        {
            var projectilePrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            Assert.That(projectilePrefabGo, Is.Not.Null, "Expected weapon projectile prefab GameObject.");

            return projectilePrefabGo!.GetComponent<WeaponProjectile>();
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Rig CreateRig(bool includeCameraPivot, bool includeWeaponPresentationRoot)
        {
            var playerRoot = new GameObject("PlayerRoot");
            var inputReader = playerRoot.AddComponent<PlayerInputReader>();
            var inventoryController = playerRoot.AddComponent<PlayerInventoryController>();
            var cameraDefaults = playerRoot.AddComponent<PlayerCameraDefaults>();

            Transform cameraPivot = null;
            if (includeCameraPivot)
            {
                cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.SetParent(playerRoot.transform, false);
                SetField(cameraDefaults, "_cameraPivot", cameraPivot);
                SetField(cameraDefaults, "_cameraFollowTarget", cameraPivot);

                var muzzle = new GameObject("WeaponMuzzle").transform;
                muzzle.SetParent(cameraPivot, false);
            }

            if (includeWeaponPresentationRoot && cameraPivot != null)
            {
                var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
                weaponPresentationRoot.SetParent(cameraPivot, false);
                SetField(cameraDefaults, "_weaponPresentationRoot", weaponPresentationRoot);
            }

            return new Rig(playerRoot, inputReader, inventoryController, cameraDefaults, cameraPivot);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
        }

        private sealed class Rig : IDisposable
        {
            public Rig(GameObject playerRoot, PlayerInputReader inputReader, PlayerInventoryController inventoryController, PlayerCameraDefaults cameraDefaults, Transform cameraPivot)
            {
                PlayerRoot = playerRoot;
                InputReader = inputReader;
                InventoryController = inventoryController;
                CameraDefaults = cameraDefaults;
                CameraPivot = cameraPivot;
            }

            public GameObject PlayerRoot { get; }
            public PlayerInputReader InputReader { get; }
            public PlayerInventoryController InventoryController { get; }
            public PlayerCameraDefaults CameraDefaults { get; }
            public Transform CameraPivot { get; }

            public void Dispose()
            {
                if (PlayerRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(PlayerRoot);
                }
            }
        }
    }
}
