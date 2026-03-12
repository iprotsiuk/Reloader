using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponCombatAudioEmitterPlayModeTests
    {
        [UnityTest]
        public IEnumerator EmitWeaponFire_UsesOverrideClip_AndPublishesPlayback()
        {
            var go = new GameObject("Emitter");
            var emitter = go.AddComponent<WeaponCombatAudioEmitter>();
            var clip = AudioClip.Create("shot", 128, 1, 44100, false);

            var played = 0;
            emitter.ClipPlayed += (_, playedClip, _) =>
            {
                if (playedClip == clip)
                {
                    played++;
                }
            };

            emitter.EmitWeaponFire("weapon-kar98k", Vector3.zero, clip);
            yield return null;

            Assert.That(played, Is.EqualTo(1));

            Object.Destroy(go);
            Object.Destroy(clip);
        }

        [UnityTest]
        public IEnumerator EmitWeaponFire_DoesNotMoveEmitterHostTransform()
        {
            var go = new GameObject("EmitterHost");
            go.transform.position = new Vector3(2f, 1f, -3f);
            var initialPosition = go.transform.position;

            var emitter = go.AddComponent<WeaponCombatAudioEmitter>();
            var clip = AudioClip.Create("shot", 128, 1, 44100, false);
            var muzzlePosition = initialPosition + new Vector3(0f, 0.5f, 1.25f);

            emitter.EmitWeaponFire("weapon-kar98k", muzzlePosition, clip);
            yield return null;

            Assert.That(Vector3.Distance(go.transform.position, initialPosition), Is.LessThan(0.0001f));

            Object.Destroy(go);
            Object.Destroy(clip);
        }

        [UnityTest]
        public IEnumerator PlayerController_FireAndReload_InvokesEmitterHooks()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            RuntimeKernelBootstrapper.Events = new DefaultRuntimeEvents();

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);
            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 12, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 12, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var emitterSpy = root.AddComponent<SpyWeaponCombatAudioEmitter>();
            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_combatAudioEmitter", emitterSpy);
            SetControllerField(controller, "_weaponRegistry", registry);

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            input.ReloadPressedThisFrame = true;
            yield return null;
            yield return new WaitForSeconds(0.36f);

            Assert.That(emitterSpy.FireCount, Is.EqualTo(1));
            Assert.That(emitterSpy.ReloadStartCount, Is.EqualTo(1));
            Assert.That(emitterSpy.ReloadCompleteCount, Is.EqualTo(1));

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        private static void SetControllerField(PlayerWeaponController controller, string fieldName, object value)
        {
            var field = typeof(PlayerWeaponController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(controller, value);
        }

        private sealed class SpyWeaponCombatAudioEmitter : WeaponCombatAudioEmitter
        {
            public int FireCount { get; private set; }
            public int ReloadStartCount { get; private set; }
            public int ReloadCompleteCount { get; private set; }

            public override void EmitWeaponFire(string weaponId, Vector3 muzzlePosition, AudioClip overrideClip = null)
            {
                FireCount++;
            }

            public override void EmitReloadStarted(string weaponId, Vector3 position)
            {
                ReloadStartCount++;
            }

            public override void EmitReloadCompleted(string weaponId, Vector3 position)
            {
                ReloadCompleteCount++;
            }
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

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
