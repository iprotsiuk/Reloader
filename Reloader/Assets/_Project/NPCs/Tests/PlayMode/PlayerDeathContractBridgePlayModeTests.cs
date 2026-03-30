using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;
using Reloader.NPCs.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class PlayerDeathContractBridgePlayModeTests
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
        public IEnumerator LethalImpact_ForwardsContractRecoveryExactlyOncePerDeathEdge()
        {
            var bridgeType = ResolveBridgeType();
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");

            GameObject providerGo = null;
            GameObject recoveryGo = null;
            GameObject playerRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                providerGo.AddComponent<StaticContractRuntimeProvider>();

                recoveryGo = new GameObject("RecoveryService");
                var recovery = recoveryGo.AddComponent<RecordingPlayerRecoveryService>();

                playerRoot = new GameObject("RuntimePlayerRoot");
                var receiver = playerRoot.AddComponent<HumanoidDamageReceiver>();
                playerRoot.AddComponent(bridgeType);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(payloadType!, playerRoot));
                yield return null;

                Assert.That(recovery.DeathCallCount, Is.EqualTo(1),
                    "Expected the first alive-to-dead transition to forward into contract recovery.");

                InvokeApplyDamage(receiver, CreateImpactPayload(payloadType!, playerRoot));
                yield return null;

                Assert.That(recovery.DeathCallCount, Is.EqualTo(1),
                    "Expected repeated lethal impacts against an already-dead player root not to forward duplicate death handling.");
            }
            finally
            {
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

        private static Type ResolveBridgeType()
        {
            var type = Type.GetType("Reloader.NPCs.Combat.PlayerDeathContractBridge, Reloader.NPCs", throwOnError: false);
            Assert.That(type, Is.Not.Null, "Expected PlayerDeathContractBridge type.");
            return type;
        }

        private static Type ResolveType(string typeName, string assemblyName)
        {
            return Type.GetType($"{typeName}, {assemblyName}", throwOnError: false);
        }

        private static object CreateImpactPayload(Type payloadType, GameObject hitObject)
        {
            var constructor = payloadType.GetConstructor(new[]
            {
                typeof(string),
                typeof(Vector3),
                typeof(Vector3),
                typeof(float),
                typeof(GameObject)
            });

            Assert.That(constructor, Is.Not.Null, "Expected ProjectileImpactPayload(string, Vector3, Vector3, float, GameObject).");
            return constructor!.Invoke(new object[]
            {
                "weapon-kar98k",
                hitObject.transform.position,
                Vector3.back,
                20f,
                hitObject
            });
        }

        private static void InvokeApplyDamage(HumanoidDamageReceiver receiver, object payload)
        {
            var method = typeof(HumanoidDamageReceiver).GetMethod(
                nameof(HumanoidDamageReceiver.ApplyDamage),
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
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
