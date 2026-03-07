using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Core.Tests.PlayMode
{
    public class StaticContractRuntimeProviderPlayModeTests
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
        public IEnumerator RuntimeKernelReconfigure_AfterAccept_PreservesActiveContractAndConsumedOffer()
        {
            var providerGo = new GameObject("ContractProvider");
            var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
            var definition = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);

            try
            {
                yield return null;

                SetAvailableContract(provider, definition);
                Assert.That(provider.AcceptAvailableContract(), Is.True);
                Assert.That(provider.TryGetContractSnapshot(out var before), Is.True);
                Assert.That(before.HasAvailableContract, Is.False);
                Assert.That(before.HasActiveContract, Is.True);

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                yield return null;

                Assert.That(provider.TryGetContractSnapshot(out var after), Is.True);
                Assert.That(after.ContractId, Is.EqualTo("contract.alpha"));
                Assert.That(after.HasAvailableContract, Is.False, "Accepted contract should not revert to an available offer after runtime event reconfigure.");
                Assert.That(after.HasActiveContract, Is.True);
                Assert.That(after.StatusText, Is.EqualTo("Active contract"));
            }
            finally
            {
                UnityEngine.Object.Destroy(providerGo);
                DestroyDefinition(definition);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeKernelReconfigure_DuringSearchClearWait_PreservesPendingPayoutProgress()
        {
            var providerGo = new GameObject("ContractProvider");
            var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
            var probe = providerGo.AddComponent<RecordingPayoutReceiver>();
            var definition = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);

            try
            {
                yield return null;

                SetAvailableContract(provider, definition);
                provider.SetPayoutReceiver(probe);
                Assert.That(provider.AcceptAvailableContract(), Is.True);

                provider.ReportContractTargetEliminated("target.alpha", wasExposed: true);
                provider.AdvanceRuntime(10f);

                var runtimeBefore = ReadRuntime(provider);
                Assert.That(runtimeBefore.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(runtimeBefore.CurrentHeatState.SearchTimeRemainingSeconds, Is.EqualTo(35f).Within(0.01f));
                Assert.That(probe.TotalAwarded, Is.EqualTo(0));

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                yield return null;

                var runtimeAfter = ReadRuntime(provider);
                Assert.That(runtimeAfter.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(runtimeAfter.CurrentHeatState.SearchTimeRemainingSeconds, Is.EqualTo(35f).Within(0.01f));
                Assert.That(ReadActiveContract(runtimeAfter), Is.Not.Null);
                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.HasActiveContract, Is.True);
                Assert.That(snapshot.StatusText, Does.StartWith("Escape search:"));

                provider.AdvanceRuntime(36f);

                Assert.That(probe.TotalAwarded, Is.EqualTo(1500));
                Assert.That(ReadActiveContract(runtimeAfter), Is.Null);
                Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.Destroy(providerGo);
                DestroyDefinition(definition);
            }
        }

        [UnityTest]
        public IEnumerator OnEnable_AfterHubSwapWhileDisabled_RebindsLawEnforcementEvents()
        {
            var providerGo = new GameObject("ContractProvider");
            var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
            var definition = CreateDefinition("contract.alpha", "target.alpha", 420f, 1500);

            try
            {
                yield return null;

                SetAvailableContract(provider, definition);
                Assert.That(provider.AcceptAvailableContract(), Is.True);

                provider.enabled = false;
                yield return null;

                var replacementHub = new DefaultRuntimeEvents();
                var heatRaised = false;
                var receivedState = default(PoliceHeatState);
                replacementHub.OnHeatChanged += state =>
                {
                    heatRaised = true;
                    receivedState = state;
                };

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);

                provider.enabled = true;
                yield return null;

                Assert.That(provider.TryGetContractSnapshot(out var snapshot), Is.True);
                Assert.That(snapshot.HasActiveContract, Is.True);

                provider.ReportContractTargetEliminated("target.alpha", wasExposed: true);
                yield return null;

                Assert.That(heatRaised, Is.True, "Re-enabled provider should publish heat updates to the replacement runtime hub.");
                Assert.That(receivedState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            }
            finally
            {
                UnityEngine.Object.Destroy(providerGo);
                DestroyDefinition(definition);
            }
        }

        private static void SetAvailableContract(StaticContractRuntimeProvider provider, UnityEngine.Object definition)
        {
            var contractType = definition.GetType();
            var setAvailableMethod = typeof(StaticContractRuntimeProvider).GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setAvailableMethod, Is.Not.Null);
            Assert.That(contractType.FullName, Is.EqualTo("Reloader.Contracts.Runtime.AssassinationContractDefinition"));
            setAvailableMethod!.Invoke(provider, new object[] { definition });
        }

        private static UnityEngine.Object CreateDefinition(
            string contractId,
            string targetId,
            float distanceBand,
            int payout)
        {
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            Assert.That(definitionType, Is.Not.Null);

            var definition = ScriptableObject.CreateInstance(definitionType!);
            Assert.That(definition, Is.Not.Null);

            SetPrivateField(definitionType!, definition, "_contractId", contractId);
            SetPrivateField(definitionType!, definition, "_targetId", targetId);
            SetPrivateField(definitionType!, definition, "_title", "Street contract");
            SetPrivateField(definitionType!, definition, "_targetDisplayName", "Victor Hale");
            SetPrivateField(definitionType!, definition, "_targetDescription", "Grey jacket, rooftop smoker.");
            SetPrivateField(definitionType!, definition, "_briefingText", "Eliminate the target and clear the area.");
            SetPrivateField(definitionType!, definition, "_distanceBand", distanceBand);
            SetPrivateField(definitionType!, definition, "_payout", payout);
            return definition;
        }

        private static void DestroyDefinition(UnityEngine.Object definition)
        {
            if (definition != null)
            {
                UnityEngine.Object.Destroy(definition);
            }
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type.Name}.");
            field!.SetValue(target, value);
        }

        private static ContractEscapeResolutionRuntime ReadRuntime(StaticContractRuntimeProvider provider)
        {
            var runtimeField = typeof(StaticContractRuntimeProvider).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(runtimeField, Is.Not.Null);

            var runtime = runtimeField!.GetValue(provider) as ContractEscapeResolutionRuntime;
            Assert.That(runtime, Is.Not.Null);
            return runtime!;
        }

        private static object ReadActiveContract(ContractEscapeResolutionRuntime runtime)
        {
            var activeContractProperty = typeof(ContractEscapeResolutionRuntime).GetProperty("ActiveContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(activeContractProperty, Is.Not.Null);
            return activeContractProperty!.GetValue(runtime);
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
