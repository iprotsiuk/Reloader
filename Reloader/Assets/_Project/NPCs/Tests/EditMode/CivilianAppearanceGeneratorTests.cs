using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

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

        private static Type ResolveRequiredType(string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            Assert.That(type, Is.Not.Null, $"Expected type '{assemblyQualifiedName}' to exist.");
            return type;
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
