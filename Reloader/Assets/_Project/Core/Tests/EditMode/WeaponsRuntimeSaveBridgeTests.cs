using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save.Modules;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class WeaponsRuntimeSaveBridgeTests
    {
        [Test]
        public void CaptureToModule_TransfersRuntimeCounts()
        {
            var bridgeType = System.Type.GetType("Reloader.Weapons.Runtime.WeaponsRuntimeSaveBridge, Reloader.Weapons");
            Assert.That(bridgeType, Is.Not.Null, "WeaponsRuntimeSaveBridge type should exist.");

            var (player, controller, definition, registryGo) = CreateWeaponController(
                "weapon-kar98k",
                magCapacity: 5,
                startingMagCount: 4,
                reserveCount: 12);
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            state.SetAmmoCounts(magazineCount: 2, reserveCount: 9, chamberLoaded: true);
            state.SetAmmoLoadoutForTests(
                new AmmoBallisticSnapshot(AmmoSourceType.Handload, 2725f, 8f, 175f, 0.51f, 0.7f),
                new[]
                {
                    new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2640f, 16f, 168f, 0.45f, 1.2f),
                    new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2635f, 15f, 168f, 0.45f, 1.2f)
                });

            var bridgeGo = new GameObject("Bridge");
            var bridge = bridgeGo.AddComponent(bridgeType);
            SetPrivateField(bridgeType, bridge, "_weaponController", controller);

            var module = new WeaponsModule();
            SetPrivateField(bridgeType, bridge, "_weaponsModule", module);

            InvokeMethod(bridgeType, bridge, "CaptureToModule");

            Assert.That(module.WeaponStates.Count, Is.EqualTo(1));
            Assert.That(module.WeaponStates[0].ItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(module.WeaponStates[0].MagCount, Is.EqualTo(2));
            Assert.That(module.WeaponStates[0].ReserveCount, Is.EqualTo(9));
            Assert.That(module.WeaponStates[0].ChamberLoaded, Is.True);
            Assert.That(module.WeaponStates[0].ChamberRound, Is.Not.Null);
            Assert.That(module.WeaponStates[0].ChamberRound.MuzzleVelocityFps, Is.EqualTo(2725f));
            Assert.That(module.WeaponStates[0].MagazineRounds, Is.Not.Null);
            Assert.That(module.WeaponStates[0].MagazineRounds.Count, Is.EqualTo(2));

            Object.DestroyImmediate(bridgeGo);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(registryGo);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void RestoreFromModule_AppliesCountsIntoRuntimeState()
        {
            var bridgeType = System.Type.GetType("Reloader.Weapons.Runtime.WeaponsRuntimeSaveBridge, Reloader.Weapons");
            Assert.That(bridgeType, Is.Not.Null, "WeaponsRuntimeSaveBridge type should exist.");

            var (player, controller, definition, registryGo) = CreateWeaponController(
                "weapon-kar98k",
                magCapacity: 5,
                startingMagCount: 5,
                reserveCount: 15);
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var initialState), Is.True);
            initialState.SetAmmoCounts(5, 15, chamberLoaded: true);

            var module = new WeaponsModule();
            module.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
            {
                ItemId = "weapon-kar98k",
                MagCount = 1,
                ReserveCount = 3,
                ChamberLoaded = true,
                ChamberRound = new WeaponsModule.AmmoBallisticRecord
                {
                    AmmoSource = (int)AmmoSourceType.Handload,
                    MuzzleVelocityFps = 2750f,
                    VelocityStdDevFps = 7f,
                    ProjectileMassGrains = 175f,
                    BallisticCoefficientG1 = 0.52f,
                    DispersionMoa = 0.6f
                },
                MagazineRounds = new System.Collections.Generic.List<WeaponsModule.AmmoBallisticRecord>
                {
                    new WeaponsModule.AmmoBallisticRecord
                    {
                        AmmoSource = (int)AmmoSourceType.Factory,
                        MuzzleVelocityFps = 2660f,
                        VelocityStdDevFps = 14f,
                        ProjectileMassGrains = 168f,
                        BallisticCoefficientG1 = 0.45f,
                        DispersionMoa = 1f
                    }
                }
            });

            var bridgeGo = new GameObject("Bridge");
            var bridge = bridgeGo.AddComponent(bridgeType);
            SetPrivateField(bridgeType, bridge, "_weaponController", controller);
            SetPrivateField(bridgeType, bridge, "_weaponsModule", module);

            InvokeMethod(bridgeType, bridge, "RestoreFromModule");

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var restored), Is.True);
            Assert.That(restored.MagazineCount, Is.EqualTo(1));
            Assert.That(restored.ReserveCount, Is.EqualTo(3));
            Assert.That(restored.ChamberLoaded, Is.True);

            var fired = restored.TryFire(5f, out var fireData);
            Assert.That(fired, Is.True);
            Assert.That(fireData.FiredRound.HasValue, Is.True);
            Assert.That(fireData.FiredRound.Value.MuzzleVelocityFps, Is.EqualTo(2750f));

            Object.DestroyImmediate(bridgeGo);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(registryGo);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void RestoreFromModule_LegacyRifleId_MapsToKar98kRuntimeState()
        {
            var bridgeType = System.Type.GetType("Reloader.Weapons.Runtime.WeaponsRuntimeSaveBridge, Reloader.Weapons");
            Assert.That(bridgeType, Is.Not.Null, "WeaponsRuntimeSaveBridge type should exist.");

            var (player, controller, definition, registryGo) = CreateWeaponController(
                WeaponItemIdAliases.Kar98k,
                magCapacity: 5,
                startingMagCount: 5,
                reserveCount: 15);

            var module = new WeaponsModule();
            module.WeaponStates.Add(new WeaponsModule.WeaponStateRecord
            {
                ItemId = WeaponItemIdAliases.LegacyStarterRifle,
                MagCount = 2,
                ReserveCount = 4,
                ChamberLoaded = true
            });

            var bridgeGo = new GameObject("Bridge");
            var bridge = bridgeGo.AddComponent(bridgeType);
            SetPrivateField(bridgeType, bridge, "_weaponController", controller);
            SetPrivateField(bridgeType, bridge, "_weaponsModule", module);

            InvokeMethod(bridgeType, bridge, "RestoreFromModule");

            Assert.That(controller.TryGetRuntimeState(WeaponItemIdAliases.Kar98k, out var restored), Is.True);
            Assert.That(restored.MagazineCount, Is.EqualTo(2));
            Assert.That(restored.ReserveCount, Is.EqualTo(4));
            Assert.That(restored.ChamberLoaded, Is.True);

            Object.DestroyImmediate(bridgeGo);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(registryGo);
            Object.DestroyImmediate(player);
        }

        private static (GameObject root, PlayerWeaponController controller, WeaponDefinition definition, GameObject registryRoot) CreateWeaponController(
            string itemId,
            int magCapacity,
            int startingMagCount,
            int reserveCount)
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);
            runtime.BeltSlotItemIds[0] = itemId;
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(itemId, "Rifle", magCapacity, 0.1f, 80f, 0f, 20f, 120f, startingMagCount, reserveCount, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            var awakeMethod = typeof(PlayerWeaponController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(awakeMethod, Is.Not.Null);
            awakeMethod.Invoke(controller, null);
            var updateMethod = typeof(PlayerWeaponController).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(updateMethod, Is.Not.Null);
            updateMethod.Invoke(controller, null);
            return (root, controller, definition, registryGo);
        }

        private static void SetPrivateField(System.Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(System.Type type, object target, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {type?.Name}.");
            method.Invoke(target, null);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
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
