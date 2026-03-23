using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PlayerStateSaveModuleTests
    {
        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-player-state-save-tests-" + Guid.NewGuid().ToString("N"));
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
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesPlayerStateModule_BeforeWorldObjectState()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.9.0-dev");

            Assert.That(envelope.SchemaVersion, Is.EqualTo(10));
            Assert.That(envelope.Modules.ContainsKey("PlayerState"), Is.True);
            Assert.That(envelope.Modules["PlayerState"].ModuleVersion, Is.EqualTo(1));

            var moduleKeys = GetRegisteredModuleKeys(coordinator);
            Assert.That(moduleKeys.IndexOf("PlayerState"), Is.GreaterThanOrEqualTo(0));
            Assert.That(moduleKeys.IndexOf("PlayerState"), Is.LessThan(moduleKeys.IndexOf("WorldObjectState")));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_RejectsSaveMissingPlayerStateModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.9.0-dev");
            envelope.Modules.Remove("PlayerState");

            repository.WriteEnvelope(_savePath, envelope);

            var ex = Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(ex!.Message, Does.Contain("PlayerState"));
        }

        [Test]
        public void PlayerStateModule_RoundTrip_PopulatedPayload_PreservesCanonicalState()
        {
            var moduleType = ResolvePlayerStateModuleType();
            var source = Activator.CreateInstance(moduleType);
            Assert.That(source, Is.Not.Null, "PlayerStateModule type should exist.");

            SetProperty(source, "CurrentScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
            SetProperty(source, "CurrentAnchorId", "entry.maintown.return");
            SetProperty(source, "PositionX", 4.5f);
            SetProperty(source, "PositionY", 1.25f);
            SetProperty(source, "PositionZ", -3.75f);
            SetProperty(source, "RotationX", 0f);
            SetProperty(source, "RotationY", 0.5f);
            SetProperty(source, "RotationZ", 0f);
            SetProperty(source, "RotationW", 0.8660254f);
            SetProperty(source, "SelectedBeltSlotIndex", 3);
            SetProperty(source, "RecoveryReasonId", "death");
            SetProperty(source, "RecoveryScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
            SetProperty(source, "RecoveryAnchorId", "entry.maintown.respawn.hospital");

            var payloadJson = (string)moduleType.GetMethod("CaptureModuleStateJson", BindingFlags.Instance | BindingFlags.Public)!.Invoke(source, null);

            var restored = Activator.CreateInstance(moduleType);
            moduleType.GetMethod("RestoreModuleStateFromJson", BindingFlags.Instance | BindingFlags.Public)!
                .Invoke(restored, new object[] { payloadJson });

            Assert.That(GetProperty<string>(restored, "CurrentScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(restored, "CurrentAnchorId"), Is.EqualTo("entry.maintown.return"));
            Assert.That(GetProperty<float>(restored, "PositionX"), Is.EqualTo(4.5f));
            Assert.That(GetProperty<float>(restored, "PositionY"), Is.EqualTo(1.25f));
            Assert.That(GetProperty<float>(restored, "PositionZ"), Is.EqualTo(-3.75f));
            Assert.That(GetProperty<float>(restored, "RotationX"), Is.EqualTo(0f));
            Assert.That(GetProperty<float>(restored, "RotationY"), Is.EqualTo(0.5f));
            Assert.That(GetProperty<float>(restored, "RotationZ"), Is.EqualTo(0f));
            Assert.That(GetProperty<float>(restored, "RotationW"), Is.EqualTo(0.8660254f));
            Assert.That(GetProperty<int>(restored, "SelectedBeltSlotIndex"), Is.EqualTo(3));
            Assert.That(GetProperty<string>(restored, "RecoveryReasonId"), Is.EqualTo("death"));
            Assert.That(GetProperty<string>(restored, "RecoveryScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetProperty<string>(restored, "RecoveryAnchorId"), Is.EqualTo("entry.maintown.respawn.hospital"));
        }

        [Test]
        public void PlayerStateModule_Validate_RejectsOutOfRangeSelectedBeltSlot()
        {
            var moduleType = ResolvePlayerStateModuleType();
            var module = Activator.CreateInstance(moduleType);

            SetProperty(module, "CurrentScenePath", "Assets/_Project/World/Scenes/MainTown.unity");
            SetProperty(module, "CurrentAnchorId", "entry.maintown.spawn");
            SetProperty(module, "RotationW", 1f);
            SetProperty(module, "SelectedBeltSlotIndex", 9);

            var ex = Assert.Throws<TargetInvocationException>(() =>
                moduleType.GetMethod("ValidateModuleState", BindingFlags.Instance | BindingFlags.Public)!.Invoke(module, null));
            Assert.That(ex!.InnerException!.Message, Does.Contain("SelectedBeltSlotIndex"));
        }

        private static Type ResolvePlayerStateModuleType()
        {
            return Type.GetType("Reloader.Core.Save.Modules.PlayerStateModule, Reloader.Core");
        }

        private static List<string> GetRegisteredModuleKeys(SaveCoordinator coordinator)
        {
            var field = typeof(SaveCoordinator).GetField("_moduleRegistrations", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "SaveCoordinator should expose module registrations for deterministic ordering.");

            var registrations = field!.GetValue(coordinator) as IEnumerable;
            Assert.That(registrations, Is.Not.Null);

            var keys = new List<string>();
            foreach (var registration in registrations!)
            {
                var moduleProperty = registration.GetType().GetProperty("Module", BindingFlags.Instance | BindingFlags.Public);
                var module = moduleProperty?.GetValue(registration);
                var moduleKeyProperty = module?.GetType().GetProperty("ModuleKey", BindingFlags.Instance | BindingFlags.Public);
                var moduleKey = moduleKeyProperty?.GetValue(module) as string;
                if (!string.IsNullOrWhiteSpace(moduleKey))
                {
                    keys.Add(moduleKey);
                }
            }

            return keys;
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            return (T)property!.GetValue(instance);
        }

        private static void SetProperty(object instance, string propertyName, object value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            property!.SetValue(instance, value);
        }
    }
}
