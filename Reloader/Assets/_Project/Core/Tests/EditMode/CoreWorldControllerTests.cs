using NUnit.Framework;
using Reloader.Core.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Reloader.Core.Tests.EditMode
{
    public class CoreWorldControllerTests
    {
        [Test]
        public void SetWorldState_UpdatesSnapshotAndRaisesEvent()
        {
            var go = new GameObject("CoreWorldControllerTests");
            var controller = go.AddComponent<CoreWorldController>();
            var raisedCount = 0;
            controller.WorldStateChanged += () => raisedCount++;

            controller.SetWorldState(4, 18.6667f);
            var snapshot = controller.CaptureSnapshot();

            Assert.That(raisedCount, Is.EqualTo(1));
            Assert.That(snapshot.DayCount, Is.EqualTo(4));
            Assert.That(snapshot.GetDayOfWeekName(), Is.EqualTo("Friday"));
            Assert.That(snapshot.GetFormattedTimeOfDay(), Is.EqualTo("18:40"));

            Object.DestroyImmediate(go);
        }
    }
}
