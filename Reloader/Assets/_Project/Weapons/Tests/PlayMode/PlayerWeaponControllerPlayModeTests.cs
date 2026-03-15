using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Cinematics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class PlayerWeaponControllerPlayModeTests
    {
        private IGameEventsRuntimeHub _runtimeEventsBeforeEachTest;
        private readonly HashSet<int> _baselineRootInstanceIds = new();

        [SetUp]
        public void SetUp()
        {
            _runtimeEventsBeforeEachTest = RuntimeKernelBootstrapper.Events;
            _baselineRootInstanceIds.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                _baselineRootInstanceIds.Add(root.GetInstanceID());
            }
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null || _baselineRootInstanceIds.Contains(root.GetInstanceID()))
                {
                    continue;
                }

                Object.Destroy(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_InjectedWeaponAndInventoryEvents_RaisesThroughInjectedPortsOnly()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            var fallbackHandlersRegistered = false;

            var fallbackEquipRaised = 0;
            var fallbackFireRaised = 0;
            var fallbackReloadRaised = 0;
            var fallbackInventoryChangedRaised = 0;
            void HandleFallbackWeaponEquipped(string _) => fallbackEquipRaised++;
            void HandleFallbackWeaponFired(string _, Vector3 __, Vector3 ___) => fallbackFireRaised++;
            void HandleFallbackWeaponReloaded(string _, int __, int ___) => fallbackReloadRaised++;
            void HandleFallbackInventoryChanged() => fallbackInventoryChangedRaised++;

            try
            {
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                registry.SetDefinitionsForTests(new[] { definition });

                var injectedEvents = new DefaultRuntimeEvents();
                var injectedEquipRaised = 0;
                var injectedFireRaised = 0;
                var injectedReloadRaised = 0;
                var injectedInventoryChangedRaised = 0;
                injectedEvents.OnWeaponEquipped += _ => injectedEquipRaised++;
                injectedEvents.OnWeaponFired += (_, _, _) => injectedFireRaised++;
                injectedEvents.OnWeaponReloaded += (_, _, _) => injectedReloadRaised++;
                injectedEvents.OnInventoryChanged += () => injectedInventoryChangedRaised++;

                fallbackRuntimeEvents.OnWeaponEquipped += HandleFallbackWeaponEquipped;
                fallbackRuntimeEvents.OnWeaponFired += HandleFallbackWeaponFired;
                fallbackRuntimeEvents.OnWeaponReloaded += HandleFallbackWeaponReloaded;
                fallbackRuntimeEvents.OnInventoryChanged += HandleFallbackInventoryChanged;
                fallbackHandlersRegistered = true;

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                controller.Configure(weaponEvents: injectedEvents, inventoryEvents: injectedEvents);
                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;
                input.ReloadPressedThisFrame = true;
                yield return null;
                yield return new WaitForSeconds(0.36f);

                Assert.That(injectedEquipRaised, Is.GreaterThan(0));
                Assert.That(injectedFireRaised, Is.EqualTo(1));
                Assert.That(injectedReloadRaised, Is.EqualTo(1));
                Assert.That(injectedInventoryChangedRaised, Is.EqualTo(1));

                Assert.That(fallbackEquipRaised, Is.EqualTo(0));
                Assert.That(fallbackFireRaised, Is.EqualTo(0));
                Assert.That(fallbackReloadRaised, Is.EqualTo(0));
                Assert.That(fallbackInventoryChangedRaised, Is.EqualTo(0));
            }
            finally
            {
                if (fallbackHandlersRegistered)
                {
                    fallbackRuntimeEvents.OnWeaponEquipped -= HandleFallbackWeaponEquipped;
                    fallbackRuntimeEvents.OnWeaponFired -= HandleFallbackWeaponFired;
                    fallbackRuntimeEvents.OnWeaponReloaded -= HandleFallbackWeaponReloaded;
                    fallbackRuntimeEvents.OnInventoryChanged -= HandleFallbackInventoryChanged;
                }

                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Fire_RealStarterRifleContent_ReachesLongRangeTarget()
        {
#if UNITY_EDITOR
            GameObject root = null;
            GameObject registryGo = null;
            GameObject target = null;

            try
            {
                var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset");
                Assert.That(definition, Is.Not.Null, "Expected the real StarterRifle weapon definition asset.");

                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = new Vector3(0f, 1000f, 300f);
                target.transform.localScale = new Vector3(60f, 120f, 1f);
                var receiver = target.AddComponent<TestDamageable>();

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                Random.InitState(1337);
                input.FirePressedThisFrame = true;
                yield return null;

                var elapsed = 0f;
                while (receiver.HitCount == 0 && elapsed < 2f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(
                    receiver.HitCount,
                    Is.EqualTo(1),
                    "The real Kar98k content should keep the projectile alive past the old 220m cutoff instead of despawning mid-flight.");
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (target != null)
                {
                    Object.Destroy(target);
                }
            }
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator Fire_AimingAtPredictedHitBeyond100Meters_RequestsShotCameraImmediately()
        {
            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 1000f, 0f);
            var input = root.AddComponent<TestInputSource>();
            input.AimHeldValue = true;
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);
            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var worldCameraGo = new GameObject("WorldCamera");
            worldCameraGo.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.tag = "MainCamera";

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var farTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            farTarget.transform.position = root.transform.position + (Vector3.forward * 150f);
            farTarget.transform.localScale = new Vector3(20f, 20f, 2f);

            var shotCameraSpy = root.AddComponent<ShotCameraRegistrationSpy>();
            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", worldCamera);
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraSpy);
            SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
            yield return null;

            var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
            var magazineRounds = new[]
            {
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
            };
            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
            Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(shotCameraSpy.RequestCount, Is.EqualTo(1), "Expected long ADS shots to request shot cam on the same fired frame.");
            Assert.That(shotCameraSpy.LastProjectile, Is.Not.Null, "Expected the shot-cam request to carry the live projectile instance.");

            Object.Destroy(farTarget);
            Object.Destroy(worldCameraGo);
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_AimingAtPredictedHitAtOrBelow100Meters_DoesNotRequestShotCamera()
        {
            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 1000f, 0f);
            var input = root.AddComponent<TestInputSource>();
            input.AimHeldValue = true;
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);
            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var worldCameraGo = new GameObject("WorldCamera");
            worldCameraGo.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.tag = "MainCamera";

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var nearTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nearTarget.transform.position = root.transform.position + (Vector3.forward * 80f);
            nearTarget.transform.localScale = new Vector3(20f, 20f, 2f);

            var shotCameraSpy = root.AddComponent<ShotCameraRegistrationSpy>();
            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", worldCamera);
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraSpy);
            SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
            yield return null;

            var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
            var magazineRounds = new[]
            {
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
            };
            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
            Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(shotCameraSpy.RequestCount, Is.EqualTo(0), "Expected short ADS shots to stay in normal first-person view.");

            Object.Destroy(nearTarget);
            Object.Destroy(worldCameraGo);
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_HipFireLongPredictedHit_DoesNotRequestShotCamera()
        {
            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 1000f, 0f);
            var input = root.AddComponent<TestInputSource>();
            input.AimHeldValue = false;
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);
            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var worldCameraGo = new GameObject("WorldCamera");
            worldCameraGo.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.tag = "MainCamera";

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var farTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            farTarget.transform.position = root.transform.position + (Vector3.forward * 150f);
            farTarget.transform.localScale = new Vector3(20f, 20f, 2f);

            var shotCameraSpy = root.AddComponent<ShotCameraRegistrationSpy>();
            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", worldCamera);
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraSpy);
            SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
            yield return null;

            var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
            var magazineRounds = new[]
            {
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
            };
            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
            Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(shotCameraSpy.RequestCount, Is.EqualTo(0), "Expected hip-fire to bypass shot cam even for long predicted hits.");

            Object.Destroy(farTarget);
            Object.Destroy(worldCameraGo);
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_WithShotCameraRuntime_SetsDefaultSlowMotionScale()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                var gameplayBrain = cameraGo.AddComponent<CinemachineBrain>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 180f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;
                yield return null;

                var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
                var shotRenderCamera = FindShotRenderCamera();
                var originalWorldCameraPosition = worldCamera.transform.position;
                var originalWorldCameraRotation = worldCamera.transform.rotation;
                Assert.That(projectile, Is.Not.Null);
                Assert.That(shotRenderCamera, Is.Not.Null, "Expected shot cam to create a dedicated temporary render camera instead of reusing the gameplay MainCamera.");
                Assert.That(shotRenderCamera, Is.Not.SameAs(worldCamera), "Expected shot cam render camera ownership to be separate from the gameplay MainCamera.");
                Assert.That(gameplayBrain.enabled, Is.True, "Expected the pre-existing gameplay CinemachineBrain to remain enabled while the isolated shot camera renders the cinematic.");
                Assert.That(Vector3.Distance(worldCamera.transform.position, originalWorldCameraPosition), Is.LessThan(0.001f),
                    "Expected the gameplay MainCamera transform to remain parked on the player rig during shot-cam.");
                Assert.That(Quaternion.Angle(worldCamera.transform.rotation, originalWorldCameraRotation), Is.LessThan(0.01f),
                    "Expected the gameplay MainCamera rotation to remain unchanged during shot-cam.");
                Assert.That(shotCameraRuntime.IsShotActive, Is.True);
                Assert.That(shotCameraRuntime.HasActiveCinematicCamera, Is.True);
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.SameAs(shotRenderCamera), "Expected shot cam to publish the temporary render camera as the current presentation camera.");
                Assert.That(projectile!.IsShotCameraPresentationActive, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0.1f).Within(0.001f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_HoldingSpeedUpInput_RaisesTimeScaleUntilReleased()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 180f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
                Assert.That(projectile, Is.Not.Null);
                input.ShotCameraSpeedUpHeldValue = true;
                yield return null;

                Assert.That(Time.timeScale, Is.EqualTo(0.25f).Within(0.001f));

                input.ShotCameraSpeedUpHeldValue = false;
                yield return null;

                Assert.That(Time.timeScale, Is.EqualTo(0.1f).Within(0.001f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_CancelInput_RestoresRealtimeAndKeepsProjectileAlive()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 300f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
                Assert.That(projectile, Is.Not.Null);

                input.ShotCameraCancelPressedThisFrame = true;
                yield return null;
                yield return null;

                Assert.That(shotCameraRuntime.IsShotActive, Is.False);
                Assert.That(shotCameraRuntime.HasActiveCinematicCamera, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.Null, "Expected shot cam cancel to clear the current presentation camera.");
                Assert.That(projectile, Is.Not.Null);
                Assert.That(projectile.gameObject, Is.Not.Null);
                Assert.That(projectile.IsShotCameraPresentationActive, Is.False);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_ProjectileImpact_RestoresRealtimeAndClearsCinematicCamera()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 110f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);
                target.AddComponent<TestDamageable>();

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = new AmmoBallisticSnapshot(
                    AmmoSourceType.Factory,
                    muzzleVelocityFps: 10000f,
                    velocityStdDevFps: 0f,
                    projectileMassGrains: 147f,
                    ballisticCoefficientG1: 0.45f,
                    dispersionMoa: 0f,
                    displayName: "ShotCamTestRound",
                    cartridgeId: "shotcam-test-round",
                    ammoItemId: "ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    chamberRound,
                    chamberRound,
                    chamberRound,
                    chamberRound
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(FindShotRenderCamera(), Is.Not.Null);
                Assert.That(shotCameraRuntime.IsShotActive, Is.True);

                var lingerStartTime = Time.unscaledTime;
                var elapsed = 0f;
                while (shotCameraRuntime.IsShotActive && elapsed < 4f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(shotCameraRuntime.IsShotActive, Is.False, "Expected projectile impact to end shot cam automatically.");
                Assert.That(shotCameraRuntime.HasActiveCinematicCamera, Is.False, "Expected projectile impact to clear the temporary cinematic camera.");
                Assert.That(Time.unscaledTime - lingerStartTime, Is.GreaterThanOrEqualTo(0.9f), "Expected non-NPC impact to linger at the impact location before restoring the player camera.");
                Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
                Assert.That(FindShotRenderCamera(), Is.Null, "Expected the temporary shot camera to be removed after impact.");
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.Null, "Expected shot-cam impact exit to clear the current presentation camera.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Fire_WithShotCameraRuntime_HidesUiAndViewmodel_AndRestoresThemOnCancel()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            GameObject uiRootGo = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform, false);
                var pitchTransform = new GameObject("PitchTransform");
                pitchTransform.transform.SetParent(root.transform, false);
                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, pitchTransform.transform);
                look.LookSensitivity = Vector2.one;

                cameraGo = new GameObject("WorldCamera");
                cameraGo.transform.SetParent(cameraPivot.transform, false);
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                var playerCameraDefaults = root.AddComponent<PlayerCameraDefaults>();
                SetPrivateField(typeof(PlayerCameraDefaults), playerCameraDefaults, "_mainCamera", worldCamera);

                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                var viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();
                viewmodelCamera.enabled = true;

                var scopeCameraGo = new GameObject("ScopeCamera");
                scopeCameraGo.transform.SetParent(worldCamera.transform, false);
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();
                scopeCamera.enabled = true;

                var packAnimatorGo = new GameObject("PackAnimator");
                packAnimatorGo.transform.SetParent(root.transform, false);
                var packAnimator = packAnimatorGo.AddComponent<Animator>();
                var viewmodelVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                viewmodelVisual.transform.SetParent(packAnimatorGo.transform, false);
                var viewmodelRenderer = viewmodelVisual.GetComponent<Renderer>();
                Assert.That(viewmodelRenderer, Is.Not.Null);
                viewmodelRenderer!.enabled = true;

                uiRootGo = new GameObject("UiToolkitRuntimeRoot");
                var beltDocument = CreateRuntimeHudDocument("belt-hud", uiRootGo.transform);
                var ammoDocument = CreateRuntimeHudDocument("ammo-hud", uiRootGo.transform);
                yield return null;

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 180f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_cameraDefaults", playerCameraDefaults);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_packAnimator", packAnimator);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;
                yield return null;

                Assert.That(shotCameraRuntime.IsShotActive, Is.True);
                Assert.That(viewmodelRenderer.enabled, Is.False, "Expected shot cam to hide the weapon viewmodel.");
                Assert.That(viewmodelCamera.enabled, Is.False, "Expected shot cam to disable the viewmodel overlay camera.");
                Assert.That(scopeCamera.enabled, Is.False, "Expected shot cam to disable the scope camera during the cinematic.");
                Assert.That(IsDocumentVisible(beltDocument), Is.False, "Expected shot cam to hide HUD documents for a clean screen.");
                Assert.That(IsDocumentVisible(ammoDocument), Is.False, "Expected shot cam to hide all runtime HUD documents for a clean screen.");

                input.ShotCameraCancelPressedThisFrame = true;
                yield return null;
                yield return null;

                Assert.That(shotCameraRuntime.IsShotActive, Is.False);
                Assert.That(viewmodelRenderer.enabled, Is.True, "Expected shot cam exit to restore the weapon viewmodel.");
                Assert.That(viewmodelCamera.enabled, Is.True, "Expected shot cam exit to restore the viewmodel overlay camera.");
                Assert.That(scopeCamera.enabled, Is.True, "Expected shot cam exit to restore the scope camera.");
                Assert.That(IsDocumentVisible(beltDocument), Is.True, "Expected shot cam exit to restore hidden HUD documents.");
                Assert.That(IsDocumentVisible(ammoDocument), Is.True, "Expected shot cam exit to restore all hidden HUD documents.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (uiRootGo != null)
                {
                    Object.Destroy(uiRootGo);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Fire_WithShotCameraRuntime_DoesNotToggleHiddenMenuDocuments()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            GameObject uiRootGo = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                uiRootGo = new GameObject("UiToolkitRuntimeRoot");
                CreateRuntimeHudDocument("belt-hud", uiRootGo.transform);
                var hiddenMenuDocument = CreateRuntimeHudDocument("trade-ui", uiRootGo.transform);
                yield return null;

                hiddenMenuDocument.rootVisualElement.visible = false;
                hiddenMenuDocument.rootVisualElement.pickingMode = PickingMode.Ignore;

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 180f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(hiddenMenuDocument.enabled, Is.True, "Expected hidden menu documents to stay enabled so their controller state is preserved.");
                Assert.That(hiddenMenuDocument.gameObject.activeSelf, Is.True, "Expected hidden menu documents not to be toggled off during shot cam.");
                Assert.That(hiddenMenuDocument.rootVisualElement.visible, Is.False, "Expected hidden menus to remain hidden during shot cam.");

                input.ShotCameraCancelPressedThisFrame = true;
                yield return null;

                Assert.That(hiddenMenuDocument.enabled, Is.True, "Expected hidden menu documents to remain enabled after shot cam exits.");
                Assert.That(hiddenMenuDocument.gameObject.activeSelf, Is.True, "Expected hidden menu documents not to be reactivated on shot cam exit because they were never toggled.");
                Assert.That(hiddenMenuDocument.rootVisualElement.visible, Is.False, "Expected hidden menus to remain hidden after shot cam exits.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (uiRootGo != null)
                {
                    Object.Destroy(uiRootGo);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_ActiveShot_BlocksFollowUpFireUntilShotEnds()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 220f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(shotCameraRuntime.IsShotActive, Is.True);
                Assert.That(Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "Expected shot cam to block follow-up fire while the cinematic is active.");

                input.ShotCameraCancelPressedThisFrame = true;
                yield return null;

                var cancelElapsed = 0f;
                while (shotCameraRuntime.IsShotActive && cancelElapsed < 0.25f)
                {
                    cancelElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(shotCameraRuntime.IsShotActive, Is.False, "Expected shot cam cancel to finish before testing follow-up fire.");
                Assert.That(ShotCameraGameplayState.IsActive, Is.False, "Expected cancel to release the exclusive shot-cam gameplay state.");
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterCancel), Is.True);

                var fireReadyElapsed = 0f;
                while (!stateAfterCancel.CanFire(Time.time) && fireReadyElapsed < 0.25f)
                {
                    fireReadyElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(stateAfterCancel.CanFire(Time.time), Is.True, "Expected the weapon fire interval cooldown to clear before validating post-shot-cam refire.");

                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length, Is.EqualTo(2),
                    "Expected firing to resume once the cinematic has fully exited.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_LookInput_OrbitsWithoutRotatingPlayerYaw()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                var pitchTransform = new GameObject("PitchTransform");
                pitchTransform.transform.SetParent(root.transform, false);
                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, pitchTransform.transform);
                look.LookSensitivity = Vector2.one;

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 220f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                var playerRotationBeforeOrbit = root.transform.rotation;
                var cameraPositionBeforeOrbit = worldCamera.transform.position;
                var shotRenderCamera = FindShotRenderCamera();
                Assert.That(shotRenderCamera, Is.Not.Null, "Expected shot cam orbit to drive the temporary render camera, not the gameplay MainCamera.");
                var cinematicPositionBeforeOrbit = shotRenderCamera.transform.position;
                input.LookInputValue = new Vector2(12f, -4f);
                yield return null;
                input.LookInputValue = Vector2.zero;
                yield return null;
                yield return null;

                Assert.That(root.transform.rotation, Is.EqualTo(playerRotationBeforeOrbit).Using(QuaternionEqualityComparer.Instance), "Expected shot cam orbit input to avoid rotating the player body.");
                Assert.That(Vector3.Distance(worldCamera.transform.position, cameraPositionBeforeOrbit), Is.LessThan(0.001f),
                    "Expected the gameplay MainCamera to stay fixed while shot-cam orbit updates the temporary render camera.");
                Assert.That(Vector3.Distance(shotRenderCamera.transform.position, cinematicPositionBeforeOrbit), Is.GreaterThan(0.05f),
                    "Expected shot cam look input to orbit the cinematic camera around the projectile.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_NpcImpact_LingersForTwoSecondsBeforeRestore()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 110f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);
                var npcAgentType = Type.GetType("Reloader.NPCs.Runtime.NpcAgent, Reloader.NPCs");
                Assert.That(npcAgentType, Is.Not.Null, "Expected NPC runtime assembly to expose NpcAgent for shot-cam linger classification.");
                target.AddComponent(npcAgentType!);
                target.AddComponent<TestDamageable>();

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = new AmmoBallisticSnapshot(
                    AmmoSourceType.Factory,
                    muzzleVelocityFps: 10000f,
                    velocityStdDevFps: 0f,
                    projectileMassGrains: 147f,
                    ballisticCoefficientG1: 0.45f,
                    dispersionMoa: 0f,
                    displayName: "ShotCamTestRound",
                    cartridgeId: "shotcam-test-round",
                    ammoItemId: "ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    chamberRound,
                    chamberRound,
                    chamberRound,
                    chamberRound
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                var lingerStartTime = Time.unscaledTime;
                var elapsed = 0f;
                while (shotCameraRuntime.IsShotActive && elapsed < 3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(shotCameraRuntime.IsShotActive, Is.False, "Expected NPC impact to restore after its linger window.");
                Assert.That(Time.unscaledTime - lingerStartTime, Is.GreaterThanOrEqualTo(1.9f), "Expected NPC hit linger to hold the camera at impact for two seconds.");
                Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Fire_WithShotCameraRuntime_HidesHudAndViewmodel_BlocksRefire_AndRestoresOnCancel()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            GameObject uiRootGo = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                var viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();
                viewmodelCamera.enabled = true;

                uiRootGo = new GameObject("UiToolkitRuntimeRoot");
                var beltHudDocument = CreateRuntimeHudDocument("belt-hud", uiRootGo.transform);
                var compassHudDocument = CreateRuntimeHudDocument("compass-hud", uiRootGo.transform);
                var ammoHudDocument = CreateRuntimeHudDocument("ammo-hud", uiRootGo.transform);
                var interactionHintDocument = CreateRuntimeHudDocument("interaction-hint", uiRootGo.transform);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 180f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;
                yield return null;

                Assert.That(viewmodelCamera.enabled, Is.False, "Expected shot cam to suppress the viewmodel camera for a clean cinematic frame.");
                Assert.That(IsDocumentVisible(beltHudDocument), Is.False, "Expected shot cam to hide HUD documents for a clean screen.");
                Assert.That(IsDocumentVisible(compassHudDocument), Is.False, "Expected shot cam to hide HUD documents for a clean screen.");
                Assert.That(IsDocumentVisible(ammoHudDocument), Is.False, "Expected shot cam to hide HUD documents for a clean screen.");
                Assert.That(IsDocumentVisible(interactionHintDocument), Is.False, "Expected shot cam to hide HUD documents for a clean screen.");

                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length, Is.EqualTo(1),
                    "Expected shot cam to block firing additional shots while the cinematic is active.");

                input.ShotCameraCancelPressedThisFrame = true;
                yield return null;
                yield return null;

                Assert.That(viewmodelCamera.enabled, Is.True, "Expected the viewmodel camera to restore after canceling shot cam.");
                Assert.That(IsDocumentVisible(beltHudDocument), Is.True, "Expected belt HUD to restore after shot cam ends.");
                Assert.That(IsDocumentVisible(compassHudDocument), Is.True, "Expected compass HUD to restore after shot cam ends.");
                Assert.That(IsDocumentVisible(ammoHudDocument), Is.True, "Expected ammo HUD to restore after shot cam ends.");
                Assert.That(IsDocumentVisible(interactionHintDocument), Is.True, "Expected interaction hints to restore after shot cam ends.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (uiRootGo != null)
                {
                    Object.Destroy(uiRootGo);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_LookInput_OrbitsRenderCameraAroundProjectile()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject projectileGo = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("ShotCameraRuntimeRoot");
                var input = root.AddComponent<TestInputSource>();
                var runtime = root.AddComponent<ShotCameraRuntime>();
                runtime.Configure(input, new ShotCameraSettings(true, 100f, 0.1f, 0.25f));

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                projectileGo = new GameObject("Projectile");
                projectileGo.transform.position = new Vector3(0f, 1000f, 0f);
                projectileGo.transform.forward = Vector3.forward;
                var projectile = projectileGo.AddComponent<WeaponProjectile>();
                projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 0f, gravityMultiplier: 0f, damage: 10f);

                var request = new ShotCameraRequest(
                    projectile,
                    projectileGo.transform.position,
                    projectileGo.transform.position + (Vector3.forward * 180f),
                    180f,
                    new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                Assert.That(runtime.TryRegisterShot(request), Is.True);
                yield return null;

                var baselinePosition = worldCamera.transform.position;
                var baselineRotation = worldCamera.transform.rotation;
                var shotRenderCamera = FindShotRenderCamera();
                Assert.That(shotRenderCamera, Is.Not.Null, "Expected shot cam registration to create a temporary render camera.");
                var cinematicBaselinePosition = shotRenderCamera.transform.position;
                var cinematicBaselineRotation = shotRenderCamera.transform.rotation;

                input.LookInputValue = new Vector2(25f, -10f);
                yield return null;
                input.LookInputValue = Vector2.zero;
                yield return null;
                yield return null;

                Assert.That(Vector3.Distance(worldCamera.transform.position, baselinePosition), Is.LessThan(0.001f),
                    "Expected shot cam look input not to move the gameplay MainCamera.");
                Assert.That(Quaternion.Angle(worldCamera.transform.rotation, baselineRotation), Is.LessThan(0.01f),
                    "Expected shot cam look input not to rotate the gameplay MainCamera.");
                Assert.That(Vector3.Distance(shotRenderCamera.transform.position, cinematicBaselinePosition), Is.GreaterThan(0.05f),
                    "Expected shot cam look input to orbit the render camera around the projectile.");
                Assert.That(Quaternion.Angle(shotRenderCamera.transform.rotation, cinematicBaselineRotation), Is.GreaterThan(0.5f),
                    "Expected shot cam look input to update the temporary render camera orientation.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (projectileGo != null)
                {
                    Object.Destroy(projectileGo);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }
            }
        }

        [Test]
        public void ShotCameraRuntime_DisableWithoutActiveShot_DoesNotOverwriteGlobalTimeState()
        {
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;
            var root = new GameObject("ShotCameraRuntimeRoot");

            try
            {
                var runtime = root.AddComponent<ShotCameraRuntime>();
                Time.timeScale = 0.65f;
                Time.fixedDeltaTime = 0.015f;

                runtime.enabled = false;

                Assert.That(Time.timeScale, Is.EqualTo(0.65f).Within(0.0001f),
                    "Expected disabling an idle shot-cam runtime to leave the current global time scale unchanged.");
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(0.015f).Within(0.0001f),
                    "Expected disabling an idle shot-cam runtime to preserve the existing global fixed timestep.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShotCameraRuntime_DisablingIdleRuntime_DoesNotClearAnotherActiveShotCameraState()
        {
            var activeRoot = new GameObject("ActiveShotCameraRuntimeRoot");
            var idleRoot = new GameObject("IdleShotCameraRuntimeRoot");

            try
            {
                var activeRuntime = activeRoot.AddComponent<ShotCameraRuntime>();
                var idleRuntime = idleRoot.AddComponent<ShotCameraRuntime>();

                ShotCameraGameplayState.PushActive();
                Assert.That(ShotCameraGameplayState.IsActive, Is.True);

                idleRuntime.enabled = false;

                Assert.That(ShotCameraGameplayState.IsActive, Is.True,
                    "Expected disabling an idle shot-cam runtime to preserve the shared gameplay-state lock owned by another active runtime.");

                ShotCameraGameplayState.PopActive();
                Assert.That(ShotCameraGameplayState.IsActive, Is.False);
            }
            finally
            {
                ShotCameraGameplayState.Reset();
                Object.DestroyImmediate(activeRoot);
                Object.DestroyImmediate(idleRoot);
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_Cancel_RestoresRenderCameraToPlayerViewWithoutDefaultGameplayCinemachine()
        {
            GameObject root = null;
            GameObject pivotGo = null;
            GameObject lookTargetGo = null;
            GameObject cameraGo = null;
            GameObject projectileGo = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("ShotCameraRuntimeRoot");
                var input = root.AddComponent<TestInputSource>();
                var runtime = root.AddComponent<ShotCameraRuntime>();
                var defaults = root.AddComponent<PlayerCameraDefaults>();

                pivotGo = new GameObject("CameraPivot");
                pivotGo.transform.SetParent(root.transform, false);
                pivotGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);

                lookTargetGo = new GameObject("CameraLookTarget");
                lookTargetGo.transform.SetParent(pivotGo.transform, false);
                lookTargetGo.transform.localPosition = Vector3.forward * 10f;

                cameraGo = new GameObject("WorldCamera");
                cameraGo.transform.SetParent(pivotGo.transform, false);
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";

                SetField(typeof(PlayerCameraDefaults), defaults, "_mainCamera", worldCamera);
                SetField(typeof(PlayerCameraDefaults), defaults, "_cameraFollowTarget", pivotGo.transform);
                SetField(typeof(PlayerCameraDefaults), defaults, "_cameraLookTarget", lookTargetGo.transform);
                defaults.ApplyDefaults();
                var gameplayBrain = worldCamera.GetComponent<CinemachineBrain>();
                Assert.That(gameplayBrain, Is.Not.Null);

                runtime.Configure(input, new ShotCameraSettings(true, 100f, 0.1f, 0.25f));

                projectileGo = new GameObject("Projectile");
                projectileGo.transform.position = new Vector3(0f, 1000f, 0f);
                projectileGo.transform.forward = Vector3.forward;
                var projectile = projectileGo.AddComponent<WeaponProjectile>();
                projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 0f, gravityMultiplier: 0f, damage: 10f);

                var request = new ShotCameraRequest(
                    projectile,
                    projectileGo.transform.position,
                    projectileGo.transform.position + (Vector3.forward * 180f),
                    180f,
                    new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                Assert.That(runtime.TryRegisterShot(request), Is.True);
                yield return null;
                Assert.That(defaults.TryGetPresentationCamera(out var activePresentationCamera), Is.True);
                Assert.That(activePresentationCamera, Is.SameAs(FindShotRenderCamera()), "Expected the player camera defaults contract to surface the temporary shot-camera render view while the cinematic is active.");

                input.ShotCameraCancelPressedThisFrame = true;
                yield return null;
                yield return null;

                Assert.That(Vector3.Distance(worldCamera.transform.position, pivotGo.transform.position), Is.LessThan(0.05f),
                    "Expected shot-cam exit to restore the render camera to the player's camera pivot instead of leaving it at the impact view.");
                Assert.That(Quaternion.Angle(worldCamera.transform.rotation, pivotGo.transform.rotation), Is.LessThan(0.5f),
                    "Expected shot-cam exit to restore the render camera orientation back to the player view.");
                Assert.That(gameplayBrain.enabled, Is.True, "Expected the gameplay CinemachineBrain to remain enabled after shot-cam cancel.");
                Assert.That(FindShotRenderCamera(), Is.Null, "Expected shot-cam cancel to remove the temporary render camera.");
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.Null, "Expected shot-cam cancel to clear the current presentation camera.");
                Assert.That(defaults.TryGetPresentationCamera(out var restoredPresentationCamera), Is.True);
                Assert.That(restoredPresentationCamera, Is.SameAs(worldCamera), "Expected the player camera defaults contract to fall back to the gameplay camera after shot-cam ends.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (projectileGo != null)
                {
                    Object.Destroy(projectileGo);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (pivotGo != null)
                {
                    Object.Destroy(pivotGo);
                }

                if (lookTargetGo != null)
                {
                    Object.Destroy(lookTargetGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_ImpactExit_RestoresRenderCameraToPlayerView()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                var pivotGo = new GameObject("CameraPivot");
                pivotGo.transform.SetParent(root.transform, false);
                pivotGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                var lookTargetGo = new GameObject("CameraLookTarget");
                lookTargetGo.transform.SetParent(pivotGo.transform, false);
                lookTargetGo.transform.localPosition = Vector3.forward * 10f;

                cameraGo = new GameObject("WorldCamera");
                cameraGo.transform.SetParent(pivotGo.transform, false);
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";

                var cameraDefaults = root.AddComponent<PlayerCameraDefaults>();
                SetField(typeof(PlayerCameraDefaults), cameraDefaults, "_mainCamera", worldCamera);
                SetField(typeof(PlayerCameraDefaults), cameraDefaults, "_cameraFollowTarget", pivotGo.transform);
                SetField(typeof(PlayerCameraDefaults), cameraDefaults, "_cameraLookTarget", lookTargetGo.transform);
                cameraDefaults.ApplyDefaults();
                var gameplayBrain = worldCamera.GetComponent<CinemachineBrain>();
                Assert.That(gameplayBrain, Is.Not.Null);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 180f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_cameraDefaults", cameraDefaults);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;
                Assert.That(cameraDefaults.TryGetPresentationCamera(out var activePresentationCamera), Is.True);
                Assert.That(activePresentationCamera, Is.SameAs(FindShotRenderCamera()), "Expected the player camera defaults contract to surface the temporary shot-camera render view while the cinematic is active.");

                var exitElapsed = 0f;
                while (shotCameraRuntime.IsShotActive && exitElapsed < 5f)
                {
                    exitElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(shotCameraRuntime.IsShotActive, Is.False, "Expected shot cam to auto-exit after projectile impact and linger.");
                Assert.That(Vector3.Distance(worldCamera.transform.position, pivotGo.transform.position), Is.LessThan(0.05f),
                    "Expected shot-cam impact exit to return the render camera to the player camera pivot.");
                Assert.That(Quaternion.Angle(worldCamera.transform.rotation, pivotGo.transform.rotation), Is.LessThan(0.5f),
                    "Expected shot-cam impact exit to restore the render camera orientation to the player view.");
                Assert.That(gameplayBrain.enabled, Is.True, "Expected the gameplay CinemachineBrain to remain enabled after shot-cam impact exit.");
                Assert.That(FindShotRenderCamera(), Is.Null, "Expected shot-cam impact exit to remove the temporary render camera.");
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.Null, "Expected shot-cam impact exit to clear the current presentation camera.");
                Assert.That(cameraDefaults.TryGetPresentationCamera(out var restoredPresentationCamera), Is.True);
                Assert.That(restoredPresentationCamera, Is.SameAs(worldCamera), "Expected the player camera defaults contract to fall back to the gameplay camera after shot-cam impact exit.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_ProjectileImpactOnNpc_LingersBeforeRestore()
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject registryGo = null;
            GameObject target = null;
            WeaponDefinition definition = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.transform.SetPositionAndRotation(root.transform.position, Quaternion.identity);
                worldCamera.tag = "MainCamera";

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = root.transform.position + (Vector3.forward * 110f);
                target.transform.localScale = new Vector3(20f, 20f, 2f);
                var damageable = target.AddComponent<TestDamageable>();
                var npcAgentType = Type.GetType("Reloader.NPCs.Runtime.NpcAgent, Reloader.NPCs");
                Assert.That(npcAgentType, Is.Not.Null, "Expected NPC runtime assembly to expose NpcAgent for shot-cam linger classification.");
                target.AddComponent(npcAgentType!);

                var controller = root.AddComponent<PlayerWeaponController>();
                var shotCameraRuntime = root.AddComponent<ShotCameraRuntime>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_shotCameraRuntimeBehaviour", shotCameraRuntime);
                SetControllerField(controller, "_shotCameraSettings", new ShotCameraSettings(true, 100f, 0.1f, 0.25f));
                yield return null;

                var chamberRound = new AmmoBallisticSnapshot(
                    AmmoSourceType.Factory,
                    muzzleVelocityFps: 10000f,
                    velocityStdDevFps: 0f,
                    projectileMassGrains: 147f,
                    ballisticCoefficientG1: 0.45f,
                    dispersionMoa: 0f,
                    displayName: "ShotCamTestRound",
                    cartridgeId: "shotcam-test-round",
                    ammoItemId: "ammo-factory-308-147-fmj");
                var magazineRounds = new[] { chamberRound, chamberRound, chamberRound, chamberRound };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                input.AimHeldValue = true;
                input.FirePressedThisFrame = true;
                yield return null;

                var elapsed = 0f;
                while (damageable.HitCount == 0 && elapsed < 1f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(damageable.HitCount, Is.EqualTo(1), "Expected projectile to hit the NPC test target.");

                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(shotCameraRuntime.IsShotActive, Is.True,
                    "Expected NPC impacts to keep the shot camera active during the longer linger window.");

                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(shotCameraRuntime.IsShotActive, Is.False,
                    "Expected the NPC-impact linger window to end and restore the player camera.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                if (target != null)
                {
                    Object.Destroy(target);
                }

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Configure_InjectedWeaponEvents_PropagatesInjectedChannelToSpawnedProjectile()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;

            try
            {
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                registry.SetDefinitionsForTests(new[] { definition });

                var injectedEvents = new DefaultRuntimeEvents();

                root.AddComponent<PlayerWeaponController>().Configure(weaponEvents: injectedEvents, inventoryEvents: injectedEvents);

                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;

                var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
                Assert.That(projectile, Is.Not.Null);

                var useRuntimeField = typeof(WeaponProjectile).GetField("_useRuntimeKernelWeaponEvents", BindingFlags.Instance | BindingFlags.NonPublic);
                var projectileEventsField = typeof(WeaponProjectile).GetField("_weaponEvents", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(useRuntimeField, Is.Not.Null);
                Assert.That(projectileEventsField, Is.Not.Null);

                var useRuntimeKernelWeaponEvents = (bool)useRuntimeField.GetValue(projectile);
                var projectileEvents = projectileEventsField.GetValue(projectile);
                Assert.That(useRuntimeKernelWeaponEvents, Is.False);
                Assert.That(projectileEvents, Is.SameAs(injectedEvents));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Configure_InjectedWeaponEvents_ProjectileHitRaisesOnlyInjectedChannel()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject target = null;
            WeaponProjectile projectile = null;
            var fallbackHandlersRegistered = false;

            var fallbackProjectileHitRaised = 0;
            void HandleFallbackProjectileHit(string _, Vector3 __, float ___) => fallbackProjectileHitRaised++;

            try
            {
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 120f, 0f, 20f, 10000f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = new Vector3(0f, 1000f, 20f);
                target.transform.localScale = new Vector3(40f, 40f, 8f);

                var injectedEvents = new DefaultRuntimeEvents();
                var injectedProjectileHitRaised = 0;
                injectedEvents.OnProjectileHit += (_, _, _) => injectedProjectileHitRaised++;

                fallbackRuntimeEvents.OnProjectileHit += HandleFallbackProjectileHit;
                fallbackHandlersRegistered = true;

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                controller.Configure(weaponEvents: injectedEvents, inventoryEvents: injectedEvents);
                yield return null;

                var injectedWeaponFiredRaised = 0;
                injectedEvents.OnWeaponFired += (_, _, _) => injectedWeaponFiredRaised++;

                Random.InitState(1337);
                input.FirePressedThisFrame = true;
                yield return null;

                var elapsed = 0f;
                while (injectedProjectileHitRaised == 0 && elapsed < 2f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                projectile = Object.FindFirstObjectByType<WeaponProjectile>();

                Assert.That(injectedWeaponFiredRaised, Is.EqualTo(1), "Injected weapon fire event was not raised.");
                Assert.That(injectedProjectileHitRaised, Is.EqualTo(1), "Injected projectile hit event was not raised.");
                Assert.That(fallbackProjectileHitRaised, Is.EqualTo(0), "Fallback runtime projectile hit channel should stay silent.");
            }
            finally
            {
                if (fallbackHandlersRegistered)
                {
                    fallbackRuntimeEvents.OnProjectileHit -= HandleFallbackProjectileHit;
                }

                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (projectile != null)
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (target != null)
                {
                    Object.Destroy(target);
                }

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Configure_WithoutInjectedPorts_UsesCurrentRuntimeHubAfterSwap()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var initialRuntimeEvents = new DefaultRuntimeEvents();
            var replacementRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialRuntimeEvents;

            var initialFiredRaised = 0;
            var replacementFiredRaised = 0;
            initialRuntimeEvents.OnWeaponFired += (_, _, _) => initialFiredRaised++;
            replacementRuntimeEvents.OnWeaponFired += (_, _, _) => replacementFiredRaised++;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            controller.Configure();
            yield return null;

            RuntimeKernelBootstrapper.Events = replacementRuntimeEvents;
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(initialFiredRaised, Is.EqualTo(0));
            Assert.That(replacementFiredRaised, Is.EqualTo(1));

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_WhenMenuIsOpen_IsBlockedAndDoesNotCarryOverAfterClose()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var firedRaised = 0;
            runtimeEvents.OnWeaponFired += (_, _, _) => firedRaised++;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var cursorController = root.AddComponent<PlayerCursorLockController>();
            cursorController.LockCursor();

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            controller.Configure();
            yield return null;

            runtimeEvents.RaiseTabInventoryVisibilityChanged(true);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Weapon fire should be blocked while a menu is open.");

            runtimeEvents.RaiseTabInventoryVisibilityChanged(false);
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Blocked fire input should be consumed instead of firing after the menu closes.");

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(1), "Fire should resume once the menu has closed.");

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_WhenGameplayInputIsForcedBlocked_IsBlockedAndDoesNotCarryOverAfterRelease()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var firedRaised = 0;
            runtimeEvents.OnWeaponFired += (_, _, _) => firedRaised++;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var cursorController = root.AddComponent<PlayerCursorLockController>();
            cursorController.LockCursor();

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            controller.Configure();
            yield return null;

            cursorController.SetForcedCursorUnlock(true);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Weapon fire should be blocked while dialogue-style forced input blocking is active.");

            cursorController.SetForcedCursorUnlock(false);
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Blocked fire input should be consumed instead of firing after forced blocking ends.");

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(1), "Fire should resume once forced input blocking ends.");

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Aiming_LerpsFieldOfViewToPackAdsFovAndBack()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                "weapon-kar98k",
                "Scoped Rifle",
                5,
                0.1f,
                80f,
                0f,
                20f,
                120f,
                1,
                10,
                true,
                0.7f,
                WeaponScopeConfiguration.Create(true, 4f, 20f, 8f, "ebr-7c", 100, 25));
            registry.SetDefinitionsForTests(new[] { definition });

            var adsCameraGo = new GameObject("AdsCamera");
            var adsCamera = adsCameraGo.AddComponent<Camera>();
            const float baseFieldOfView = 60f;
            adsCamera.fieldOfView = baseFieldOfView;
            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", adsCamera);
            yield return null;

            const float expectedAdsFieldOfView = 45f;
            input.AimHeldValue = true;
            yield return null;
            yield return new WaitForSeconds(0.45f);

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(expectedAdsFieldOfView).Within(0.6f));

            input.AimHeldValue = false;
            yield return null;
            yield return new WaitForSeconds(0.45f);

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(baseFieldOfView).Within(0.6f));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(adsCameraGo);
        }

        [UnityTest]
        public IEnumerator UnequipWhileAiming_RestoresBaselineFieldOfView()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.BeltSlotItemIds[1] = null;
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                "weapon-kar98k",
                "Scoped Rifle",
                5,
                0.1f,
                80f,
                0f,
                20f,
                120f,
                1,
                10,
                true,
                0.7f,
                WeaponScopeConfiguration.Create(true, 4f, 20f, 8f, "ebr-7c", 100, 25));
            registry.SetDefinitionsForTests(new[] { definition });

            var adsCameraGo = new GameObject("AdsCamera");
            var adsCamera = adsCameraGo.AddComponent<Camera>();
            const float baseFieldOfView = 60f;
            adsCamera.fieldOfView = baseFieldOfView;

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", adsCamera);
            yield return null;

            input.AimHeldValue = true;
            yield return null;
            yield return new WaitForSeconds(0.25f);
            Assert.That(adsCamera.fieldOfView, Is.LessThan(baseFieldOfView - 1f), "Expected ADS to lower FOV before unequip.");

            runtime.SelectBeltSlot(1);
            yield return null;
            yield return new WaitForSeconds(0.15f);

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(baseFieldOfView).Within(0.6f), "Unequipping while ADS should restore baseline FOV.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(adsCameraGo);
        }

        [UnityTest]
        public IEnumerator UnarmedState_PreservesExternalFieldOfViewChanges()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.BeltSlotItemIds[1] = null;
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                "weapon-kar98k",
                "Scoped Rifle",
                5,
                0.1f,
                80f,
                0f,
                20f,
                120f,
                1,
                10,
                true,
                0.7f,
                WeaponScopeConfiguration.Create(true, 4f, 20f, 8f, "ebr-7c", 100, 25));
            registry.SetDefinitionsForTests(new[] { definition });

            var adsCameraGo = new GameObject("AdsCamera");
            var adsCamera = adsCameraGo.AddComponent<Camera>();
            const float baseFieldOfView = 60f;
            adsCamera.fieldOfView = baseFieldOfView;

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", adsCamera);
            yield return null;

            input.AimHeldValue = true;
            yield return null;
            yield return new WaitForSeconds(0.25f);

            runtime.SelectBeltSlot(1);
            yield return null;
            yield return new WaitForSeconds(0.15f);
            Assert.That(adsCamera.fieldOfView, Is.EqualTo(baseFieldOfView).Within(0.6f));

            const float externalFov = 72f;
            adsCamera.fieldOfView = externalFov;
            yield return null;
            yield return null;

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(externalFov).Within(0.2f), "Unarmed flow should not overwrite externally applied FOV.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(adsCameraGo);
        }

        [UnityTest]
        public IEnumerator BeltSelectedWeapon_EquipsAndFiresAndReloads()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);

            string equipped = null;
            string fired = null;
            var firedCount = 0;
            string reloaded = null;
            runtimeEvents.OnWeaponEquipped += OnEquipped;
            runtimeEvents.OnWeaponFired += OnFired;
            runtimeEvents.OnWeaponReloaded += OnReloaded;

            void OnEquipped(string itemId) => equipped = itemId;
            void OnFired(string itemId, Vector3 _, Vector3 __)
            {
                fired = itemId;
                firedCount++;
            }
            void OnReloaded(string itemId, int _, int __) => reloaded = itemId;

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(equipped, Is.EqualTo("weapon-kar98k"));
            Assert.That(fired, Is.EqualTo("weapon-kar98k"));
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterFire), Is.True);
            Assert.That(stateAfterFire.MagazineCount, Is.EqualTo(0));

            input.ReloadPressedThisFrame = true;
            yield return null;
            yield return new WaitForSeconds(0.36f);

            Assert.That(reloaded, Is.EqualTo("weapon-kar98k"));
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterReload), Is.True);
            Assert.That(stateAfterReload.MagazineCount, Is.EqualTo(5));
            Assert.That(stateAfterReload.ReserveCount, Is.EqualTo(runtime.GetItemQuantity("ammo-factory-308-147-fmj")));

            yield return new WaitForSeconds(0.11f);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedCount, Is.EqualTo(2));
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterReloadFire), Is.True);
            Assert.That(stateAfterReloadFire.ChamberLoaded, Is.True);
            Assert.That(stateAfterReloadFire.MagazineCount, Is.EqualTo(4));

            runtimeEvents.OnWeaponEquipped -= OnEquipped;
            runtimeEvents.OnWeaponFired -= OnFired;
            runtimeEvents.OnWeaponReloaded -= OnReloaded;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingLocalInputSource_StillEquips_FromSceneInputProvider()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();

            var inputGo = new GameObject("InputSource");
            var input = inputGo.AddComponent<TestInputSource>();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);

            string equipped = null;
            runtimeEvents.OnWeaponEquipped += OnEquipped;
            void OnEquipped(string itemId) => equipped = itemId;

            yield return null;

            Assert.That(equipped, Is.EqualTo("weapon-kar98k"));

            runtimeEvents.OnWeaponEquipped -= OnEquipped;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(inputGo);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MultipleRegistries_WhenAssignedRegistryMissesSelectedItem_DoesNotRescueFromOtherSceneRegistry()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var staleRegistryGo = new GameObject("StaleRegistry");
            var staleRegistry = staleRegistryGo.AddComponent<WeaponRegistry>();
            var staleDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();
            staleDefinition.SetRuntimeValuesForTests("weapon-other-99", "Other", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            staleRegistry.SetDefinitionsForTests(new[] { staleDefinition });

            var activeRegistryGo = new GameObject("ActiveRegistry");
            var activeRegistry = activeRegistryGo.AddComponent<WeaponRegistry>();
            var activeDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();
            activeDefinition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            activeRegistry.SetDefinitionsForTests(new[] { activeDefinition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", staleRegistry);

            var firedCount = 0;
            runtimeEvents.OnWeaponFired += HandleWeaponFired;
            void HandleWeaponFired(string _, Vector3 __, Vector3 ___) => firedCount++;

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedCount, Is.EqualTo(0));
            Assert.That(controller.EquippedItemId, Is.Empty);
            Assert.That(GetControllerField<WeaponRegistry>(controller, "_weaponRegistry"), Is.SameAs(staleRegistry));

            runtimeEvents.OnWeaponFired -= HandleWeaponFired;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(staleRegistryGo);
            Object.Destroy(activeRegistryGo);
            Object.Destroy(staleDefinition);
            Object.Destroy(activeDefinition);
        }

        [UnityTest]
        public IEnumerator ReloadCompletion_FromRuntimeAppliedLoadout_ConsumesAmmoAndRaisesEvents()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 0, false);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            var inventoryChangedCount = 0;
            var reloadEventCount = 0;
            string reloadedItemId = null;
            runtimeEvents.OnInventoryChanged += OnInventoryChanged;
            runtimeEvents.OnWeaponReloaded += OnWeaponReloaded;
            void OnInventoryChanged() => inventoryChangedCount++;
            void OnWeaponReloaded(string itemId, int _, int __)
            {
                reloadEventCount++;
                reloadedItemId = itemId;
            }

            yield return null;

            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 0, 0, false), Is.True);
            Assert.That(
                controller.ApplyRuntimeBallistics(
                    "weapon-kar98k",
                    null,
                    new[]
                    {
                        new AmmoBallisticSnapshot(
                            AmmoSourceType.Factory,
                            2780f,
                            55f,
                            147f,
                            0.398f,
                            4.5f,
                            "Factory .308 147gr FMJ",
                            "runtime-pack-round",
                            "ammo-factory-308-147-fmj")
                    }),
                Is.True);

            var quantityBeforeReload = runtime.GetItemQuantity("ammo-factory-308-147-fmj");
            input.ReloadPressedThisFrame = true;
            yield return null;
            yield return new WaitForSeconds(0.36f);

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterReload), Is.True);
            Assert.That(stateAfterReload.ChamberLoaded, Is.True);
            Assert.That(stateAfterReload.MagazineCount, Is.GreaterThan(0));
            Assert.That(runtime.GetItemQuantity("ammo-factory-308-147-fmj"), Is.LessThan(quantityBeforeReload));
            Assert.That(inventoryChangedCount, Is.EqualTo(1));
            Assert.That(reloadEventCount, Is.EqualTo(1));
            Assert.That(reloadedItemId, Is.EqualTo("weapon-kar98k"));

            runtimeEvents.OnInventoryChanged -= OnInventoryChanged;
            runtimeEvents.OnWeaponReloaded -= OnWeaponReloaded;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingCoreDependencies_LogsErrorOnce_AndSkipsTick()
        {
            var root = new GameObject("PlayerRoot");
            root.AddComponent<PlayerWeaponController>();

            LogAssert.Expect(LogType.Error, "PlayerWeaponController requires PlayerInventoryController and WeaponRegistry references.");
            yield return null;
            yield return null;

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ApplyRuntimePayload_InvalidCountsAndLoadout_AreNormalized()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(input, resolver, new PlayerInventoryRuntime());

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 0, false);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 999, -5, true), Is.True);
            Assert.That(
                controller.ApplyRuntimeBallistics(
                    "weapon-kar98k",
                    null,
                    new[]
                    {
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c1", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c2", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c3", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c4", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c5", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c6", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c7", "ammo-factory-308-147-fmj")
                    }),
                Is.True);

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            Assert.That(state.MagazineCount, Is.EqualTo(5));
            Assert.That(state.GetMagazineRoundsSnapshot().Count, Is.EqualTo(5));
            Assert.That(state.ReserveCount, Is.EqualTo(0));
            Assert.That(state.ChamberLoaded, Is.True);
            Assert.That(state.ChamberRound.HasValue, Is.True);

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingProjectilePrefab_StillSpawnsRuntimeProjectile()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            yield return null;

            var before = Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length;
            input.FirePressedThisFrame = true;
            yield return null;

            var after = Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length;
            Assert.That(after, Is.GreaterThan(before));

            foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
            {
                Object.Destroy(projectile.gameObject);
            }

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_UsesAmmoSnapshotVelocityAndProjectileMass_NotWeaponDefaults()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 10f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            var chamberRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 3000f, 0f, 168f, 0.46f, 0f);
            state.SetAmmoLoadoutForTests(chamberRound, System.Array.Empty<AmmoBallisticSnapshot>());

            input.FirePressedThisFrame = true;
            yield return null;

            var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.InitialSpeedMetersPerSecond, Is.EqualTo(914.4f).Within(1f));
            Assert.That(projectile.ProjectileMassGrains, Is.EqualTo(168f).Within(0.001f));

            if (projectile != null)
            {
                Object.Destroy(projectile.gameObject);
            }

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_RaisesWeaponFiredWithActualDispersedDirection()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 10f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            var chamberRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 3000f, 0f, 168f, 0.46f, 12f);
            state.SetAmmoLoadoutForTests(chamberRound, System.Array.Empty<AmmoBallisticSnapshot>());

            var firedDirection = Vector3.zero;
            var firedDirectionCaptured = false;
            runtimeEvents.OnWeaponFired += HandleWeaponFired;
            void HandleWeaponFired(string _, Vector3 __, Vector3 direction)
            {
                firedDirection = direction;
                firedDirectionCaptured = true;
            }

            Random.InitState(1337);
            input.FirePressedThisFrame = true;
            yield return null;

            var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
            Assert.That(projectile, Is.Not.Null);
            Assert.That(firedDirectionCaptured, Is.True);
            Assert.That(Vector3.Dot(firedDirection.normalized, projectile.transform.forward), Is.GreaterThan(0.9999f));

            runtimeEvents.OnWeaponFired -= HandleWeaponFired;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            if (projectile != null)
            {
                Object.Destroy(projectile.gameObject);
            }

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator ReloadStart_ThenSprint_CancelsReload()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 10, false);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string startedItemId = null;
            string cancelledItemId = null;
            var cancelledReason = WeaponReloadCancelReason.DryStateInvalidated;
            runtimeEvents.OnWeaponReloadStarted += HandleStarted;
            runtimeEvents.OnWeaponReloadCancelled += HandleCancelled;
            void HandleStarted(string itemId) => startedItemId = itemId;
            void HandleCancelled(string itemId, WeaponReloadCancelReason reason)
            {
                cancelledItemId = itemId;
                cancelledReason = reason;
            }

            yield return null;

            input.ReloadPressedThisFrame = true;
            yield return null;

            input.SprintHeldValue = true;
            yield return null;

            Assert.That(startedItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(cancelledItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(cancelledReason, Is.EqualTo(WeaponReloadCancelReason.Sprint));

            runtimeEvents.OnWeaponReloadStarted -= HandleStarted;
            runtimeEvents.OnWeaponReloadCancelled -= HandleCancelled;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator AimingStateChange_RaisesWeaponAimChangedEvent()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string eventItemId = null;
            var isAiming = false;
            var raised = false;
            runtimeEvents.OnWeaponAimChanged += HandleAimChanged;
            void HandleAimChanged(string itemId, bool value)
            {
                eventItemId = itemId;
                isAiming = value;
                raised = true;
            }

            yield return null;

            input.AimHeldValue = true;
            yield return null;

            Assert.That(raised, Is.True);
            Assert.That(eventItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(isAiming, Is.True);

            runtimeEvents.OnWeaponAimChanged -= HandleAimChanged;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator ReloadingThenUnequip_CancelsReloadWithUnequipReason()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 10, false);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string cancelledItemId = null;
            var cancelledReason = WeaponReloadCancelReason.DryStateInvalidated;
            runtimeEvents.OnWeaponReloadCancelled += HandleCancelled;
            void HandleCancelled(string itemId, WeaponReloadCancelReason reason)
            {
                cancelledItemId = itemId;
                cancelledReason = reason;
            }

            yield return null;

            input.ReloadPressedThisFrame = true;
            yield return null;

            runtime.SelectBeltSlot(1);
            yield return null;

            Assert.That(cancelledItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(cancelledReason, Is.EqualTo(WeaponReloadCancelReason.Unequip));

            runtimeEvents.OnWeaponReloadCancelled -= HandleCancelled;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Equip_WithoutExplicitViewBindings_DoesNotSpawnWeaponViewFallback()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var viewPrefab = new GameObject("FallbackWeaponViewPrefab");
            GameObject registryGo = null;
            WeaponDefinition definition = null;

            try
            {
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_weaponViewParent", root.transform);
                yield return null;

                var equippedView = root.transform.Find("EquippedView_weapon-kar98k");
                Assert.That(equippedView, Is.Null, "Weapon view spawn should fail loudly when explicit bindings are missing.");
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator SameItemSelection_WhenViewSpawnFails_DoesNotRestartEquipCycle()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var equipStartedCount = 0;
            var unequipStartedCount = 0;
            runtimeEvents.OnWeaponEquipStarted += HandleEquipStarted;
            runtimeEvents.OnWeaponUnequipStarted += HandleUnequipStarted;
            void HandleEquipStarted(string _) => equipStartedCount++;
            void HandleUnequipStarted(string _) => unequipStartedCount++;

            root.AddComponent<PlayerWeaponController>();
            yield return null;
            yield return new WaitForSeconds(0.2f);

            Assert.That(equipStartedCount, Is.EqualTo(1), "Same-item selection with missing view should not restart equip flow.");
            Assert.That(unequipStartedCount, Is.EqualTo(0), "Same-item selection with missing view should not trigger holster churn.");

            runtimeEvents.OnWeaponEquipStarted -= HandleEquipStarted;
            runtimeEvents.OnWeaponUnequipStarted -= HandleUnequipStarted;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator SameItemSelection_AfterDisableEnable_RespawnsMissingViewWithoutRestartingEquipCycle()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var viewPrefab = new GameObject("ReenableWeaponViewPrefab");
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            var equipStartedCount = 0;
            var unequipStartedCount = 0;
            var handlersRegistered = false;
            void HandleEquipStarted(string _) => equipStartedCount++;
            void HandleUnequipStarted(string _) => unequipStartedCount++;

            try
            {
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                runtimeEvents.OnWeaponEquipStarted += HandleEquipStarted;
                runtimeEvents.OnWeaponUnequipStarted += HandleUnequipStarted;
                handlersRegistered = true;

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_weaponViewParent", root.transform);
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);
                yield return null;

                var viewName = "EquippedView_weapon-kar98k";
                Assert.That(root.transform.Find(viewName), Is.Not.Null, "Initial equip should spawn the bound weapon view.");
                Assert.That(equipStartedCount, Is.EqualTo(1));
                Assert.That(unequipStartedCount, Is.EqualTo(0));

                controller.enabled = false;
                yield return null;
                Assert.That(root.transform.Find(viewName), Is.Null, "Disabling should destroy the equipped view instance.");

                controller.enabled = true;
                yield return null;

                Assert.That(root.transform.Find(viewName), Is.Not.Null, "Re-enabling with the same selected item should respawn a missing equipped view.");
                Assert.That(equipStartedCount, Is.EqualTo(1), "View respawn should not restart equip flow for same item.");
                Assert.That(unequipStartedCount, Is.EqualTo(0), "View respawn should not trigger unequip flow.");

            }
            finally
            {
                if (handlersRegistered)
                {
                    runtimeEvents.OnWeaponEquipStarted -= HandleEquipStarted;
                    runtimeEvents.OnWeaponUnequipStarted -= HandleUnequipStarted;
                }

                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Equip_WithConfiguredWeaponViewParent_UsesConfiguredParentBeforeFallback()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual").transform;
            playerArmsVisual.SetParent(playerArms, false);
            playerArmsVisual.gameObject.AddComponent<Animator>();
            var fallbackParent = new GameObject("ik_hand_gun").transform;
            fallbackParent.SetParent(playerArmsVisual, false);

            var configuredParent = new GameObject("ConfiguredWeaponViewParent").transform;
            configuredParent.SetParent(root.transform, false);

            var viewPrefab = new GameObject("ConfiguredParentViewPrefab");
            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(iconPrefabField, Is.Not.Null);
            iconPrefabField.SetValue(definition, viewPrefab);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_weaponViewParent", configuredParent);
            SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);
            yield return null;

            Assert.That(configuredParent.Find("EquippedView_weapon-kar98k"), Is.Not.Null);
            Assert.That(fallbackParent.Find("EquippedView_weapon-kar98k"), Is.Null);

            Object.Destroy(root);
            Object.Destroy(viewPrefab);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator BeltSelectionChange_DuringHolsterDelay_UpdatesPendingEquipToLatestSelection()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.BeltSlotItemIds[1] = "weapon-rifle-02";
            runtime.BeltSlotItemIds[2] = "weapon-rifle-03";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definitionA = ScriptableObject.CreateInstance<WeaponDefinition>();
            definitionA.SetRuntimeValuesForTests("weapon-kar98k", "Rifle 01", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            var definitionB = ScriptableObject.CreateInstance<WeaponDefinition>();
            definitionB.SetRuntimeValuesForTests("weapon-rifle-02", "Rifle 02", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            var definitionC = ScriptableObject.CreateInstance<WeaponDefinition>();
            definitionC.SetRuntimeValuesForTests("weapon-rifle-03", "Rifle 03", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definitionA, definitionB, definitionC });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            runtime.SelectBeltSlot(1);
            yield return null;
            Assert.That(GetControllerField<string>(controller, "_pendingEquipItemId"), Is.EqualTo("weapon-rifle-02"));

            runtime.SelectBeltSlot(2);
            yield return null;
            Assert.That(GetControllerField<string>(controller, "_pendingEquipItemId"), Is.EqualTo("weapon-rifle-03"));

            yield return new WaitForSeconds(0.12f);
            Assert.That(controller.EquippedItemId, Is.EqualTo("weapon-rifle-03"));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definitionA);
            Object.Destroy(definitionB);
            Object.Destroy(definitionC);
        }

        [UnityTest]
        public IEnumerator Fire_WithoutSerializedCombatEmitter_AutoResolvesRuntimeEmitter()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_combatAudioEmitter", null);

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            var emitterField = typeof(PlayerWeaponController).GetField("_combatAudioEmitter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(emitterField, Is.Not.Null);
            var resolvedEmitter = emitterField.GetValue(controller) as WeaponCombatAudioEmitter;
            Assert.That(resolvedEmitter, Is.Not.Null);
            Assert.That(root.GetComponentInChildren<WeaponCombatAudioEmitter>(true), Is.SameAs(resolvedEmitter));
            var catalogField = typeof(WeaponCombatAudioEmitter).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(catalogField, Is.Not.Null);
            Assert.That(catalogField.GetValue(resolvedEmitter), Is.Not.Null, "Auto-resolved emitter should bind a default combat audio catalog.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_DiscoveredEmitterWithCustomCatalog_PreservesConfiguredCatalog()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var customCatalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            var emitterHost = new GameObject("EmitterHost");
            emitterHost.transform.SetParent(root.transform, false);
            var emitter = emitterHost.AddComponent<WeaponCombatAudioEmitter>();
            emitter.SetCatalog(customCatalog);

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_combatAudioEmitter", null);

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            var emitterField = typeof(PlayerWeaponController).GetField("_combatAudioEmitter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(emitterField, Is.Not.Null);
            var resolvedEmitter = emitterField.GetValue(controller) as WeaponCombatAudioEmitter;
            Assert.That(resolvedEmitter, Is.SameAs(emitter));

            var catalogField = typeof(WeaponCombatAudioEmitter).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(catalogField, Is.Not.Null);
            Assert.That(catalogField.GetValue(resolvedEmitter), Is.SameAs(customCatalog));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(customCatalog);
        }

        [UnityTest]
        public IEnumerator Spawn_WithViewMuzzleDefaultAttachment_DoesNotAutoEquipMuzzleRuntime()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            RuntimeKernelBootstrapper.Events = new DefaultRuntimeEvents();

            var runtimeType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object muzzleDefinition = null;
            GameObject muzzlePrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                var emitterSpy = root.AddComponent<ClipCaptureWeaponCombatAudioEmitter>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);

                viewPrefab = new GameObject("ViewPrefabWithMuzzleRuntime");
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(viewPrefab.transform, false);
                var slot = new GameObject("MuzzleAttachmentSlot").transform;
                slot.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, muzzleFirePoint: muzzle, muzzleSlot: slot);
                var runtimeComponent = viewPrefab.AddComponent(runtimeType);
                var muzzleSocketField = runtimeType.GetField("_muzzleSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                var attachmentSlotField = runtimeType.GetField("_attachmentSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(muzzleSocketField, Is.Not.Null);
                Assert.That(attachmentSlotField, Is.Not.Null);
                Assert.That(defaultAttachmentField, Is.Not.Null);

                muzzleDefinition = ScriptableObject.CreateInstance(definitionType);
                muzzlePrefab = new GameObject("MuzzleDevicePrefab");

                var definitionMuzzlePrefabField = definitionType.GetField("_muzzlePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(definitionMuzzlePrefabField, Is.Not.Null);
                definitionMuzzlePrefabField.SetValue(muzzleDefinition, muzzlePrefab);

                muzzleSocketField.SetValue(runtimeComponent, muzzle);
                attachmentSlotField.SetValue(runtimeComponent, slot);
                defaultAttachmentField.SetValue(runtimeComponent, muzzleDefinition);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);

                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_combatAudioEmitter", emitterSpy);
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);
                yield return null;

                var equippedViewField = typeof(PlayerWeaponController).GetField("_equippedWeaponView", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(equippedViewField, Is.Not.Null);
                var equippedView = equippedViewField.GetValue(controller) as GameObject;
                Assert.That(equippedView, Is.Not.Null);

                var bridgedRuntime = equippedView.GetComponent(runtimeType);
                Assert.That(bridgedRuntime, Is.Not.Null);

                var activeAttachmentField = runtimeType.GetField("_activeAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(activeAttachmentField, Is.Not.Null);
                Assert.That(activeAttachmentField.GetValue(bridgedRuntime), Is.Null, "Spawned weapon views should not auto-equip authored default muzzle attachments.");

                var bridgedSlotField = runtimeType.GetField("_attachmentSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bridgedSlotField, Is.Not.Null);
                var bridgedSlot = bridgedSlotField.GetValue(bridgedRuntime) as Transform;
                Assert.That(bridgedSlot, Is.Not.Null);
                Assert.That(bridgedSlot.childCount, Is.EqualTo(0), "Muzzle attachment slot should stay empty until runtime state explicitly equips a muzzle.");

                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(emitterSpy.LastFireOverrideClip, Is.Null, "Fire override should remain unset when no muzzle is explicitly equipped.");
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (muzzleDefinition != null)
                {
                    Object.Destroy(muzzleDefinition);
                }

                if (muzzlePrefab != null)
                {
                    Object.Destroy(muzzlePrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator Spawn_WithDetachableMagazineRuntimeDefaultAttachment_DoesNotAutoEquipAttachment()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.DetachableMagazineRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MagazineAttachmentDefinition");
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object magazineDefinition = null;
            GameObject magazineVisualPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);

                viewPrefab = new GameObject("ViewPrefabWithMagRuntime");
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(viewPrefab.transform, false);
                var magazineSocket = new GameObject("MagazineSocket").transform;
                magazineSocket.SetParent(viewPrefab.transform, false);
                var dropSocket = new GameObject("MagazineDropSocket").transform;
                dropSocket.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(
                    viewPrefab,
                    muzzleFirePoint: muzzle,
                    magazineSocket: magazineSocket,
                    magazineDropSocket: dropSocket);

                var runtimeComponent = viewPrefab.AddComponent(runtimeType);
                var magazineSocketField = runtimeType.GetField("_magazineSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                var dropSocketField = runtimeType.GetField("_magazineDropSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(magazineSocketField, Is.Not.Null);
                Assert.That(dropSocketField, Is.Not.Null);
                Assert.That(defaultAttachmentField, Is.Not.Null);

                magazineDefinition = ScriptableObject.CreateInstance(definitionType);
                magazineVisualPrefab = new GameObject("MagazineVisualPrefab");
                var definitionVisualField = definitionType.GetField("_magazineVisualPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                var spawnDroppedField = definitionType.GetField("_spawnDroppedMagazine", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(definitionVisualField, Is.Not.Null);
                Assert.That(spawnDroppedField, Is.Not.Null);
                definitionVisualField.SetValue(magazineDefinition, magazineVisualPrefab);
                spawnDroppedField.SetValue(magazineDefinition, false);

                magazineSocketField.SetValue(runtimeComponent, magazineSocket);
                dropSocketField.SetValue(runtimeComponent, dropSocket);
                defaultAttachmentField.SetValue(runtimeComponent, magazineDefinition);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);

                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);
                yield return null;

                var equippedViewField = typeof(PlayerWeaponController).GetField("_equippedWeaponView", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(equippedViewField, Is.Not.Null);
                var equippedView = equippedViewField.GetValue(controller) as GameObject;
                Assert.That(equippedView, Is.Not.Null);

                var bridgedRuntime = equippedView.GetComponent(runtimeType);
                Assert.That(bridgedRuntime, Is.Not.Null);

                var activeAttachmentField = runtimeType.GetField("_activeAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(activeAttachmentField, Is.Not.Null);
                Assert.That(activeAttachmentField.GetValue(bridgedRuntime), Is.Null, "Spawned weapon views should not auto-equip authored default magazine attachments.");

                var bridgedMagSocketField = runtimeType.GetField("_magazineSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bridgedMagSocketField, Is.Not.Null);
                var bridgedMagazineSocket = bridgedMagSocketField.GetValue(bridgedRuntime) as Transform;
                Assert.That(bridgedMagazineSocket, Is.Not.Null);
                Assert.That(bridgedMagazineSocket.childCount, Is.EqualTo(0), "Magazine socket should stay empty until runtime state explicitly equips a magazine attachment.");
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (magazineDefinition != null)
                {
                    Object.Destroy(magazineDefinition);
                }

                if (magazineVisualPrefab != null)
                {
                    Object.Destroy(magazineVisualPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_RejectsUnknownIncompatibleAndUnownedAttachment()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" }),
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-brake" })
            });
            registry.SetDefinitionsForTests(new[] { definition });

            runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);
            runtime.TryAddStackItem("att-muzzle-brake", 1, out _, out _, out _);

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_attachmentItemMetadata", new[]
            {
                WeaponAttachmentItemMetadata.CreateForTests("att-optic-4x", WeaponAttachmentSlotType.Scope),
                WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-brake", WeaponAttachmentSlotType.Muzzle)
            });

            yield return null;

            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-unknown"), Is.False, "Unknown attachment should be rejected.");
            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-muzzle-brake"), Is.False, "Incompatible attachment should be rejected.");

            Assert.That(runtime.TryRemoveStackItem("att-optic-4x", 1), Is.True, "Test setup should remove the scope so swap is unowned.");
            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.False, "Unowned attachment should be rejected.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_WithOwnedCompatibleAttachment_PerformsAtomicSwapAndUpdatesRuntimeState()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            var viewPrefab = new GameObject("Kar98kView");
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(viewPrefab.transform, false);
            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(viewPrefab.transform, false);
            ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x", "att-optic-8x" })
            });
            var lowOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
            var lowOpticPrefab = new GameObject("OpticFourXPrefab");
            new GameObject("SightAnchor").transform.SetParent(lowOpticPrefab.transform, false);
            SetField(opticDefinitionType, lowOpticDefinition, "_opticId", "att-optic-4x");
            SetField(opticDefinitionType, lowOpticDefinition, "_opticPrefab", lowOpticPrefab);

            var highOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
            var highOpticPrefab = new GameObject("OpticEightXPrefab");
            new GameObject("SightAnchor").transform.SetParent(highOpticPrefab.transform, false);
            SetField(opticDefinitionType, highOpticDefinition, "_opticId", "att-optic-8x");
            SetField(opticDefinitionType, highOpticDefinition, "_opticPrefab", highOpticPrefab);

            var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(iconPrefabField, Is.Not.Null);
            iconPrefabField.SetValue(definition, viewPrefab);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_attachmentItemMetadata", new[]
            {
                WeaponAttachmentItemMetadata.CreateForTests("att-optic-4x", WeaponAttachmentSlotType.Scope, lowOpticDefinition),
                WeaponAttachmentItemMetadata.CreateForTests("att-optic-8x", WeaponAttachmentSlotType.Scope, highOpticDefinition)
            });
            SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-8x");

            Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1));
            Assert.That(runtime.GetItemQuantity("att-optic-8x"), Is.EqualTo(0));

            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo("att-optic-4x"));
            Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(0), "New attachment should be consumed from inventory.");
            Assert.That(runtime.GetItemQuantity("att-optic-8x"), Is.EqualTo(1), "Previous attachment should be returned to inventory.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(viewPrefab);
            Object.Destroy(lowOpticDefinition);
            Object.Destroy(lowOpticPrefab);
            Object.Destroy(highOpticDefinition);
            Object.Destroy(highOpticPrefab);
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_AllowsCompatibilityBasedSwap_WhenMetadataLookupIsEmpty()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            var viewPrefab = new GameObject("Kar98kView");
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(viewPrefab.transform, false);
            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(viewPrefab.transform, false);
            ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);
            var manager = viewPrefab.AddComponent(attachmentManagerType);
            var scopeSlotField = attachmentManagerType.GetField("_scopeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            var ironSightField = attachmentManagerType.GetField("_ironSightAnchor", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(scopeSlotField, Is.Not.Null);
            Assert.That(ironSightField, Is.Not.Null);
            scopeSlotField.SetValue(manager, scopeSlot);
            ironSightField.SetValue(manager, ironSightAnchor);
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
            });
            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
            var opticPrefab = new GameObject("OpticFourXPrefab");
            new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
            SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-4x");
            SetField(opticDefinitionType, opticDefinition, "_isVariableZoom", false);
            SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 1f);
            SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 1f);
            SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);
            SetField(opticDefinitionType, opticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "Auto"));
            SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);
            var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(iconPrefabField, Is.Not.Null);
            iconPrefabField.SetValue(definition, viewPrefab);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_attachmentItemMetadata", Array.Empty<WeaponAttachmentItemMetadata>());
            SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

            yield return null;

            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo("att-optic-4x"));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(viewPrefab);
            Object.Destroy(opticDefinition);
            Object.Destroy(opticPrefab);
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_ScopeHotSwap_UpdatesAttachmentManagerAndAdsMask()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var scopeMaskControllerType = ResolveType("Reloader.Game.Weapons.ScopeMaskController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
            Assert.That(scopeMaskControllerType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object highOpticDefinition = null;
            UnityEngine.Object lowOpticDefinition = null;
            GameObject highOpticPrefab = null;
            GameObject lowOpticPrefab = null;
            GameObject worldCameraGo = null;
            GameObject viewmodelCameraGo = null;
            GameObject scopeMaskGo = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);
                runtime.TryAddStackItem("att-optic-8x", 1, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x", "att-optic-8x" })
                });

                viewPrefab = new GameObject("ViewPrefabWithAttachmentManager");
                viewPrefab.layer = 23;
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                scopeSlot.gameObject.layer = 23;
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ironSightAnchor.gameObject.layer = 23;
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var manager = viewPrefab.AddComponent(attachmentManagerType);
                var scopeSlotField = attachmentManagerType.GetField("_scopeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                var ironSightField = attachmentManagerType.GetField("_ironSightAnchor", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(scopeSlotField, Is.Not.Null);
                Assert.That(ironSightField, Is.Not.Null);
                scopeSlotField.SetValue(manager, scopeSlot);
                ironSightField.SetValue(manager, ironSightAnchor);

                highOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                lowOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                highOpticPrefab = new GameObject("OpticHighPrefab");
                var highSightAnchor = new GameObject("SightAnchor").transform;
                highSightAnchor.SetParent(highOpticPrefab.transform, false);
                lowOpticPrefab = new GameObject("OpticLowPrefab");
                var lowSightAnchor = new GameObject("SightAnchor").transform;
                lowSightAnchor.SetParent(lowOpticPrefab.transform, false);

                SetField(opticDefinitionType, highOpticDefinition, "_opticId", "att-optic-8x");
                SetField(opticDefinitionType, highOpticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, highOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, highOpticDefinition, "_magnificationMax", 12f);
                SetField(opticDefinitionType, highOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, highOpticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "Auto"));
                SetField(opticDefinitionType, highOpticDefinition, "_opticPrefab", highOpticPrefab);

                SetField(opticDefinitionType, lowOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, lowOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, lowOpticDefinition, "_magnificationMin", 1f);
                SetField(opticDefinitionType, lowOpticDefinition, "_magnificationMax", 1f);
                SetField(opticDefinitionType, lowOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, lowOpticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "Auto"));
                SetField(opticDefinitionType, lowOpticDefinition, "_opticPrefab", lowOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-4x", WeaponAttachmentSlotType.Scope, lowOpticDefinition),
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-8x", WeaponAttachmentSlotType.Scope, highOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-8x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);
                var bridgedManager = equippedView.GetComponent(attachmentManagerType);
                Assert.That(bridgedManager, Is.Not.Null);

                var activeOpticProperty = attachmentManagerType.GetProperty("ActiveOpticDefinition", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(activeOpticProperty, Is.Not.Null);
                var activeOptic = activeOpticProperty.GetValue(bridgedManager);
                Assert.That(activeOptic, Is.Not.Null);
                var runtimeScopeSlot = equippedView.transform.Find("ScopeSlot");
                Assert.That(runtimeScopeSlot, Is.Not.Null);
                Assert.That(runtimeScopeSlot.childCount, Is.EqualTo(1), "Scope should be mounted into scope slot.");
                Assert.That(runtimeScopeSlot.GetChild(0).gameObject.layer, Is.EqualTo(runtimeScopeSlot.gameObject.layer), "Mounted scope visual must inherit slot layer so the viewmodel camera can render it.");
                var opticIdProperty = opticDefinitionType.GetProperty("OpticId", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(opticIdProperty, Is.Not.Null);
                Assert.That((string)opticIdProperty.GetValue(activeOptic), Is.EqualTo("att-optic-8x"));

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                viewmodelCameraGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();
                scopeMaskGo = new GameObject("ScopeMask");
                var scopeMask = scopeMaskGo.AddComponent(scopeMaskControllerType);
                var ads = root.AddComponent(adsControllerType);
                SetField(adsControllerType, ads, "_worldCamera", worldCamera);
                SetField(adsControllerType, ads, "_viewmodelCamera", viewmodelCamera);
                SetField(adsControllerType, ads, "_attachmentManager", bridgedManager);
                SetField(adsControllerType, ads, "_scopeMaskController", scopeMask);
                SetField(adsControllerType, ads, "_useLegacyInput", false);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;
                Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.True);

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
                Invoke(ads, "SetMagnification", 1f);
                yield return null;
                yield return null;
                Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.False);
                Assert.That(runtimeScopeSlot.childCount, Is.EqualTo(1), "Scope should remain mounted after hot swap.");
                Assert.That(runtimeScopeSlot.GetChild(0).gameObject.layer, Is.EqualTo(runtimeScopeSlot.gameObject.layer), "Swapped scope visual must inherit slot layer.");

                activeOptic = activeOpticProperty.GetValue(bridgedManager);
                Assert.That((string)opticIdProperty.GetValue(activeOptic), Is.EqualTo("att-optic-4x"));
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (highOpticDefinition != null)
                {
                    Object.Destroy(highOpticDefinition);
                }

                if (lowOpticDefinition != null)
                {
                    Object.Destroy(lowOpticDefinition);
                }

                if (highOpticPrefab != null)
                {
                    Object.Destroy(highOpticPrefab);
                }

                if (lowOpticPrefab != null)
                {
                    Object.Destroy(lowOpticPrefab);
                }

                if (worldCameraGo != null)
                {
                    Object.Destroy(worldCameraGo);
                }

                if (viewmodelCameraGo != null)
                {
                    Object.Destroy(viewmodelCameraGo);
                }

                if (scopeMaskGo != null)
                {
                    Object.Destroy(scopeMaskGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_ReturnsFalse_WhenScopeCannotMount_AndRollsBackInventory()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                // No ScopeSlot on purpose: runtime mount should fail.
                viewPrefab = new GameObject("ViewPrefabWithoutScopeSlot");
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, ironSightAnchor: ironSightAnchor);

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-4x",
                        WeaponAttachmentSlotType.Scope,
                        opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;

                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
                Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1));

                var swapped = controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x");
                Assert.That(swapped, Is.False, "Swap must fail when runtime cannot mount scope.");
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty), "State should roll back when mount fails.");
                Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1), "Inventory should not consume attachment on failed mount.");
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator Reequip_DoesNotReseedRemovedAuthoredScopeAttachment()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-kar98k-optic" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);
                var authoredScopeVisual = new GameObject("WWII_Optic_Remote_Range_A");
                authoredScopeVisual.AddComponent<MeshFilter>();
                authoredScopeVisual.AddComponent<MeshRenderer>();
                authoredScopeVisual.transform.SetParent(viewPrefab.transform, false);

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("WWII_Optic_Remote_Range_A");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-kar98k-optic");
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-kar98k-optic",
                        WeaponAttachmentSlotType.Scope,
                        opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-kar98k-optic");
                runtime.TryAddStackItem("att-kar98k-optic", 1, out _, out _, out _);

                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, string.Empty);
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
                Assert.That(runtime.GetItemQuantity("att-kar98k-optic"), Is.EqualTo(1), "Removing authored attachment should refund inventory.");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty), "Re-equipping weapon must not re-seed removed authored scope.");
                Assert.That(runtime.GetItemQuantity("att-kar98k-optic"), Is.EqualTo(1), "Refunded attachment should remain in inventory after re-equip.");
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_ReturnsFalse_WhenDefinitionIsUnresolved_AndRollsBackInventory()
        {
            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("att-optic-unresolved", 1, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-unresolved" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-unresolved", WeaponAttachmentSlotType.Scope)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;

                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
                Assert.That(runtime.GetItemQuantity("att-optic-unresolved"), Is.EqualTo(1));

                var swapped = controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-unresolved");
                Assert.That(swapped, Is.False, "Swap must fail when runtime definition cannot be resolved.");
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty), "State should roll back when runtime definition is unresolved.");
                Assert.That(runtime.GetItemQuantity("att-optic-unresolved"), Is.EqualTo(1), "Inventory should not consume attachment on unresolved runtime definition.");
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_RemoveScope_DestroysAuthoredScopeVisual()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-kar98k-optic" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);
                var authoredScopeVisual = new GameObject("WWII_Optic_Remote_Range_A");
                authoredScopeVisual.AddComponent<MeshFilter>();
                authoredScopeVisual.AddComponent<MeshRenderer>();
                authoredScopeVisual.transform.SetParent(viewPrefab.transform, false);

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("WWII_Optic_Remote_Range_A");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-kar98k-optic");
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-kar98k-optic",
                        WeaponAttachmentSlotType.Scope,
                        opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-kar98k-optic");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);

                var authoredScopePresentBeforeRemove = false;
                var transformsBefore = equippedView.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transformsBefore.Length; i++)
                {
                    if (string.Equals(transformsBefore[i].name, "WWII_Optic_Remote_Range_A", StringComparison.Ordinal))
                    {
                        authoredScopePresentBeforeRemove = true;
                        break;
                    }
                }

                Assert.That(authoredScopePresentBeforeRemove, Is.True, "Test setup should include authored scope visual.");
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, string.Empty), Is.True);
                yield return null;

                var authoredScopePresentAfterRemove = false;
                var transformsAfter = equippedView.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transformsAfter.Length; i++)
                {
                    if (string.Equals(transformsAfter[i].name, "WWII_Optic_Remote_Range_A", StringComparison.Ordinal))
                    {
                        authoredScopePresentAfterRemove = true;
                        break;
                    }
                }

                Assert.That(authoredScopePresentAfterRemove, Is.False, "Removing scope should remove authored scope visuals from equipped view.");
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_MuzzleHotSwap_UpdatesFireOverrideDeterministically()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            RuntimeKernelBootstrapper.Events = new DefaultRuntimeEvents();

            var muzzleRuntimeType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var muzzleDefinitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(muzzleRuntimeType, Is.Not.Null);
            Assert.That(muzzleDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object muzzleA = null;
            UnityEngine.Object muzzleB = null;
            AudioClip clipA = null;
            AudioClip clipB = null;
            GameObject muzzlePrefabA = null;
            GameObject muzzlePrefabB = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("att-muzzle-a", 1, out _, out _, out _);
                runtime.TryAddStackItem("att-muzzle-b", 1, out _, out _, out _);

                var emitterSpy = root.AddComponent<ClipCaptureWeaponCombatAudioEmitter>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.01f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-a", "att-muzzle-b" })
                });

                viewPrefab = new GameObject("ViewPrefabWithMuzzleRuntime");
                var muzzleSocket = new GameObject("Muzzle").transform;
                muzzleSocket.SetParent(viewPrefab.transform, false);
                var attachmentSlot = new GameObject("MuzzleAttachmentSlot").transform;
                attachmentSlot.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, muzzleFirePoint: muzzleSocket, muzzleSlot: attachmentSlot);

                var runtimeComponent = viewPrefab.AddComponent(muzzleRuntimeType);
                SetField(muzzleRuntimeType, runtimeComponent, "_muzzleSocket", muzzleSocket);
                SetField(muzzleRuntimeType, runtimeComponent, "_attachmentSlot", attachmentSlot);

                muzzleA = ScriptableObject.CreateInstance(muzzleDefinitionType);
                muzzleB = ScriptableObject.CreateInstance(muzzleDefinitionType);
                clipA = AudioClip.Create("muzzle-a", 128, 1, 44100, false);
                clipB = AudioClip.Create("muzzle-b", 128, 1, 44100, false);
                muzzlePrefabA = new GameObject("MuzzlePrefabA");
                muzzlePrefabB = new GameObject("MuzzlePrefabB");
                SetField(muzzleDefinitionType, muzzleA, "_attachmentId", "att-muzzle-a");
                SetField(muzzleDefinitionType, muzzleA, "_muzzlePrefab", muzzlePrefabA);
                SetField(muzzleDefinitionType, muzzleA, "_fireClipOverride", clipA);
                SetField(muzzleDefinitionType, muzzleB, "_attachmentId", "att-muzzle-b");
                SetField(muzzleDefinitionType, muzzleB, "_muzzlePrefab", muzzlePrefabB);
                SetField(muzzleDefinitionType, muzzleB, "_fireClipOverride", clipB);
                SetField(muzzleRuntimeType, runtimeComponent, "_defaultAttachment", muzzleB);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_combatAudioEmitter", emitterSpy);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-a", WeaponAttachmentSlotType.Muzzle, muzzleA),
                    WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-b", WeaponAttachmentSlotType.Muzzle, muzzleB)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Muzzle, "att-muzzle-b");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;
                Assert.That(emitterSpy.LastFireOverrideClip, Is.SameAs(clipB), "Initial equipped muzzle override should match runtime slot id.");

                var fireReadyDeadline = Time.time + 1f;
                while (!state.CanFire(Time.time) && Time.time < fireReadyDeadline)
                {
                    yield return null;
                }

                Assert.That(state.CanFire(Time.time), Is.True, "Expected the weapon to clear its fire interval before validating the hot-swapped muzzle override.");
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Muzzle, "att-muzzle-a"), Is.True);
                input.FirePressedThisFrame = true;
                yield return null;
                Assert.That(emitterSpy.LastFireOverrideClip, Is.SameAs(clipA), "Hot-swapped muzzle override should apply immediately without fallback randomness.");
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (muzzleA != null)
                {
                    Object.Destroy(muzzleA);
                }

                if (muzzleB != null)
                {
                    Object.Destroy(muzzleB);
                }

                if (clipA != null)
                {
                    Object.Destroy(clipA);
                }

                if (clipB != null)
                {
                    Object.Destroy(clipB);
                }

                if (muzzlePrefabA != null)
                {
                    Object.Destroy(muzzlePrefabA);
                }

                if (muzzlePrefabB != null)
                {
                    Object.Destroy(muzzlePrefabB);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_MuzzleOnlyViewWithoutScopeSlot_StillMountsMuzzle()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            RuntimeKernelBootstrapper.Events = new DefaultRuntimeEvents();

            var muzzleDefinitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(muzzleDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object muzzleDefinition = null;
            AudioClip overrideClip = null;
            GameObject muzzlePrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("att-muzzle-a", 1, out _, out _, out _);

                var emitterSpy = root.AddComponent<ClipCaptureWeaponCombatAudioEmitter>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.01f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-a" })
                });

                viewPrefab = new GameObject("ViewPrefabWithOnlyMuzzleSlot");
                var muzzleSocket = new GameObject("Muzzle").transform;
                muzzleSocket.SetParent(viewPrefab.transform, false);
                var muzzleSlot = new GameObject("MuzzleAttachmentSlot").transform;
                muzzleSlot.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, muzzleFirePoint: muzzleSocket, muzzleSlot: muzzleSlot);

                muzzleDefinition = ScriptableObject.CreateInstance(muzzleDefinitionType);
                overrideClip = AudioClip.Create("muzzle-only", 128, 1, 44100, false);
                muzzlePrefab = new GameObject("MuzzleOnlyPrefab");
                SetField(muzzleDefinitionType, muzzleDefinition, "_attachmentId", "att-muzzle-a");
                SetField(muzzleDefinitionType, muzzleDefinition, "_muzzlePrefab", muzzlePrefab);
                SetField(muzzleDefinitionType, muzzleDefinition, "_fireClipOverride", overrideClip);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_combatAudioEmitter", emitterSpy);
                SetControllerField(controller, "_weaponViewParent", root.transform);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-a", WeaponAttachmentSlotType.Muzzle, muzzleDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Muzzle, "att-muzzle-a"), Is.True);

                input.FirePressedThisFrame = true;
                yield return null;

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                var runtimeMuzzleSlot = equippedView.Find("MuzzleAttachmentSlot");
                Assert.That(runtimeMuzzleSlot, Is.Not.Null);
                Assert.That(runtimeMuzzleSlot.childCount, Is.EqualTo(1), "Muzzle-only views should still mount runtime muzzle attachments.");
                Assert.That(emitterSpy.LastFireOverrideClip, Is.SameAs(overrideClip), "Muzzle-only views should still drive fire override clips through the attachment runtime.");
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (muzzleDefinition != null)
                {
                    Object.Destroy(muzzleDefinition);
                }

                if (overrideClip != null)
                {
                    Object.Destroy(overrideClip);
                }

                if (muzzlePrefab != null)
                {
                    Object.Destroy(muzzlePrefab);
                }
            }
        }

    }
}
