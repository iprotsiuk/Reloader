using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save.Modules;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class CivilianAppearanceGeneratorTests
    {
        private const string GeneratorTypeName = "Reloader.NPCs.Generation.CivilianAppearanceGenerator, Reloader.NPCs";
        private const string LibraryTypeName = "Reloader.NPCs.Generation.CivilianAppearanceLibrary, Reloader.NPCs";

        [Test]
        public void GenerateRecord_WhenLibraryHasSingleChoices_UsesThoseChoicesInTheGeneratedCivilian()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "body.male.a" });
            SetProperty(library, "PresentationTypes", new[] { "masculine" });
            SetProperty(library, "HairIds", new[] { "hair.short.01" });
            SetProperty(library, "HairColorIds", new[] { "hair.black" });
            SetProperty(library, "BeardIds", new[] { "beard.none" });
            SetProperty(library, "OutfitTopIds", new[] { "top.coat.01" });
            SetProperty(library, "OutfitBottomIds", new[] { "bottom.jeans.01" });
            SetProperty(library, "OuterwearIds", new[] { "outer.gray.coat" });
            SetProperty(library, "MaterialColorIds", new[] { "color.gray" });
            SetProperty(library, "DescriptionTags", new[] { "gray coat" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var record = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.001", 4, "spawn.busstop.a", 1337, true });

            Assert.That(record, Is.Not.Null);
            Assert.That(GetProperty<string>(record, "CivilianId"), Is.EqualTo("citizen.mainTown.001"));
            Assert.That(GetProperty<string>(record, "FirstName"), Is.Not.Empty);
            Assert.That(GetProperty<string>(record, "LastName"), Is.Not.Empty);
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
            CollectionAssert.AreEqual(new[] { "color.gray" }, (IEnumerable)GetProperty<object>(record, "MaterialColorIds"));
            CollectionAssert.AreEqual(new[] { "gray coat" }, (IEnumerable)GetProperty<object>(record, "GeneratedDescriptionTags"));
            Assert.That(GetProperty<string>(record, "SpawnAnchorId"), Is.EqualTo("spawn.busstop.a"));
            Assert.That(GetProperty<int>(record, "CreatedAtDay"), Is.EqualTo(4));
            Assert.That(GetProperty<int>(record, "RetiredAtDay"), Is.EqualTo(-1));
        }

        [Test]
        public void GenerateRecord_WithSameSeed_ProducesStablePublicIdentity()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "body.male.a" });
            SetProperty(library, "PresentationTypes", new[] { "masculine" });
            SetProperty(library, "HairIds", new[] { "hair.short.01" });
            SetProperty(library, "HairColorIds", new[] { "hair.black" });
            SetProperty(library, "BeardIds", new[] { "beard.none" });
            SetProperty(library, "OutfitTopIds", new[] { "top.coat.01" });
            SetProperty(library, "OutfitBottomIds", new[] { "bottom.jeans.01" });
            SetProperty(library, "OuterwearIds", new[] { "outer.gray.coat" });
            SetProperty(library, "MaterialColorIds", new[] { "color.gray" });
            SetProperty(library, "DescriptionTags", new[] { "gray coat" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var first = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.001", 4, "spawn.busstop.a", 1337, true });
            var second = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.001", 4, "spawn.busstop.a", 1337, true });

            Assert.That(GetProperty<string>(first, "FirstName"), Is.EqualTo(GetProperty<string>(second, "FirstName")));
            Assert.That(GetProperty<string>(first, "LastName"), Is.EqualTo(GetProperty<string>(second, "LastName")));
            Assert.That(GetProperty<string>(first, "Nickname"), Is.EqualTo(GetProperty<string>(second, "Nickname")));
        }

        [Test]
        public void CivilianAppearanceLibrary_JsonRoundTrip_PreservesConfiguredArrays()
        {
            var libraryType = ResolveRequiredType(LibraryTypeName);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "body.male.a" });
            SetProperty(library, "PresentationTypes", new[] { "masculine" });
            SetProperty(library, "HairIds", new[] { "hair.short.01" });

            var json = JsonUtility.ToJson(library);
            var restored = JsonUtility.FromJson(json, libraryType);

            CollectionAssert.AreEqual(new[] { "body.male.a" }, (IEnumerable)GetProperty<object>(restored, "BaseBodyIds"));
            CollectionAssert.AreEqual(new[] { "masculine" }, (IEnumerable)GetProperty<object>(restored, "PresentationTypes"));
            CollectionAssert.AreEqual(new[] { "hair.short.01" }, (IEnumerable)GetProperty<object>(restored, "HairIds"));
        }

        [Test]
        public void GenerateRecord_WhenMaleBodyIsSelected_UsesOnlyMaleCompatibleStyleModules()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "male.body" });
            SetProperty(library, "PresentationTypes", new[] { "masculine" });
            SetProperty(library, "HairIds", new[] { "hair.short", "hair.long" });
            SetProperty(library, "HairColorIds", new[] { "hair.black" });
            SetProperty(library, "BeardIds", new[] { "beard4" });
            SetProperty(library, "OutfitTopIds", new[] { "tshirt2", "turtleneck" });
            SetProperty(library, "OutfitBottomIds", new[] { "brous1" });
            SetProperty(library, "OuterwearIds", new[] { "openJacket", "coat" });
            SetProperty(library, "MaterialColorIds", new[] { "style.default" });
            SetProperty(library, "DescriptionTags", new[] { "worker" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var record = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.101", 2, "Anchor_Townsfolk_01", 17, true });

            Assert.That(GetProperty<string>(record, "BaseBodyId"), Is.EqualTo("male.body"));
            Assert.That(GetProperty<string>(record, "HairId"), Is.EqualTo("hair.short"));
            Assert.That(GetProperty<string>(record, "BeardId"), Is.EqualTo("beard4"));
            Assert.That(GetProperty<string>(record, "OutfitTopId"), Is.EqualTo("tshirt2"));
            Assert.That(GetProperty<string>(record, "OutfitBottomId"), Is.EqualTo("pants1"));
            Assert.That(GetProperty<string>(record, "OuterwearId"), Is.EqualTo("openJacket"));
        }

        [Test]
        public void GenerateRecord_WhenFemaleBodyIsSelected_DropsMaleOnlyFacialHairAndKeepsFemaleCompatibleHair()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "female.body" });
            SetProperty(library, "PresentationTypes", new[] { "feminine" });
            SetProperty(library, "HairIds", new[] { "hair.short", "hair.long" });
            SetProperty(library, "HairColorIds", new[] { "hair.brown" });
            SetProperty(library, "BeardIds", new[] { "beard7" });
            SetProperty(library, "OutfitTopIds", new[] { "tshirt1" });
            SetProperty(library, "OutfitBottomIds", new[] { "brous10" });
            SetProperty(library, "OuterwearIds", new[] { "hoody" });
            SetProperty(library, "MaterialColorIds", new[] { "style.default" });
            SetProperty(library, "DescriptionTags", new[] { "townsfolk" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var record = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.202", 3, "Anchor_Townsfolk_02", 3, true });

            Assert.That(GetProperty<string>(record, "BaseBodyId"), Is.EqualTo("female.body"));
            Assert.That(GetProperty<string>(record, "HairId"), Is.EqualTo("hair.long"));
            Assert.That(GetProperty<string>(record, "BeardId"), Is.EqualTo("beard.none"));
            Assert.That(GetProperty<string>(record, "OutfitTopId"), Is.EqualTo("tshirt1"));
            Assert.That(GetProperty<string>(record, "OutfitBottomId"), Is.EqualTo("pants1"));
            Assert.That(GetProperty<string>(record, "OuterwearId"), Is.EqualTo("hoody"));
            AssertGeneratedRecordPassesSaveModuleValidation(record);
        }

        [Test]
        public void GenerateRecord_WhenFemaleBodyUsesApprovedCuratedBottom_PreservesThatBottom()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "female.body" });
            SetProperty(library, "PresentationTypes", new[] { "feminine" });
            SetProperty(library, "HairIds", new[] { "hair.long" });
            SetProperty(library, "HairColorIds", new[] { "hair.brown" });
            SetProperty(library, "BeardIds", new[] { "beard7" });
            SetProperty(library, "OutfitTopIds", new[] { "tshirt2" });
            SetProperty(library, "OutfitBottomIds", new[] { "brous7" });
            SetProperty(library, "OuterwearIds", new[] { string.Empty });
            SetProperty(library, "MaterialColorIds", new[] { "style.default" });
            SetProperty(library, "DescriptionTags", new[] { "townsfolk" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var record = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.203", 3, "Anchor_Townsfolk_03", 5, true });

            Assert.That(GetProperty<string>(record, "OutfitBottomId"), Is.EqualTo("pants1"));
        }

        [Test]
        public void GenerateRecord_WhenCuratedLibraryExplicitlyRequestsNoOuterwear_PreservesNoOuterwear()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "female.body" });
            SetProperty(library, "PresentationTypes", new[] { "feminine" });
            SetProperty(library, "HairIds", new[] { "hair.long" });
            SetProperty(library, "HairColorIds", new[] { "hair.brown" });
            SetProperty(library, "BeardIds", new[] { string.Empty });
            SetProperty(library, "OutfitTopIds", new[] { "tshirt2" });
            SetProperty(library, "OutfitBottomIds", new[] { "pants1" });
            SetProperty(library, "OuterwearIds", new[] { string.Empty, "   " });
            SetProperty(library, "MaterialColorIds", new[] { "style.default" });
            SetProperty(library, "DescriptionTags", new[] { "townsfolk" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var record = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.204", 3, "Anchor_Townsfolk_04", 9, true });

            Assert.That(GetProperty<string>(record, "OuterwearId"), Is.EqualTo("none"));
            AssertGeneratedRecordPassesSaveModuleValidation(record);
        }

        [Test]
        public void GenerateRecord_WhenBaseBodyAndPresentationConflict_PrioritizesBodyForCuratedGender()
        {
            var generatorType = ResolveRequiredType(GeneratorTypeName);
            var libraryType = ResolveRequiredType(LibraryTypeName);

            var generator = Activator.CreateInstance(generatorType);
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", new[] { "male.body" });
            SetProperty(library, "PresentationTypes", new[] { "feminine" });
            SetProperty(library, "HairIds", new[] { "hair.short", "hair.long" });
            SetProperty(library, "HairColorIds", new[] { "hair.black" });
            SetProperty(library, "BeardIds", new[] { "beard4" });
            SetProperty(library, "OutfitTopIds", new[] { "tshirt1" });
            SetProperty(library, "OutfitBottomIds", new[] { "pants1" });
            SetProperty(library, "OuterwearIds", new[] { string.Empty });
            SetProperty(library, "MaterialColorIds", new[] { "style.default" });
            SetProperty(library, "DescriptionTags", new[] { "townsfolk" });

            var generateMethod = generatorType.GetMethod(
                "GenerateRecord",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { libraryType, typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.That(generateMethod, Is.Not.Null, "Expected a public GenerateRecord(...) method on CivilianAppearanceGenerator.");

            var record = generateMethod.Invoke(
                generator,
                new object[] { library, "citizen.mainTown.205", 3, "Anchor_Townsfolk_05", 11, true });

            Assert.That(GetProperty<string>(record, "BaseBodyId"), Is.EqualTo("male.body"));
            Assert.That(GetProperty<string>(record, "PresentationType"), Is.EqualTo("masculine"));
            Assert.That(GetProperty<string>(record, "HairId"), Is.EqualTo("hair.short"));
            Assert.That(GetProperty<string>(record, "BeardId"), Is.EqualTo("beard4"));
        }

        [Test]
        public void IsCuratedStyleLibrary_WhenBaseBodyIdsAreMissing_ReturnsFalse()
        {
            var libraryType = ResolveRequiredType(LibraryTypeName);
            var rulesType = ResolveRequiredType("Reloader.NPCs.Generation.MainTownCuratedAppearanceRules, Reloader.NPCs");
            var library = Activator.CreateInstance(libraryType);

            SetProperty(library, "BaseBodyIds", null);

            var method = rulesType.GetMethod("IsCuratedStyleLibrary", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var isCurated = method.Invoke(null, new[] { library });

            Assert.That(isCurated, Is.EqualTo(false));
        }

        private static Type ResolveRequiredType(string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            Assert.That(type, Is.Not.Null, $"Expected type '{assemblyQualifiedName}' to exist.");
            return type;
        }

        private static void AssertGeneratedRecordPassesSaveModuleValidation(object record)
        {
            SetProperty(record, "PopulationSlotId", "validation.slot.001");
            SetProperty(record, "PoolId", "validation.pool");
            SetProperty(record, "AreaTag", "validation.area");
            var module = new CivilianPopulationModule();
            module.Civilians.Add((CivilianPopulationRecord)record);

            Assert.DoesNotThrow(() => module.ValidateModuleState());
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
