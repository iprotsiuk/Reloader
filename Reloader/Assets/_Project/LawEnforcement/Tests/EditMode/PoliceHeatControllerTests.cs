using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.LawEnforcement.Tests.EditMode
{
    public class PoliceHeatControllerTests
    {
        [Test]
        public void PoliceHeatController_TransitionsAcrossCoreHeatStates()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));

            controller.ReportCrime(CrimeType.Murder);
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Alerted));

            controller.ReportLineOfSightAcquired();
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

            controller.ReportLineOfSightLost();
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));

            controller.Advance(45f);
            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
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
        public void PoliceHeatController_RepeatCrimeRefreshesActiveHeatTimer()
        {
            var controller = new PoliceHeatController(searchDurationSeconds: 45f);

            controller.ReportCrime(CrimeType.Murder);
            controller.ReportLineOfSightLost();
            controller.Advance(40f);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(5f));

            controller.ReportCrime(CrimeType.Resisting);

            Assert.That(controller.CurrentState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(controller.CurrentState.LastCrimeType, Is.EqualTo(CrimeType.Resisting));
            Assert.That(controller.CurrentState.SearchTimeRemainingSeconds, Is.EqualTo(45f));
        }
    }
}
