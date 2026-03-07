using System;
using System.Reflection;
using NUnit.Framework;

namespace Reloader.World.Tests.EditMode
{
    public class StyleCharacterMaterialsEditModeTests
    {
        [Test]
        public void LowpolyUrpMaterialFixer_TargetRoots_IncludeStyleCharacterCustomizationKit()
        {
            var fixerType = Type.GetType("Reloader.Weapons.Editor.LowpolyUrpMaterialFixer, Assembly-CSharp-Editor");
            Assert.That(fixerType, Is.Not.Null, "Expected Lowpoly URP material fixer editor type.");

            var rootsField = fixerType!.GetField("TargetRoots", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(rootsField, Is.Not.Null, "Expected TargetRoots field on Lowpoly URP material fixer.");

            var roots = rootsField!.GetValue(null) as string[];
            Assert.That(roots, Is.Not.Null);
            CollectionAssert.Contains(roots!, "Assets/STYLE - Character Customization Kit");
        }
    }
}
