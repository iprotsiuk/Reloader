using NUnit.Framework;
using Reloader.Core.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public class CoreWorldRuntimeTests
    {
        [Test]
        public void CaptureSnapshot_FormatsWeekdayAndMilitaryTime()
        {
            var runtime = new CoreWorldRuntime(dayCount: 4, timeOfDay: 18.6667f);

            var snapshot = runtime.CaptureSnapshot();

            Assert.That(snapshot.DayCount, Is.EqualTo(4));
            Assert.That(snapshot.TimeOfDay, Is.EqualTo(18.6667f).Within(0.001f));
            Assert.That(snapshot.GetDayOfWeekName(), Is.EqualTo("Friday"));
            Assert.That(snapshot.GetFormattedTimeOfDay(), Is.EqualTo("18:40"));
        }

        [Test]
        public void CaptureSnapshot_DayCountWrapsWeekdayNameEverySevenDays()
        {
            var runtime = new CoreWorldRuntime(dayCount: 8, timeOfDay: 6.5f);

            var snapshot = runtime.CaptureSnapshot();

            Assert.That(snapshot.GetDayOfWeekName(), Is.EqualTo("Tuesday"));
            Assert.That(snapshot.GetFormattedTimeOfDay(), Is.EqualTo("06:30"));
        }

        [Test]
        public void AdvanceRealtimeSeconds_WhenOneRealSecondEqualsOneWorldMinute_WrapsAcrossMidnight()
        {
            var runtime = new CoreWorldRuntime(dayCount: 3, timeOfDay: 23.5f);

            runtime.AdvanceRealtimeSeconds(60f);
            var snapshot = runtime.CaptureSnapshot();

            Assert.That(snapshot.DayCount, Is.EqualTo(4));
            Assert.That(snapshot.TimeOfDay, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(snapshot.GetDayOfWeekName(), Is.EqualTo("Friday"));
            Assert.That(snapshot.GetFormattedTimeOfDay(), Is.EqualTo("00:30"));
        }
    }
}
