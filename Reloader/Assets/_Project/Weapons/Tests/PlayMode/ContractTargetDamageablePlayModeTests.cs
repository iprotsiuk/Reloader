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

            Object.Destroy(sinkGo);
            Object.Destroy(targetGo);
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

            Object.Destroy(unrelatedSinkGo);
            Object.Destroy(targetGo);
        }


        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}'.");
            field.SetValue(target, value);
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
