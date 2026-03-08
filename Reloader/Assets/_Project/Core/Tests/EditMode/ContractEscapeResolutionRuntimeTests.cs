using System;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class ContractEscapeResolutionRuntimeTests
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
        public void ReportTargetEliminated_CorrectTargetExposed_RequiresExplicitClaimAfterSearchClears()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(
                contract,
                searchDurationSeconds: 10f,
                payoutReceiver: payoutReceiver,
                lawEnforcementEvents: RuntimeKernelBootstrapper.LawEnforcementEvents);

            var completedContractId = string.Empty;
            RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted += HandleCompleted;

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                var eliminated = runtime.ReportTargetEliminated("target.alpha", wasExposed: true);

                Assert.That(eliminated, Is.True);
                Assert.That(runtime.IsAwaitingSearchClear, Is.True);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));

                runtime.Advance(9.5f);
                Assert.That(runtime.ActiveContract, Is.Not.Null);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));

                runtime.Advance(0.6f);
                Assert.That(runtime.ActiveContract, Is.Not.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
                Assert.That(runtime.HasPendingPayout, Is.True);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));
                Assert.That(runtime.TryGetSnapshot(out var claimSnapshot), Is.True);
                Assert.That(claimSnapshot.StatusText, Is.EqualTo("Ready to claim"));
                Assert.That(claimSnapshot.CanClaimReward, Is.True);

                Assert.That(runtime.ClaimCompletedContractReward(), Is.True);

                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(1500));
                Assert.That(completedContractId, Is.EqualTo("contract.alpha"));
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted -= HandleCompleted;
                UnityEngine.Object.DestroyImmediate(contract);
            }

            void HandleCompleted(string contractId, int payout)
            {
                completedContractId = contractId;
            }
        }

        [Test]
        public void ReportTargetEliminated_CorrectTargetHidden_RequiresExplicitClaimBeforePayout()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(contract, payoutReceiver: payoutReceiver);

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                var eliminated = runtime.ReportTargetEliminated("target.alpha", wasExposed: false);

                Assert.That(eliminated, Is.True);
                Assert.That(runtime.ActiveContract, Is.Not.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
                Assert.That(runtime.TryGetSnapshot(out var claimSnapshot), Is.True);
                Assert.That(claimSnapshot.StatusText, Is.EqualTo("Ready to claim"));
                Assert.That(claimSnapshot.CanClaimReward, Is.True);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));

                Assert.That(runtime.ClaimCompletedContractReward(), Is.True);

                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(1500));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
            }
        }

        [Test]
        public void CancelActiveContract_BeforeTargetElimination_RestoresPostedOfferWithoutAwardingPayout()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(contract, payoutReceiver: payoutReceiver);

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);
                Assert.That(runtime.TryGetSnapshot(out var activeSnapshot), Is.True);
                Assert.That(activeSnapshot.HasActiveContract, Is.True);
                Assert.That(activeSnapshot.CanCancel, Is.True);

                Assert.That(runtime.CancelActiveContract(), Is.True);

                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(runtime.HasPendingPayout, Is.False);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));
                Assert.That(runtime.TryGetSnapshot(out var repostedSnapshot), Is.True);
                Assert.That(repostedSnapshot.HasAvailableContract, Is.True);
                Assert.That(repostedSnapshot.HasActiveContract, Is.False);
                Assert.That(repostedSnapshot.CanAccept, Is.True);
                Assert.That(repostedSnapshot.CanCancel, Is.False);
                Assert.That(repostedSnapshot.CanClaimReward, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
            }
        }

        [Test]
        public void Advance_AfterAcceptBeforeElimination_DoesNotCompleteContractOrAwardPayout()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(contract, payoutReceiver: payoutReceiver);

            var completedContractId = string.Empty;
            RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted += HandleCompleted;

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                runtime.Advance(1f);

                Assert.That(runtime.ActiveContract, Is.Not.Null);
                Assert.That(runtime.ActiveContract.ContractId, Is.EqualTo("contract.alpha"));
                Assert.That(runtime.HasPendingPayout, Is.False);
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));
                Assert.That(completedContractId, Is.Empty);
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted -= HandleCompleted;
                UnityEngine.Object.DestroyImmediate(contract);
            }

            void HandleCompleted(string contractId, int payout)
            {
                completedContractId = contractId;
            }
        }

        [Test]
        public void ReportTargetEliminated_BeforeAccept_ConsumesOfferAndRaisesHeatWhenExposed()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(
                contract,
                searchDurationSeconds: 10f,
                payoutReceiver: payoutReceiver,
                lawEnforcementEvents: RuntimeKernelBootstrapper.LawEnforcementEvents);

            try
            {
                Assert.That(runtime.TryGetSnapshot(out var availableSnapshot), Is.True);
                Assert.That(availableSnapshot.HasAvailableContract, Is.True);
                Assert.That(availableSnapshot.CanAccept, Is.True);
                Assert.That(availableSnapshot.CanCancel, Is.False);
                Assert.That(availableSnapshot.CanClaimReward, Is.False);

                var handled = runtime.ReportTargetEliminated("target.alpha", wasExposed: true);

                Assert.That(handled, Is.True);
                Assert.That(runtime.TryGetSnapshot(out _), Is.False, "Offer should disappear once its target is already dead.");
                Assert.That(runtime.AcceptAvailableContract(), Is.False, "A consumed offer must not become acceptible after a pre-accept kill.");
                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
            }
        }

        [Test]
        public void ReportTargetEliminated_WrongTargetOnDefaultContract_KeepsContractActiveAndRaisesHeat()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(
                contract,
                searchDurationSeconds: 10f,
                payoutReceiver: payoutReceiver,
                lawEnforcementEvents: RuntimeKernelBootstrapper.LawEnforcementEvents);

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                var eliminated = runtime.ReportTargetEliminated("target.bravo", wasExposed: true);

                Assert.That(eliminated, Is.True, "Ordinary contracts should treat wrong-target kills as sandbox consequences, not automatic mission failure.");
                Assert.That(runtime.ActiveContract, Is.Not.Null, "Default contracts should remain active after a wrong-target kill.");
                Assert.That(runtime.ActiveContract!.ContractId, Is.EqualTo("contract.alpha"));
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));
                Assert.That(runtime.TryGetSnapshot(out var activeSnapshot), Is.True);
                Assert.That(activeSnapshot.HasActiveContract, Is.True);
                Assert.That(activeSnapshot.HasFailedContract, Is.False);
                Assert.That(activeSnapshot.StatusText, Is.EqualTo("Active contract"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
            }
        }

        [Test]
        public void ReportTargetEliminated_WrongTargetFailsStrictContractAndDoesNotAwardPayout()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            ConfigureWrongTargetFailureRule(contract);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(
                contract,
                searchDurationSeconds: 10f,
                payoutReceiver: payoutReceiver,
                lawEnforcementEvents: RuntimeKernelBootstrapper.LawEnforcementEvents);

            var failedContractId = string.Empty;
            RuntimeKernelBootstrapper.ContractEvents.OnContractFailed += HandleFailed;

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                var eliminated = runtime.ReportTargetEliminated("target.bravo", wasExposed: true);

                Assert.That(eliminated, Is.False);
                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(0));
                Assert.That(failedContractId, Is.EqualTo("contract.alpha"));
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractFailed -= HandleFailed;
                UnityEngine.Object.DestroyImmediate(contract);
            }

            void HandleFailed(string contractId)
            {
                failedContractId = contractId;
            }
        }

        [Test]
        public void ReportTargetEliminated_WrongTargetKeepsStrictFailureSnapshotVisibleDuringSearch()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            ConfigureWrongTargetFailureRule(contract);
            var runtime = new ContractEscapeResolutionRuntime(
                contract,
                searchDurationSeconds: 10f,
                payoutReceiver: new RecordingPayoutReceiver(),
                lawEnforcementEvents: RuntimeKernelBootstrapper.LawEnforcementEvents);

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                var eliminated = runtime.ReportTargetEliminated("target.bravo", wasExposed: true);

                Assert.That(eliminated, Is.False);
                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(runtime.TryGetSnapshot(out var failedSnapshot), Is.True,
                    "Expected wrong-target failures to keep a readable failure snapshot instead of collapsing the Contracts UI to 'no contracts'.");
                Assert.That(failedSnapshot.StatusText, Does.StartWith("Failed: wrong target"),
                    "Expected the failure snapshot to explain why the accepted contract disappeared.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contract);
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

        private static void ConfigureWrongTargetFailureRule(AssassinationContractDefinition definition)
        {
            var serializedObject = new SerializedObject(definition);
            var rulesProperty = serializedObject.FindProperty("_failurePolicy._failureRules");
            Assert.That(rulesProperty, Is.Not.Null,
                "Expected assassination contracts to expose a serialized failure-policy list for opt-in mission restrictions.");

            rulesProperty!.arraySize = 1;
            var ruleProperty = rulesProperty.GetArrayElementAtIndex(0);
            var ruleTypeProperty = ruleProperty.FindPropertyRelative("_ruleType");
            Assert.That(ruleTypeProperty, Is.Not.Null,
                "Expected each failure rule to serialize a rule type so strict contracts can opt into wrong-target failure.");

            ruleTypeProperty!.enumValueIndex = 0;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class RecordingPayoutReceiver : IContractPayoutReceiver
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
