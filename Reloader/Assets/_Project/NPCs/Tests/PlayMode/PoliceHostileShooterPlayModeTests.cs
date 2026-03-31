using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class PoliceHostileShooterPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [UnityTest]
        public IEnumerator ForcedHostilePoliceShooter_FiresProjectileThroughSharedDeathPipeline()
        {
            var shooterType = Type.GetType("Reloader.NPCs.Combat.PoliceHostileShooter, Reloader.NPCs", throwOnError: false);
            Assert.That(shooterType, Is.Not.Null, "Expected a PoliceHostileShooter runtime component.");

            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), runtimeEvents);

            var projectileHitCount = 0;
            runtimeEvents.OnProjectileHit += (_, _, _) => projectileHitCount++;

            GameObject providerGo = null;
            GameObject recoveryGo = null;
            GameObject playerRoot = null;
            GameObject policeRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                providerGo.AddComponent<StaticContractRuntimeProvider>();

                recoveryGo = new GameObject("RecoveryService");
                var recovery = recoveryGo.AddComponent<RecordingPlayerRecoveryService>();

                playerRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                playerRoot.name = "RuntimePlayerRoot";
                playerRoot.transform.position = new Vector3(0f, 1f, 8f);
                playerRoot.transform.localScale = new Vector3(1.25f, 2f, 1.25f);
                playerRoot.AddComponent<HumanoidDamageReceiver>();
                playerRoot.AddComponent<PlayerDeathContractBridge>();

                policeRoot = new GameObject("PoliceShooter");
                policeRoot.transform.position = new Vector3(0f, 1f, 0f);
                policeRoot.transform.forward = Vector3.forward;

                var shooter = policeRoot.AddComponent(shooterType!);
                InvokeVoid(shooter, "SetPlayerTargetForTests", playerRoot.transform);
                InvokeVoid(shooter, "SetHostileOverrideForTests", true);

                yield return null;

                var elapsed = 0f;
                while (recovery.DeathCallCount == 0 && elapsed < 1.5f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(projectileHitCount, Is.EqualTo(1),
                    "Expected the hostile police shooter to use the shared WeaponProjectile path before the player death bridge runs.");
                Assert.That(recovery.DeathCallCount, Is.EqualTo(1),
                    "Expected a hostile police shot to kill the player through HumanoidDamageReceiver and PlayerDeathContractBridge.");
            }
            finally
            {
                if (policeRoot != null)
                {
                    UnityEngine.Object.Destroy(policeRoot);
                }

                if (playerRoot != null)
                {
                    UnityEngine.Object.Destroy(playerRoot);
                }

                if (recoveryGo != null)
                {
                    UnityEngine.Object.Destroy(recoveryGo);
                }

                if (providerGo != null)
                {
                    UnityEngine.Object.Destroy(providerGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator OnEnable_AfterPoliceHeatRestore_SamplesCurrentHostileStateWithoutWaitingForNextHeatEvent()
        {
            var shooterType = Type.GetType("Reloader.NPCs.Combat.PoliceHostileShooter, Reloader.NPCs", throwOnError: false);
            Assert.That(shooterType, Is.Not.Null, "Expected a PoliceHostileShooter runtime component.");

            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());

            GameObject providerGo = null;
            GameObject recoveryGo = null;
            GameObject playerRoot = null;
            GameObject policeRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                recoveryGo = new GameObject("RecoveryService");
                var recovery = recoveryGo.AddComponent<RecordingPlayerRecoveryService>();

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
                Assert.That(provider.CurrentHeatState.IsPlayerIdentified, Is.True);

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit),
                    "Expected contract runtime restore to preserve active pursuit during runtime hub rebuild.");
                Assert.That(provider.CurrentHeatState.IsPlayerIdentified, Is.True);

                playerRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                playerRoot.name = "RuntimePlayerRoot";
                playerRoot.transform.position = new Vector3(0f, 1f, 8f);
                playerRoot.transform.localScale = new Vector3(1.25f, 2f, 1.25f);
                playerRoot.AddComponent<HumanoidDamageReceiver>();
                playerRoot.AddComponent<PlayerDeathContractBridge>();

                policeRoot = new GameObject("PoliceShooter");
                policeRoot.transform.position = new Vector3(0f, 1f, 0f);
                policeRoot.transform.forward = Vector3.forward;

                var shooter = policeRoot.AddComponent(shooterType!);
                InvokeVoid(shooter, "SetPlayerTargetForTests", playerRoot.transform);

                yield return null;

                var elapsed = 0f;
                while (recovery.DeathCallCount == 0 && elapsed < 1.5f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(recovery.DeathCallCount, Is.EqualTo(1),
                    "Expected a police shooter enabled after heat restore to sample current wanted state and fire immediately without waiting for another heat event.");
            }
            finally
            {
                if (policeRoot != null)
                {
                    UnityEngine.Object.Destroy(policeRoot);
                }

                if (playerRoot != null)
                {
                    UnityEngine.Object.Destroy(playerRoot);
                }

                if (recoveryGo != null)
                {
                    UnityEngine.Object.Destroy(recoveryGo);
                }

                if (providerGo != null)
                {
                    UnityEngine.Object.Destroy(providerGo);
                }
            }
        }

        private static void InvokeVoid(object instance, string methodName, object argument)
        {
            var method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Expected {instance.GetType().Name}.{methodName} to exist.");
            method!.Invoke(instance, new[] { argument });
        }

        private sealed class RecordingPlayerRecoveryService : MonoBehaviour, IPlayerRecoveryService
        {
            public int ArrestCallCount { get; private set; }
            public int DeathCallCount { get; private set; }

            public bool TryApplyArrestRecovery()
            {
                ArrestCallCount++;
                return true;
            }

            public bool TryApplyDeathRecovery()
            {
                DeathCallCount++;
                return true;
            }
        }
    }
}
