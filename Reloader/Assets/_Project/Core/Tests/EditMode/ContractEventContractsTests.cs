using System;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public class ContractEventContractsTests
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
        public void IRuntimeEvents_ExposesContractEventsPort()
        {
            var runtimeEventsType = typeof(IRuntimeEvents);
            var property = runtimeEventsType.GetProperty("ContractEvents");

            Assert.That(property, Is.Not.Null, "IRuntimeEvents should expose a ContractEvents typed port.");
            Assert.That(property!.PropertyType, Is.EqualTo(typeof(IContractEvents)));
        }

        [Test]
        public void ContractEventsPort_RaisesContractLifecycleEvents()
        {
            var hub = new DefaultRuntimeEvents();

            var acceptedContractId = string.Empty;
            var failedContractId = string.Empty;
            var completedContractId = string.Empty;
            var completedPayout = -1;

            Action<string> acceptedHandler = contractId => acceptedContractId = contractId;
            Action<string> failedHandler = contractId => failedContractId = contractId;
            Action<string, int> completedHandler = (contractId, payout) =>
            {
                completedContractId = contractId;
                completedPayout = payout;
            };

            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);
            RuntimeKernelBootstrapper.ContractEvents.OnContractAccepted += acceptedHandler;
            RuntimeKernelBootstrapper.ContractEvents.OnContractFailed += failedHandler;
            RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted += completedHandler;

            try
            {
                RuntimeKernelBootstrapper.ContractEvents.RaiseContractAccepted("contract.alpha");
                RuntimeKernelBootstrapper.ContractEvents.RaiseContractFailed("contract.bravo");
                RuntimeKernelBootstrapper.ContractEvents.RaiseContractCompleted("contract.charlie", 1500);
            }
            finally
            {
                RuntimeKernelBootstrapper.ContractEvents.OnContractAccepted -= acceptedHandler;
                RuntimeKernelBootstrapper.ContractEvents.OnContractFailed -= failedHandler;
                RuntimeKernelBootstrapper.ContractEvents.OnContractCompleted -= completedHandler;
            }

            Assert.That(acceptedContractId, Is.EqualTo("contract.alpha"));
            Assert.That(failedContractId, Is.EqualTo("contract.bravo"));
            Assert.That(completedContractId, Is.EqualTo("contract.charlie"));
            Assert.That(completedPayout, Is.EqualTo(1500));
        }

        [Test]
        public void AssassinationContractRuntimeState_HoldsCoreContractFields()
        {
            var state = new AssassinationContractRuntimeState("contract.alpha", "target.window", 420f, 1500);
            Assert.That(state.ContractId, Is.EqualTo("contract.alpha"));
            Assert.That(state.TargetId, Is.EqualTo("target.window"));
            Assert.That(state.DistanceBand, Is.EqualTo(420f));
            Assert.That(state.Payout, Is.EqualTo(1500));
        }
    }
}
