using System.Collections;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class PlayerWeaponControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator BeltSelectedWeapon_EquipsAndFiresAndReloads()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();

            string equipped = null;
            string fired = null;
            var firedCount = 0;
            string reloaded = null;
            GameEvents.OnWeaponEquipped += OnEquipped;
            GameEvents.OnWeaponFired += OnFired;
            GameEvents.OnWeaponReloaded += OnReloaded;

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

            Assert.That(equipped, Is.EqualTo("weapon-rifle-01"));
            Assert.That(fired, Is.EqualTo("weapon-rifle-01"));
            Assert.That(controller.TryGetRuntimeState("weapon-rifle-01", out var stateAfterFire), Is.True);
            Assert.That(stateAfterFire.MagazineCount, Is.EqualTo(0));

            input.ReloadPressedThisFrame = true;
            yield return null;
            yield return new WaitForSeconds(0.36f);

            Assert.That(reloaded, Is.EqualTo("weapon-rifle-01"));
            Assert.That(controller.TryGetRuntimeState("weapon-rifle-01", out var stateAfterReload), Is.True);
            Assert.That(stateAfterReload.MagazineCount, Is.EqualTo(5));
            Assert.That(stateAfterReload.ReserveCount, Is.EqualTo(runtime.GetItemQuantity("ammo-factory-308-147-fmj")));

            yield return new WaitForSeconds(0.11f);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedCount, Is.EqualTo(2));
            Assert.That(controller.TryGetRuntimeState("weapon-rifle-01", out var stateAfterReloadFire), Is.True);
            Assert.That(stateAfterReloadFire.ChamberLoaded, Is.True);
            Assert.That(stateAfterReloadFire.MagazineCount, Is.EqualTo(4));

            GameEvents.OnWeaponEquipped -= OnEquipped;
            GameEvents.OnWeaponFired -= OnFired;
            GameEvents.OnWeaponReloaded -= OnReloaded;

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingLocalInputSource_StillEquips_FromSceneInputProvider()
        {
            var root = new GameObject("PlayerRoot");
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();

            var inputGo = new GameObject("InputSource");
            var input = inputGo.AddComponent<TestInputSource>();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string equipped = null;
            GameEvents.OnWeaponEquipped += OnEquipped;
            void OnEquipped(string itemId) => equipped = itemId;

            yield return null;

            Assert.That(equipped, Is.EqualTo("weapon-rifle-01"));

            GameEvents.OnWeaponEquipped -= OnEquipped;
            Object.Destroy(root);
            Object.Destroy(inputGo);
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
        public IEnumerator MissingProjectilePrefab_StillSpawnsRuntimeProjectile()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
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
        public IEnumerator Fire_UsesAmmoSnapshotVelocityFps_NotWeaponProjectileSpeed()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 10f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-rifle-01", out var state), Is.True);
            var chamberRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 3000f, 0f, 168f, 0.46f, 0f);
            state.SetAmmoLoadoutForTests(chamberRound, System.Array.Empty<AmmoBallisticSnapshot>());

            input.FirePressedThisFrame = true;
            yield return null;

            var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.InitialSpeedMetersPerSecond, Is.EqualTo(914.4f).Within(1f));

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
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 10, false);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string startedItemId = null;
            string cancelledItemId = null;
            var cancelledReason = WeaponReloadCancelReason.DryStateInvalidated;
            GameEvents.OnWeaponReloadStarted += HandleStarted;
            GameEvents.OnWeaponReloadCancelled += HandleCancelled;
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

            Assert.That(startedItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(cancelledItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(cancelledReason, Is.EqualTo(WeaponReloadCancelReason.Sprint));

            GameEvents.OnWeaponReloadStarted -= HandleStarted;
            GameEvents.OnWeaponReloadCancelled -= HandleCancelled;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator AimingStateChange_RaisesWeaponAimChangedEvent()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string eventItemId = null;
            var isAiming = false;
            var raised = false;
            GameEvents.OnWeaponAimChanged += HandleAimChanged;
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
            Assert.That(eventItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(isAiming, Is.True);

            GameEvents.OnWeaponAimChanged -= HandleAimChanged;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator ReloadingThenUnequip_CancelsReloadWithUnequipReason()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-rifle-01";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 10, false);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string cancelledItemId = null;
            var cancelledReason = WeaponReloadCancelReason.DryStateInvalidated;
            GameEvents.OnWeaponReloadCancelled += HandleCancelled;
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

            Assert.That(cancelledItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(cancelledReason, Is.EqualTo(WeaponReloadCancelReason.Unequip));

            GameEvents.OnWeaponReloadCancelled -= HandleCancelled;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public bool SprintHeldValue;
            public bool AimHeldValue;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => SprintHeldValue;
            public bool AimHeld => AimHeldValue;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;

            public bool ConsumeFirePressed()
            {
                if (!FirePressedThisFrame)
                {
                    return false;
                }

                FirePressedThisFrame = false;
                return true;
            }

            public bool ConsumeReloadPressed()
            {
                if (!ReloadPressedThisFrame)
                {
                    return false;
                }

                ReloadPressedThisFrame = false;
                return true;
            }
        }

        private sealed class TestPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = null;
                return false;
            }
        }
    }
}
