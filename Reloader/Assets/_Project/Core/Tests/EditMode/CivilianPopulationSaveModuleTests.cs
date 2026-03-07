using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;

namespace Reloader.Core.Tests.EditMode
{
    public class CivilianPopulationSaveModuleTests
    {
        private const string ModuleTypeName = "Reloader.Core.Save.Modules.CivilianPopulationModule, Reloader.Core";
        private const string RecordTypeName = "Reloader.Core.Save.Modules.CivilianPopulationRecord, Reloader.Core";
        private const string ReplacementTypeName = "Reloader.Core.Save.Modules.CivilianPopulationReplacementRecord, Reloader.Core";

        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-civilian-population-save-tests-" + Guid.NewGuid().ToString("N"));
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
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesCivilianPopulationModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.7.0-dev");

            Assert.That(envelope.SchemaVersion, Is.EqualTo(7));
            Assert.That(envelope.Modules.ContainsKey("CivilianPopulation"), Is.True);
            Assert.That(envelope.Modules["CivilianPopulation"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_RejectsSaveMissingCivilianPopulationModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.7.0-dev");
            envelope.Modules.Remove("CivilianPopulation");

            repository.WriteEnvelope(_savePath, envelope);

            var ex = Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(ex.Message, Does.Contain("Missing required module block"));
            Assert.That(ex.Message, Does.Contain("CivilianPopulation"));
        }

        [Test]
        public void CivilianPopulationModule_RoundTrip_PopulatedPayload_PreservesData()
        {
            var moduleType = ResolveRequiredType(ModuleTypeName);
            var recordType = ResolveRequiredType(RecordTypeName);
            var replacementType = ResolveRequiredType(ReplacementTypeName);

            var source = Activator.CreateInstance(moduleType);
            Assert.That(source, Is.Not.Null);

            var civilians = GetRequiredList(source, "Civilians");
            civilians.Add(CreateCivilianRecord(recordType));

            var replacements = GetRequiredList(source, "PendingReplacements");
            replacements.Add(CreateReplacementRecord(replacementType));

            var captureMethod = moduleType.GetMethod("CaptureModuleStateJson", BindingFlags.Public | BindingFlags.Instance);
            var restoreMethod = moduleType.GetMethod("RestoreModuleStateFromJson", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(captureMethod, Is.Not.Null);
            Assert.That(restoreMethod, Is.Not.Null);

            var payloadJson = (string)captureMethod.Invoke(source, Array.Empty<object>());
            var restored = Activator.CreateInstance(moduleType);
            restoreMethod.Invoke(restored, new object[] { payloadJson });

            var restoredCivilians = GetRequiredList(restored, "Civilians");
            Assert.That(restoredCivilians.Count, Is.EqualTo(1));
            AssertRecord(restoredCivilians[0]);

            var restoredReplacements = GetRequiredList(restored, "PendingReplacements");
            Assert.That(restoredReplacements.Count, Is.EqualTo(1));
            AssertReplacement(restoredReplacements[0]);
        }

        [Test]
        public void CivilianPopulationModule_ValidateModuleState_RejectsCitizensMissingSpawnAnchor()
        {
            var moduleType = ResolveRequiredType(ModuleTypeName);
            var recordType = ResolveRequiredType(RecordTypeName);

            var module = Activator.CreateInstance(moduleType);
            var civilians = GetRequiredList(module, "Civilians");
            var record = CreateCivilianRecord(recordType);
            SetProperty(record, "SpawnAnchorId", string.Empty);
            civilians.Add(record);

            var validateMethod = moduleType.GetMethod("ValidateModuleState", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validateMethod, Is.Not.Null);

            var ex = Assert.Throws<TargetInvocationException>(() => validateMethod!.Invoke(module, Array.Empty<object>()));
            Assert.That(ex?.InnerException, Is.Not.Null);
            Assert.That(ex!.InnerException!.Message, Does.Contain("spawnAnchorId"));
        }

        private static object CreateCivilianRecord(Type recordType)
        {
            var record = Activator.CreateInstance(recordType);
            SetProperty(record, "CivilianId", "citizen.mainTown.001");
            SetProperty(record, "IsAlive", true);
            SetProperty(record, "IsContractEligible", true);
            SetProperty(record, "BaseBodyId", "body.male.a");
            SetProperty(record, "PresentationType", "masculine");
            SetProperty(record, "HairId", "hair.short.01");
            SetProperty(record, "HairColorId", "hair.black");
            SetProperty(record, "BeardId", "beard.none");
            SetProperty(record, "OutfitTopId", "top.coat.01");
            SetProperty(record, "OutfitBottomId", "bottom.jeans.01");
            SetProperty(record, "OuterwearId", "outer.gray.coat");
            SetProperty(record, "MaterialColorIds", new List<string> { "color.gray", "color.black" });
            SetProperty(record, "GeneratedDescriptionTags", new List<string> { "gray coat", "short hair" });
            SetProperty(record, "SpawnAnchorId", "spawn.busstop.a");
            SetProperty(record, "CreatedAtDay", 4);
            SetProperty(record, "RetiredAtDay", -1);
            return record;
        }

        private static object CreateReplacementRecord(Type replacementType)
        {
            var record = Activator.CreateInstance(replacementType);
            SetProperty(record, "VacatedCivilianId", "citizen.mainTown.001");
            SetProperty(record, "QueuedAtDay", 5);
            SetProperty(record, "SpawnAnchorId", "spawn.busstop.a");
            return record;
        }

        private static void AssertRecord(object record)
        {
            Assert.That(GetProperty<string>(record, "CivilianId"), Is.EqualTo("citizen.mainTown.001"));
            Assert.That(GetProperty<bool>(record, "IsAlive"), Is.True);
            Assert.That(GetProperty<bool>(record, "IsContractEligible"), Is.True);
            Assert.That(GetProperty<string>(record, "BaseBodyId"), Is.EqualTo("body.male.a"));
            Assert.That(GetProperty<string>(record, "PresentationType"), Is.EqualTo("masculine"));
            Assert.That(GetProperty<string>(record, "HairId"), Is.EqualTo("hair.short.01"));
            Assert.That(GetProperty<string>(record, "HairColorId"), Is.EqualTo("hair.black"));
            Assert.That(GetProperty<string>(record, "BeardId"), Is.EqualTo("beard.none"));
            Assert.That(GetProperty<string>(record, "OutfitTopId"), Is.EqualTo("top.coat.01"));
            Assert.That(GetProperty<string>(record, "OutfitBottomId"), Is.EqualTo("bottom.jeans.01"));
            Assert.That(GetProperty<string>(record, "OuterwearId"), Is.EqualTo("outer.gray.coat"));
            CollectionAssert.AreEqual(new[] { "color.gray", "color.black" }, (IEnumerable)GetProperty<object>(record, "MaterialColorIds"));
            CollectionAssert.AreEqual(new[] { "gray coat", "short hair" }, (IEnumerable)GetProperty<object>(record, "GeneratedDescriptionTags"));
            Assert.That(GetProperty<string>(record, "SpawnAnchorId"), Is.EqualTo("spawn.busstop.a"));
            Assert.That(GetProperty<int>(record, "CreatedAtDay"), Is.EqualTo(4));
            Assert.That(GetProperty<int>(record, "RetiredAtDay"), Is.EqualTo(-1));
        }

        private static void AssertReplacement(object record)
        {
            Assert.That(GetProperty<string>(record, "VacatedCivilianId"), Is.EqualTo("citizen.mainTown.001"));
            Assert.That(GetProperty<int>(record, "QueuedAtDay"), Is.EqualTo(5));
            Assert.That(GetProperty<string>(record, "SpawnAnchorId"), Is.EqualTo("spawn.busstop.a"));
        }

        private static Type ResolveRequiredType(string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            Assert.That(type, Is.Not.Null, $"Expected type '{assemblyQualifiedName}' to exist.");
            return type;
        }

        private static IList GetRequiredList(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");

            var value = property.GetValue(instance) as IList;
            Assert.That(value, Is.Not.Null, $"Expected '{propertyName}' to be an IList.");
            return value;
        }

        private static void SetProperty(object instance, string propertyName, object value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            property.SetValue(instance, value);
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            return (T)property.GetValue(instance);
        }
    }
}
