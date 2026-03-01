using NUnit.Framework;
using Reloader.PlayerDevice.Runtime;
using UnityEngine;

namespace Reloader.PlayerDevice.Tests.EditMode
{
    public class PlayerDeviceRuntimeStateEditModeTests
    {
        [Test]
        public void NewState_DeviceAndNotesAreAvailableByDefault()
        {
            var state = new PlayerDeviceRuntimeState();

            Assert.That(state.IsDeviceAvailable, Is.True);
            Assert.That(state.IsNotesAvailable, Is.True);
            Assert.That(state.NotesText, Is.Not.Null);
        }

        [Test]
        public void InstallThenUninstallAttachment_TransitionsStateCorrectly()
        {
            var state = new PlayerDeviceRuntimeState();
            Assert.That(state.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.False);

            state.InstallAttachment(DeviceAttachmentType.Rangefinder);
            Assert.That(state.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.True);

            state.UninstallAttachment(DeviceAttachmentType.Rangefinder);
            Assert.That(state.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.False);
        }

        [Test]
        public void SetSelectedTargetBinding_AndClearSelectedTargetBinding_TransitionsStateCorrectly()
        {
            var state = new PlayerDeviceRuntimeState();
            var binding = new DeviceTargetBinding("target.alpha", "Alpha", 120f);

            state.SetSelectedTargetBinding(binding);
            Assert.That(state.HasSelectedTargetBinding, Is.True);
            Assert.That(state.SelectedTargetBinding.TargetId, Is.EqualTo("target.alpha"));

            state.ClearSelectedTargetBinding();
            Assert.That(state.HasSelectedTargetBinding, Is.False);
            Assert.That(state.SelectedTargetBinding.IsValid, Is.False);
        }

        [Test]
        public void SaveCurrentGroupThenClearActiveGroup_ClearsSessionStateAndPreservesSavedGroups()
        {
            var state = new PlayerDeviceRuntimeState();
            var sample = new DeviceShotSample(new Vector2(0.01f, -0.01f), 150f);

            state.AddShotSampleToActiveGroup(sample);
            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(1));

            state.SaveCurrentGroup();
            Assert.That(state.SavedGroupSessions.Count, Is.EqualTo(1));

            state.ClearActiveGroup();
            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(0));
            Assert.That(state.ActiveGroupSession.HasMetrics, Is.False);
            Assert.That(state.SavedGroupSessions.Count, Is.EqualTo(1));
        }
    }
}
