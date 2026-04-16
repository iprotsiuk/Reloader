using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.LawEnforcement.Tests.EditMode
{
    public class PoliceHeatControllerTests
    {
        [Test]
        public void PoliceHeatRuntime_ReportCrimeThroughCrimeReporter_RaisesWantedHeat()
        {
            var runtime = new PoliceHeatRuntime(searchDurationSeconds: 45f);

            Assert.That(runtime, Is.AssignableTo<ILawEnforcementCrimeReporter>());

            var reporter = (ILawEnforcementCrimeReporter)runtime;
            reporter.ReportCrime(CrimeType.Murder);

            Assert.That(runtime.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(runtime.CurrentState.LastCrimeType, Is.EqualTo(CrimeType.Murder));
            Assert.That(runtime.CurrentState.WantedLevel, Is.EqualTo(3));
        }

        [Test]
        public void PoliceHeatController_ImplementsLawEnforcementCrimeReporter()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            Assert.That(controller, Is.AssignableTo<ILawEnforcementCrimeReporter>());
        }

        [Test]
        public void PoliceHeatController_TransitionsAcrossCoreHeatStates()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
            Assert.That(controller.CurrentState.WantedLevel, Is.EqualTo(0));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.False);

            controller.ReportCrime(CrimeType.Murder);
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(controller.CurrentState.WantedLevel, Is.EqualTo(3));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.False);

            controller.ReportLineOfSightAcquired();
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.True);

            controller.ReportLineOfSightLost();
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.True);

            controller.Advance(45f);
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
            Assert.That(controller.CurrentState.WantedLevel, Is.EqualTo(0));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.False);
        }

        [Test]
        public void PoliceHeatController_SearchCountdownOnlyProgressesAfterLineOfSightLost()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Murder);
            controller.ReportLineOfSightAcquired();
            controller.Advance(20f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(45f));

            controller.ReportLineOfSightLost();
            controller.Advance(20f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(25f));
        }

        [Test]
        public void PoliceHeatController_LowSeverityCrimeRequiresLineOfSightAccumulationBeforePursuit()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Trespassing);
            controller.ReportLineOfSightAcquired();

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(controller.CurrentState.WantedLevel, Is.EqualTo(1));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.False);
            Assert.That(controller.CurrentState.HasLineOfSightToPlayer, Is.True);

            controller.Advance(2f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.False);

            controller.Advance(1f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.True);
            Assert.That(controller.CurrentState.HasLineOfSightToPlayer, Is.True);
        }

        [Test]
        public void PoliceHeatController_LosingLineOfSightBeforeIdentification_RemainsAlertedInsteadOfSearching()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Trespassing);
            controller.ReportLineOfSightAcquired();
            controller.Advance(2f);
            controller.ReportLineOfSightLost();

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.False);
            Assert.That(controller.CurrentState.HasLineOfSightToPlayer, Is.False);
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(45f));

            controller.Advance(20f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(25f));

            controller.Advance(25f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
            Assert.That(controller.CurrentState.WantedLevel, Is.EqualTo(0));
        }

        [Test]
        public void PoliceHeatController_ReacquiringLineOfSightAfterIdentification_ReturnsToActivePursuit()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Trespassing);
            controller.ReportLineOfSightAcquired();
            controller.Advance(3f);
            controller.ReportLineOfSightLost();

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.True);

            controller.ReportLineOfSightAcquired();

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
            Assert.That(controller.CurrentState.IsPlayerIdentified, Is.True);
            Assert.That(controller.CurrentState.HasLineOfSightToPlayer, Is.True);
        }

        [Test]
        public void PoliceHeatController_RepeatCrimeRefreshesActiveHeatTimer()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Murder);
            controller.ReportLineOfSightAcquired();
            controller.ReportLineOfSightLost();
            controller.Advance(40f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(5f));

            controller.ReportCrime(CrimeType.Resisting);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.LastCrimeType, Is.EqualTo(CrimeType.Resisting));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(45f));
            Assert.That(controller.CurrentState.WantedLevel, Is.EqualTo(3));
        }

        [Test]
        public void PoliceHeatController_RepeatedLineOfSightLostWhileSearching_DoesNotRefreshCountdown()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Murder);
            controller.ReportLineOfSightAcquired();
            controller.ReportLineOfSightLost();
            controller.Advance(20f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(25f));

            controller.ReportLineOfSightLost();

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(25f));
        }
    }
}
