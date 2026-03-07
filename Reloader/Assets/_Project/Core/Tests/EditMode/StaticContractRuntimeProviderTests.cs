using System;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class StaticContractRuntimeProviderTests
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

        [Test]
        public void ReportTargetEliminated_WithoutExplicitPayoutReceiver_KeepsRewardClaimPendingOnProvider()
        {
            var providerGo = new GameObject("ContractProvider");
            var probeGo = new GameObject("UnrelatedPayoutProbe");
            var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
            var probe = probeGo.AddComponent<RecordingPayoutReceiver>();
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);

            try
            {
                provider.SetAvailableContract(contract);
                Assert.That(provider.AcceptAvailableContract(), Is.True);

                provider.ReportContractTargetEliminated("target.alpha", wasExposed: false);

                Assert.That(probe.TotalAwarded, Is.EqualTo(0));
                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.HasActiveContract, Is.True);
                Assert.That(snapshot.StatusText, Is.EqualTo("Ready to claim"));
                Assert.That(snapshot.CanClaimReward, Is.False, "Claim should stay disabled until a payout receiver is configured.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(probeGo);
            }
        }

        [Test]
        public void ReportTargetEliminated_WithExplicitPayoutReceiver_RequiresExplicitClaim()
        {
            var providerGo = new GameObject("ContractProvider");
            var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
            var probe = providerGo.AddComponent<RecordingPayoutReceiver>();
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);

            try
            {
                provider.SetAvailableContract(contract);
                provider.SetPayoutReceiver(probe);
                Assert.That(provider.AcceptAvailableContract(), Is.True);

                provider.ReportContractTargetEliminated("target.alpha", wasExposed: false);

                Assert.That(probe.TotalAwarded, Is.EqualTo(0));
                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.HasActiveContract, Is.True);
                Assert.That(snapshot.CanClaimReward, Is.True);

                Assert.That(provider.ClaimCompletedContractReward(), Is.True);

                Assert.That(probe.TotalAwarded, Is.EqualTo(1500));
                Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
                UnityEngine.Object.DestroyImmediate(providerGo);
            }
        }

        private static AssassinationContractDefinition CreateDefinition(
            string contractId,
            string targetId,
            float distanceBand,
            int payout)
        {
            var definition = ScriptableObject.CreateInstance<AssassinationContractDefinition>();
            var serializedObject = new SerializedObject(definition);
            serializedObject.FindProperty("_contractId")!.stringValue = contractId;
            serializedObject.FindProperty("_targetId")!.stringValue = targetId;
            serializedObject.FindProperty("_title")!.stringValue = "Street contract";
            serializedObject.FindProperty("_targetDisplayName")!.stringValue = "Victor Hale";
            serializedObject.FindProperty("_targetDescription")!.stringValue = "Grey jacket, rooftop smoker.";
            serializedObject.FindProperty("_briefingText")!.stringValue = "Eliminate the target and clear the area.";
            serializedObject.FindProperty("_distanceBand")!.floatValue = distanceBand;
            serializedObject.FindProperty("_payout")!.intValue = payout;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private sealed class RecordingPayoutReceiver : MonoBehaviour, IContractPayoutReceiver
        {
            public int TotalAwarded { get; private set; }

            public bool TryAwardContractPayout(int amount)
            {
                if (amount <= 0)
                {
                    return false;
                }

                TotalAwarded += amount;
                return true;
            }
        }
    }
}
