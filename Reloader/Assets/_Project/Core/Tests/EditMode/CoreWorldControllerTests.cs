using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
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

        [Test]
        public void AdvanceRealtimeSeconds_UpdatesSnapshotAndRaisesEvent()
        {
            var go = new GameObject("CoreWorldControllerTests");
            var controller = go.AddComponent<CoreWorldController>();
            var raisedCount = 0;
            controller.WorldStateChanged += () => raisedCount++;

            controller.SetWorldState(0, 8f);
            raisedCount = 0;

            controller.AdvanceRealtimeSeconds(1f);
            var snapshot = controller.CaptureSnapshot();

            Assert.That(raisedCount, Is.EqualTo(1));
            Assert.That(snapshot.DayCount, Is.EqualTo(0));
            Assert.That(snapshot.TimeOfDay, Is.EqualTo(8.016667f).Within(0.0001f));
            Assert.That(snapshot.GetFormattedTimeOfDay(), Is.EqualTo("08:01"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PrepareForSave_CopiesLiveSnapshotIntoCoreWorldModule()
        {
            var go = new GameObject("CoreWorldControllerTests");
            var controller = go.AddComponent<CoreWorldController>();
            var module = new CoreWorldModule();

            controller.SetWorldState(14, 8f);

            controller.PrepareForSave(new[]
            {
                new SaveModuleRegistration(0, module)
            });

            Assert.That(module.DayCount, Is.EqualTo(14));
            Assert.That(module.TimeOfDay, Is.EqualTo(8f).Within(0.001f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FinalizeAfterLoad_HydratesSnapshotWithoutRaisingWorldStateChanged()
        {
            var go = new GameObject("CoreWorldControllerTests");
            var controller = go.AddComponent<CoreWorldController>();
            var raisedCount = 0;
            controller.WorldStateChanged += () => raisedCount++;

            controller.FinalizeAfterLoad(new[]
            {
                new SaveModuleRegistration(0, new CoreWorldModule
                {
                    DayCount = 11,
                    TimeOfDay = 7.5f
                })
            });

            var snapshot = controller.CaptureSnapshot();

            Assert.That(raisedCount, Is.EqualTo(0));
            Assert.That(snapshot.DayCount, Is.EqualTo(11));
            Assert.That(snapshot.TimeOfDay, Is.EqualTo(7.5f).Within(0.001f));
            Assert.That(snapshot.GetDayOfWeekName(), Is.EqualTo("Friday"));
            Assert.That(snapshot.GetFormattedTimeOfDay(), Is.EqualTo("07:30"));

            Object.DestroyImmediate(go);
        }
    }
}
