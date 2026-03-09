using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Tests.EditMode
{
    public class PlayerDeviceSaveModuleTests
    {
        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-player-device-save-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _savePath = Path.Combine(_tempDir, "slot01.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesPlayerDeviceModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.4.0-dev");

            Assert.That(envelope.SchemaVersion, Is.EqualTo(9));
            Assert.That(envelope.Modules.ContainsKey("PlayerDevice"), Is.True);
            Assert.That(envelope.Modules["PlayerDevice"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_RejectsSaveMissingPlayerDeviceModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.4.0-dev");
            envelope.Modules.Remove("PlayerDevice");

            repository.WriteEnvelope(_savePath, envelope);

            var ex = Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(ex.Message, Does.Contain("Missing required module block"));
        }

        [Test]
        public void PlayerDeviceModule_RoundTrip_PopulatedPayload_PreservesData()
        {
            var source = new PlayerDeviceModule();
            source.SelectedTarget = new PlayerDeviceModule.TargetBindingRecord
            {
                TargetId = "target.lane01.round",
                DisplayName = "Lane 01 Dummy",
                DistanceMeters = 137.5f
            };
            source.ActiveGroupShots.Add(new PlayerDeviceModule.ShotRecord
            {
                TargetPlanePointXMeters = 0.018f,
                TargetPlanePointYMeters = -0.014f,
                DistanceMeters = 137.5f
            });
            source.SavedGroups.Add(new PlayerDeviceModule.GroupRecord
            {
                Shots = new List<PlayerDeviceModule.ShotRecord>
                {
                    new PlayerDeviceModule.ShotRecord
                    {
                        TargetPlanePointXMeters = -0.004f,
                        TargetPlanePointYMeters = 0.009f,
                        DistanceMeters = 140f
                    },
                    new PlayerDeviceModule.ShotRecord
                    {
                        TargetPlanePointXMeters = 0.011f,
                        TargetPlanePointYMeters = -0.002f,
                        DistanceMeters = 140f
                    }
                }
            });
            source.NotesText = "Shift aim 0.2 mil right in crosswind.";
            source.InstalledHooks.Add(1);

            var payloadJson = source.CaptureModuleStateJson();
            var restored = new PlayerDeviceModule();
            restored.RestoreModuleStateFromJson(payloadJson);

            Assert.That(restored.SelectedTarget, Is.Not.Null);
            Assert.That(restored.SelectedTarget.TargetId, Is.EqualTo("target.lane01.round"));
            Assert.That(restored.SelectedTarget.DisplayName, Is.EqualTo("Lane 01 Dummy"));
            Assert.That(restored.SelectedTarget.DistanceMeters, Is.EqualTo(137.5f));
            Assert.That(restored.ActiveGroupShots.Count, Is.EqualTo(1));
            Assert.That(restored.SavedGroups.Count, Is.EqualTo(1));
            Assert.That(restored.SavedGroups[0].Shots.Count, Is.EqualTo(2));
            Assert.That(restored.NotesText, Is.EqualTo("Shift aim 0.2 mil right in crosswind."));
            Assert.That(restored.InstalledHooks.Count, Is.EqualTo(1));
            Assert.That(restored.InstalledHooks[0], Is.EqualTo(1));
        }

        [Test]
        public void PlayerDeviceModule_Restore_ToleratesEmptyPayloadInternals()
        {
            var module = new PlayerDeviceModule();

            Assert.DoesNotThrow(() => module.RestoreModuleStateFromJson("{}"));
            Assert.That(module.SelectedTarget, Is.Null);
            Assert.That(module.ActiveGroupShots.Count, Is.EqualTo(0));
            Assert.That(module.SavedGroups.Count, Is.EqualTo(0));
            Assert.That(module.NotesText, Is.EqualTo(string.Empty));
            Assert.That(module.InstalledHooks.Count, Is.EqualTo(0));
        }
    }
}
