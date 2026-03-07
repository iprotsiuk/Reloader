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
        public void ReportTargetEliminated_CorrectTargetExposed_AwardsPayoutAfterSearchClears()
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
                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
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
        public void ReportTargetEliminated_CorrectTargetHidden_AwardsPayoutImmediately()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var payoutReceiver = new RecordingPayoutReceiver();
            var runtime = new ContractEscapeResolutionRuntime(contract, payoutReceiver: payoutReceiver);

            try
            {
                Assert.That(runtime.AcceptAvailableContract(), Is.True);

                var eliminated = runtime.ReportTargetEliminated("target.alpha", wasExposed: false);

                Assert.That(eliminated, Is.True);
                Assert.That(runtime.ActiveContract, Is.Null);
                Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
                Assert.That(payoutReceiver.TotalAwarded, Is.EqualTo(1500));
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
        public void ReportTargetEliminated_WrongTargetFailsContractAndDoesNotAwardPayout()
        {
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
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
