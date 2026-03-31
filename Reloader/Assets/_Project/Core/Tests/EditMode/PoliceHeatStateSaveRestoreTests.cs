using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PoliceHeatStateSaveRestoreTests
    {
        [Test]
        public void PoliceHeatStateModule_RoundTrip_PreservesAccumulatedWantedLevel()
        {
            var module = new PoliceHeatStateModule
            {
                CurrentState = new PoliceHeatState(PoliceHeatLevel.Search, CrimeType.Fleeing, 32.5f, false, 3, true)
            };

            var restored = new PoliceHeatStateModule();
            restored.RestoreModuleStateFromJson(module.CaptureModuleStateJson());

            Assert.That(restored.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(restored.CurrentState.LastCrimeType, Is.EqualTo(CrimeType.Fleeing));
            Assert.That(restored.CurrentState.WantedLevel, Is.EqualTo(3));
            Assert.That(restored.CurrentState.IsPlayerIdentified, Is.True);
        }

        [Test]
        public void PoliceHeatRuntime_RestoreAfterModuleRoundTrip_PreservesPartialIdentificationProgress()
        {
            var runtime = new PoliceHeatRuntime(searchDurationSeconds: 45f, identificationDurationSeconds: 3f);
            runtime.ReportCrime(CrimeType.Trespassing);
            runtime.ReportLineOfSightAcquired();
            runtime.Advance(2f);

            Assert.That(runtime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(runtime.CurrentState.IsPlayerIdentified, Is.False);
            Assert.That(runtime.CurrentState.HasLineOfSightToPlayer, Is.True);

            var module = new PoliceHeatStateModule
            {
                CurrentState = runtime.CurrentState
            };

            var restoredModule = new PoliceHeatStateModule();
            restoredModule.RestoreModuleStateFromJson(module.CaptureModuleStateJson());

            var restoredRuntime = new PoliceHeatRuntime(searchDurationSeconds: 45f, identificationDurationSeconds: 3f);
            InvokeRestoreState(restoredRuntime, restoredModule.CurrentState);

            restoredRuntime.Advance(1f);

            Assert.That(restoredRuntime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(restoredRuntime.CurrentState.IsPlayerIdentified, Is.True);
            Assert.That(restoredRuntime.CurrentState.HasLineOfSightToPlayer, Is.True);
        }

        [Test]
        public void PoliceHeatRuntime_RestoreState_RaisesHeatChangedForRestoredState()
        {
            var events = new DefaultRuntimeEvents();
            var runtime = new PoliceHeatRuntime(searchDurationSeconds: 45f, lawEnforcementEvents: events, identificationDurationSeconds: 3f);
            var eventRaised = false;
            var receivedState = default(PoliceHeatState);
            events.OnHeatChanged += state =>
            {
                eventRaised = true;
                receivedState = state;
            };

            InvokeRestoreState(runtime, new PoliceHeatState(PoliceHeatLevel.ActivePursuit, CrimeType.Fleeing, 20f, true, 2, true));

            Assert.That(eventRaised, Is.True);
            Assert.That(receivedState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(receivedState.IsPlayerIdentified, Is.True);
            Assert.That(receivedState.WantedLevel, Is.EqualTo(2));
        }

        private static void InvokeRestoreState(PoliceHeatRuntime runtime, PoliceHeatState state)
        {
            var restoreMethod = typeof(PoliceHeatRuntime).GetMethod(
                "RestoreState",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(restoreMethod, Is.Not.Null);
            restoreMethod.Invoke(runtime, new object[] { state });
        }
    }
}
