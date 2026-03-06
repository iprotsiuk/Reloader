using System;
using System.Reflection;
using NUnit.Framework;
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
        public void GameEventsRuntimeHub_ExposesContractLifecycleEvents()
        {
            var hubType = typeof(IGameEventsRuntimeHub);

            AssertEvent(hubType, "OnContractAccepted", typeof(Action<string>));
            AssertMethod(hubType, "RaiseContractAccepted", typeof(string));

            AssertEvent(hubType, "OnContractFailed", typeof(Action<string>));
            AssertMethod(hubType, "RaiseContractFailed", typeof(string));

            AssertEvent(hubType, "OnContractCompleted", typeof(Action<string, float>));
            AssertMethod(hubType, "RaiseContractCompleted", typeof(string), typeof(float));
        }

        [Test]
        public void DefaultRuntimeEvents_RaisesContractLifecycleEvents()
        {
            var hub = new DefaultRuntimeEvents();
            var hubType = hub.GetType();

            var acceptedContractId = string.Empty;
            var failedContractId = string.Empty;
            var completedContractId = string.Empty;
            var completedPayout = -1f;

            Action<string> acceptedHandler = contractId => acceptedContractId = contractId;
            Action<string> failedHandler = contractId => failedContractId = contractId;
            Action<string, float> completedHandler = (contractId, payout) =>
            {
                completedContractId = contractId;
                completedPayout = payout;
            };

            var acceptedEvent = AssertEvent(hubType, "OnContractAccepted", typeof(Action<string>));
            var failedEvent = AssertEvent(hubType, "OnContractFailed", typeof(Action<string>));
            var completedEvent = AssertEvent(hubType, "OnContractCompleted", typeof(Action<string, float>));

            acceptedEvent.AddEventHandler(hub, acceptedHandler);
            failedEvent.AddEventHandler(hub, failedHandler);
            completedEvent.AddEventHandler(hub, completedHandler);

            try
            {
                AssertMethod(hubType, "RaiseContractAccepted", typeof(string)).Invoke(hub, new object[] { "contract.alpha" });
                AssertMethod(hubType, "RaiseContractFailed", typeof(string)).Invoke(hub, new object[] { "contract.bravo" });
                AssertMethod(hubType, "RaiseContractCompleted", typeof(string), typeof(float)).Invoke(hub, new object[] { "contract.charlie", 1500f });
            }
            finally
            {
                acceptedEvent.RemoveEventHandler(hub, acceptedHandler);
                failedEvent.RemoveEventHandler(hub, failedHandler);
                completedEvent.RemoveEventHandler(hub, completedHandler);
            }

            Assert.That(acceptedContractId, Is.EqualTo("contract.alpha"));
            Assert.That(failedContractId, Is.EqualTo("contract.bravo"));
            Assert.That(completedContractId, Is.EqualTo("contract.charlie"));
            Assert.That(completedPayout, Is.EqualTo(1500f));
        }

        [Test]
        public void AssassinationContractRuntimeState_HoldsCoreContractFields()
        {
            var runtimeStateType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractRuntimeState, Reloader.Contracts");
            Assert.That(runtimeStateType, Is.Not.Null, "Expected AssassinationContractRuntimeState in the Reloader.Contracts assembly.");

            var state = Activator.CreateInstance(runtimeStateType, "contract.alpha", "target.window", 420f, 1500f);
            Assert.That(ReadProperty<string>(runtimeStateType, state, "ContractId"), Is.EqualTo("contract.alpha"));
            Assert.That(ReadProperty<string>(runtimeStateType, state, "TargetId"), Is.EqualTo("target.window"));
            Assert.That(ReadProperty<float>(runtimeStateType, state, "DistanceBand"), Is.EqualTo(420f));
            Assert.That(ReadProperty<float>(runtimeStateType, state, "Payout"), Is.EqualTo(1500f));
        }

        private static EventInfo AssertEvent(Type declaringType, string eventName, Type expectedHandlerType)
        {
            var eventInfo = declaringType.GetEvent(eventName);
            Assert.That(eventInfo, Is.Not.Null, $"{declaringType.Name} should declare {eventName}.");
            Assert.That(eventInfo.EventHandlerType, Is.EqualTo(expectedHandlerType));
            return eventInfo;
        }

        private static MethodInfo AssertMethod(Type declaringType, string methodName, params Type[] parameterTypes)
        {
            var methodInfo = declaringType.GetMethod(methodName, parameterTypes);
            Assert.That(methodInfo, Is.Not.Null, $"{declaringType.Name} should declare {methodName}.");
            return methodInfo;
        }

        private static T ReadProperty<T>(Type declaringType, object instance, string propertyName)
        {
            var propertyInfo = declaringType.GetProperty(propertyName);
            Assert.That(propertyInfo, Is.Not.Null, $"{declaringType.Name} should declare {propertyName}.");
            return (T)propertyInfo.GetValue(instance);
        }
    }
}
