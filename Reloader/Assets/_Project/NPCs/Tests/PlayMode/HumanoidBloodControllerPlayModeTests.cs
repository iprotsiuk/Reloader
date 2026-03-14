using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class HumanoidBloodControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ImpactResolution_MapsBodyZonesToSemanticImpactEffects()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidBloodController", "Reloader.NPCs");
            var effectKindType = ResolveType("Reloader.NPCs.Combat.BloodEffectKind", "Reloader.NPCs");
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidBloodController to exist.");
            Assert.That(effectKindType, Is.Not.Null, "Expected BloodEffectKind enum to exist.");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            var cases = new[]
            {
                new ZoneEffectExpectation(HumanoidBodyZone.Head, "HeadImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.Neck, "NeckImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.Torso, "TorsoImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.ArmL, "ArmImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.LegR, "LegImpact")
            };

            for (var i = 0; i < cases.Length; i++)
            {
                var expectation = cases[i];
                GameObject npcRoot = null;
                GameObject zoneObject = null;
                try
                {
                    npcRoot = new GameObject($"NpcRoot-{expectation.Zone}");
                    npcRoot.AddComponent<HumanoidHitboxRig>();
                    var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                    var controller = npcRoot.AddComponent(controllerType!);

                    zoneObject = new GameObject($"{expectation.Zone}Zone");
                    zoneObject.transform.SetParent(npcRoot.transform, false);
                    zoneObject.AddComponent<BoxCollider>();
                    zoneObject.AddComponent<BodyZoneHitbox>().Configure(expectation.Zone);

                    yield return null;

                    InvokeApplyDamage(receiver, CreateImpactPayload(
                        projectileImpactPayloadType!,
                        itemId: "weapon-kar98k",
                        point: zoneObject.transform.position,
                        normal: Vector3.up,
                        damage: 1f,
                        hitObject: zoneObject,
                        sourcePoint: zoneObject.transform.position + (Vector3.back * 10f),
                        direction: Vector3.forward,
                        impactSpeedMetersPerSecond: 120f,
                        projectileMassGrains: 175f,
                        deliveredEnergyJoules: 100f));

                    var requestedEffects = ReadRequestedEffectNames(controller);
                    Assert.That(requestedEffects, Is.EquivalentTo(new[] { expectation.ExpectedEffectKindName }),
                        $"Expected {expectation.Zone} impact to request semantic blood effect '{expectation.ExpectedEffectKindName}'.");
                }
                finally
                {
                    if (zoneObject != null)
                    {
                        UnityEngine.Object.Destroy(zoneObject);
                    }

                    if (npcRoot != null)
                    {
                        UnityEngine.Object.Destroy(npcRoot);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_RequestsImpactBloodAndDeathPuddle()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidBloodController", "Reloader.NPCs");
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidBloodController to exist.");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject headZone = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent(controllerType!);

                headZone = new GameObject("HeadZone");
                headZone.transform.SetParent(npcRoot.transform, false);
                headZone.AddComponent<SphereCollider>();
                headZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Head);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: headZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: headZone,
                    sourcePoint: headZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                var requestedEffects = ReadRequestedEffectNames(controller);
                Assert.That(requestedEffects, Does.Contain("HeadImpact"),
                    "Expected lethal head impact to still request the impact blood effect.");
                Assert.That(requestedEffects, Does.Contain("DeathPuddle"),
                    "Expected lethal impact to request a follow-up death puddle effect.");
            }
            finally
            {
                if (headZone != null)
                {
                    UnityEngine.Object.Destroy(headZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }
            }
        }

        private static IReadOnlyList<string> ReadRequestedEffectNames(Component controller)
        {
            var requestsProperty = controller.GetType().GetProperty("RequestedEffects", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(requestsProperty, Is.Not.Null, "Expected HumanoidBloodController to expose RequestedEffects for semantic verification.");

            var value = requestsProperty!.GetValue(controller);
            Assert.That(value, Is.Not.Null, "Expected RequestedEffects to return a collection.");
            Assert.That(value, Is.InstanceOf<System.Collections.IEnumerable>(), "Expected RequestedEffects to be enumerable.");

            var names = new List<string>();
            foreach (var effect in (System.Collections.IEnumerable)value)
            {
                if (effect == null)
                {
                    continue;
                }

                names.Add(effect.ToString());
            }

            return names;
        }

        private static object CreateImpactPayload(
            Type payloadType,
            string itemId,
            Vector3 point,
            Vector3 normal,
            float damage,
            GameObject hitObject,
            Vector3? sourcePoint,
            Vector3? direction,
            float impactSpeedMetersPerSecond,
            float projectileMassGrains,
            float deliveredEnergyJoules)
        {
            return Activator.CreateInstance(
                payloadType,
                itemId,
                point,
                normal,
                damage,
                hitObject,
                sourcePoint,
                direction,
                impactSpeedMetersPerSecond,
                projectileMassGrains,
                deliveredEnergyJoules);
        }

        private static void InvokeApplyDamage(Component receiver, object payload)
        {
            var payloadType = payload.GetType();
            var method = receiver.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { payloadType },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var type = Type.GetType($"{fullName}, {assemblyName}", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private readonly struct ZoneEffectExpectation
        {
            public ZoneEffectExpectation(HumanoidBodyZone zone, string expectedEffectKindName)
            {
                Zone = zone;
                ExpectedEffectKindName = expectedEffectKindName;
            }

            public HumanoidBodyZone Zone { get; }
            public string ExpectedEffectKindName { get; }
        }
    }
}
