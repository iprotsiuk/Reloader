using NUnit.Framework;
using Reloader.Core.Events;

namespace Reloader.LawEnforcement.Tests.EditMode
{
    public sealed class PoliceStopDialogueRuntimeTests
    {
        [Test]
        public void TryHandleOutcome_Comply_DoesNotEscalatePoliceHeat()
        {
            var runtime = new PoliceStopDialogueRuntime(searchDurationSeconds: 30f);

            var handled = runtime.TryHandleOutcome("police.stop.comply");

            Assert.That(handled, Is.True);
            Assert.That(runtime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
        }

        [Test]
        public void TryHandleOutcome_Question_DoesNotEscalatePoliceHeat()
        {
            var runtime = new PoliceStopDialogueRuntime(searchDurationSeconds: 30f);

            var handled = runtime.TryHandleOutcome("police.stop.question");

            Assert.That(handled, Is.True);
            Assert.That(runtime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
        }

        [Test]
        public void TryHandleOutcome_Leave_EscalatesIntoActivePursuit()
        {
            var runtime = new PoliceStopDialogueRuntime(searchDurationSeconds: 30f);

            var handled = runtime.TryHandleOutcome("police.stop.leave");

            Assert.That(handled, Is.True);
            Assert.That(runtime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(runtime.CurrentState.LastCrimeType, Is.EqualTo(CrimeType.Fleeing));
            Assert.That(runtime.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(30f));
            Assert.That(runtime.CurrentState.WantedLevel, Is.EqualTo(2));
            Assert.That(runtime.CurrentState.HasLineOfSightToPlayer, Is.True);
            Assert.That(runtime.CurrentState.IsPlayerIdentified, Is.True);
        }

        [Test]
        public void TryHandleOutcome_UnknownActionId_IsIgnored()
        {
            var runtime = new PoliceStopDialogueRuntime(searchDurationSeconds: 30f);

            var handled = runtime.TryHandleOutcome("dialogue.frontdesk.exit");

            Assert.That(handled, Is.False);
            Assert.That(runtime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
        }
    }
}
