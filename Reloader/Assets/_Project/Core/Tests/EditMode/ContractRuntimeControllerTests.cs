using System;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class ContractRuntimeControllerTests
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
        public void TryAcceptContract_WithNoActiveContract_SetsActiveContractAndRaisesAcceptedEvent()
        {
            var controller = new ContractRuntimeController();
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);

            var acceptedContractId = string.Empty;
            RuntimeKernelBootstrapper.ContractEvents.OnContractAccepted += CaptureAccepted;

            try
            {
                var accepted = controller.TryAcceptContract(contract);

                Assert.That(accepted, Is.True);
                Assert.That(controller.ActiveContract, Is.Not.Null);
                Assert.That(controller.ActiveContract!.ContractId, Is.EqualTo("contract.alpha"));
                Assert.That(controller.ActiveContract.TargetId, Is.EqualTo("target.alpha"));
                Assert.That(acceptedContractId, Is.EqualTo("contract.alpha"));
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractAccepted -= CaptureAccepted;
                UnityEngine.Object.DestroyImmediate(contract);
            }

            void CaptureAccepted(string contractId)
            {
                acceptedContractId = contractId;
            }
        }

        [Test]
        public void TryAcceptContract_WhenAnotherContractIsAlreadyActive_KeepsExistingActiveContract()
        {
            var controller = new ContractRuntimeController();
            var first = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            var second = CreateDefinition("contract.bravo", "target.bravo", 510f, 2200);

            Assert.That(controller.TryAcceptContract(first), Is.True);

            var accepted = controller.TryAcceptContract(second);

            Assert.That(accepted, Is.False);
            Assert.That(controller.ActiveContract, Is.Not.Null);
            Assert.That(controller.ActiveContract!.ContractId, Is.EqualTo("contract.alpha"));
            Assert.That(controller.ActiveContract.TargetId, Is.EqualTo("target.alpha"));
        }

        [Test]
        public void TryCompleteActiveContract_RaisesCompletedEventAndClearsActiveContract()
        {
            var controller = new ContractRuntimeController();
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            Assert.That(controller.TryAcceptContract(contract), Is.True);

            var completedContractId = string.Empty;
            var completedPayout = -1;
            RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted += CaptureCompleted;

            try
            {
                var completed = controller.TryCompleteActiveContract();

                Assert.That(completed, Is.True);
                Assert.That(controller.ActiveContract, Is.Null);
                Assert.That(completedContractId, Is.EqualTo("contract.alpha"));
                Assert.That(completedPayout, Is.EqualTo(1500));
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted -= CaptureCompleted;
                UnityEngine.Object.DestroyImmediate(contract);
            }

            void CaptureCompleted(string contractId, int payout)
            {
                completedContractId = contractId;
                completedPayout = payout;
            }
        }

        [Test]
        public void TryFailActiveContract_RaisesFailedEventAndClearsActiveContract()
        {
            var controller = new ContractRuntimeController();
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            Assert.That(controller.TryAcceptContract(contract), Is.True);

            var failedContractId = string.Empty;
            RuntimeKernelBootstrapper.ContractEvents.OnContractFailed += CaptureFailed;

            try
            {
                var failed = controller.TryFailActiveContract();

                Assert.That(failed, Is.True);
                Assert.That(controller.ActiveContract, Is.Null);
                Assert.That(failedContractId, Is.EqualTo("contract.alpha"));
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractFailed -= CaptureFailed;
                UnityEngine.Object.DestroyImmediate(contract);
            }

            void CaptureFailed(string contractId)
            {
                failedContractId = contractId;
            }
        }

        [Test]
        public void ClearActiveContract_RemovesRuntimeStateWithoutLifecycleEvents()
        {
            var controller = new ContractRuntimeController();
            var contract = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);
            Assert.That(controller.TryAcceptContract(contract), Is.True);

            controller.ClearActiveContract();

            Assert.That(controller.ActiveContract, Is.Null);
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
            serializedObject.FindProperty("_distanceBand")!.floatValue = distanceBand;
            serializedObject.FindProperty("_payout")!.intValue = payout;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
