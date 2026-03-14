using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class ContractTargetDamageablePlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyDamage_LethalHit_ReportsEliminationAndDisablesTarget()
        {
            var sinkGo = new GameObject("ContractSink");
            var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            SetPrivateField(target, "_eliminationSinkBehaviour", sink);
            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 25f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
            Assert.That(sink.WasExposed, Is.True);
            Assert.That(targetGo.activeSelf, Is.False);

            UnityEngine.Object.Destroy(sinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator ApplyDamage_WithoutExplicitSink_DoesNotReportToUnrelatedSceneSink()
        {
            var unrelatedSinkGo = new GameObject("UnrelatedContractSink");
            var unrelatedSink = unrelatedSinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 10f);

            yield return null;

            target.ApplyDamage(new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: Vector3.zero,
                normal: Vector3.up,
                damage: 25f,
                hitObject: targetGo,
                sourcePoint: Vector3.back * 50f));

            Assert.That(unrelatedSink.TargetId, Is.EqualTo(string.Empty));
            Assert.That(unrelatedSink.WasExposed, Is.False);
            Assert.That(targetGo.activeSelf, Is.False);

            UnityEngine.Object.Destroy(unrelatedSinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator SharedHumanoidReceiver_LethalHeadHit_ReportsEliminationSinkThroughContractBridge()
        {
            var sinkGo = new GameObject("ContractSink");
            var sink = sinkGo.AddComponent<ContractTargetSinkProbe>();
            var targetGo = new GameObject("ContractTarget");
            var target = targetGo.AddComponent<ContractTargetDamageable>();
            targetGo.AddComponent<BoxCollider>();

            var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
            var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");

            Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared humanoid receiver to exist.");
            Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected body-zone hitbox component to exist.");
            Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum to exist.");

            SetPrivateField(target, "_eliminationSinkBehaviour", sink);
            SetPrivateField(target, "_targetId", "target.alpha");
            SetPrivateField(target, "_displayName", "Victor Hale");
            SetPrivateField(target, "_maxHealth", 1000f);

            var sharedReceiver = targetGo.AddComponent(sharedReceiverType!);
            var headZoneGo = new GameObject("HeadZone");
            headZoneGo.transform.SetParent(targetGo.transform, false);
            headZoneGo.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            headZoneGo.AddComponent<SphereCollider>().radius = 0.2f;
            var hitbox = headZoneGo.AddComponent(bodyZoneHitboxType!);
            ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Head");

            yield return null;

            InvokeApplyDamage(sharedReceiver, new ProjectileImpactPayload(
                itemId: "weapon-kar98k",
                point: headZoneGo.transform.position,
                normal: Vector3.back,
                damage: 1f,
                hitObject: headZoneGo,
                sourcePoint: targetGo.transform.position + (Vector3.back * 25f),
                direction: Vector3.forward,
                impactSpeedMetersPerSecond: 240f,
                projectileMassGrains: 175f,
                deliveredEnergyJoules: 900f));

            var lastZone = ReadReceiverProperty(sharedReceiver, "LastZone");
            Assert.That(lastZone?.ToString(), Is.EqualTo("Head"));
            Assert.That(ReadLastResultIsLethal(sharedReceiver), Is.True);

            Assert.That(sink.TargetId, Is.EqualTo("target.alpha"));
            Assert.That(sink.WasExposed, Is.True);
            Assert.That(targetGo.activeSelf, Is.False);

            UnityEngine.Object.Destroy(headZoneGo);
            UnityEngine.Object.Destroy(sinkGo);
            UnityEngine.Object.Destroy(targetGo);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static void ConfigureBodyZoneHitbox(Component bodyZoneHitbox, Type bodyZoneType, string zoneName)
        {
            var zoneValue = Enum.Parse(bodyZoneType, zoneName);
            var hitboxType = bodyZoneHitbox.GetType();

            var configureMethods = hitboxType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var i = 0; i < configureMethods.Length; i++)
            {
                var method = configureMethods[i];
                if (!string.Equals(method.Name, "Configure", StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == bodyZoneType)
                {
                    method.Invoke(bodyZoneHitbox, new[] { zoneValue });
                    return;
                }
            }

            if (TrySetZoneMember(bodyZoneHitbox, hitboxType, "_bodyZone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "_zone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "BodyZone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "Zone", zoneValue))
            {
                return;
            }

            Assert.Fail("Expected BodyZoneHitbox to support assigning HumanoidBodyZone.");
        }

        private static bool TrySetZoneMember(object instance, Type type, string memberName, object value)
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == value.GetType())
            {
                field.SetValue(instance, value);
                return true;
            }

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && property.PropertyType == value.GetType())
            {
                property.SetValue(instance, value);
                return true;
            }

            return false;
        }

        private static Type ResolveType(string fullTypeName, string assemblyName)
        {
            var type = Type.GetType($"{fullTypeName}, {assemblyName}", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void InvokeApplyDamage(Component sharedReceiver, ProjectileImpactPayload payload)
        {
            var method = sharedReceiver.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(ProjectileImpactPayload) },
                modifiers: null);

            Assert.That(method, Is.Not.Null, "Expected shared receiver to expose ApplyDamage(ProjectileImpactPayload).");
            method!.Invoke(sharedReceiver, new object[] { payload });
        }

        private static object ReadReceiverProperty(Component sharedReceiver, string propertyName)
        {
            var property = sharedReceiver.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected shared receiver to expose {propertyName}.");
            return property!.GetValue(sharedReceiver);
        }

        private static bool ReadLastResultIsLethal(Component sharedReceiver)
        {
            var result = ReadReceiverProperty(sharedReceiver, "LastResult");
            Assert.That(result, Is.Not.Null, "Expected LastResult to be available.");

            var isLethalProperty = result!.GetType().GetProperty("IsLethal", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(isLethalProperty, Is.Not.Null, "Expected LastResult to expose IsLethal.");

            var isLethal = isLethalProperty!.GetValue(result);
            Assert.That(isLethal, Is.Not.Null, "Expected IsLethal to provide a value.");
            return (bool)isLethal!;
        }

        private sealed class ContractTargetSinkProbe : MonoBehaviour, IContractTargetEliminationSink
        {
            public string TargetId { get; private set; } = string.Empty;
            public bool WasExposed { get; private set; }

            public void ReportContractTargetEliminated(string targetId, bool wasExposed)
            {
                TargetId = targetId;
                WasExposed = wasExposed;
            }
        }
    }
}
