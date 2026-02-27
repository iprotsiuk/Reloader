using System.Collections;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.Player;
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

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();

            string equipped = null;
            string fired = null;
            string reloaded = null;
            GameEvents.OnWeaponEquipped += OnEquipped;
            GameEvents.OnWeaponFired += OnFired;
            GameEvents.OnWeaponReloaded += OnReloaded;

            void OnEquipped(string itemId) => equipped = itemId;
            void OnFired(string itemId, Vector3 _, Vector3 __) => fired = itemId;
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

            Assert.That(reloaded, Is.EqualTo("weapon-rifle-01"));
            Assert.That(controller.TryGetRuntimeState("weapon-rifle-01", out var stateAfterReload), Is.True);
            Assert.That(stateAfterReload.MagazineCount, Is.EqualTo(5));

            GameEvents.OnWeaponEquipped -= OnEquipped;
            GameEvents.OnWeaponFired -= OnFired;
            GameEvents.OnWeaponReloaded -= OnReloaded;

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
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
