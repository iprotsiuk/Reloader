using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class StyleCharacterMaterialsEditModeTests
    {
        private const string QuarryRockMaterialPath = "Assets/LowPoly Environment Pack/FBX/Materials/Gray.4.mat";

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
            CollectionAssert.Contains(roots!, "Assets/LowPoly Environment Pack");
        }

        [Test]
        public void QuarryRockMaterial_UsesUrpShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(QuarryRockMaterialPath);
            Assert.That(material, Is.Not.Null, $"Expected quarry rock material at '{QuarryRockMaterialPath}'.");
            Assert.That(material!.shader, Is.Not.Null, "Expected quarry rock material to resolve a shader.");
            Assert.That(
                material.shader.name,
                Does.StartWith("Universal Render Pipeline/"),
                "Quarry rocks should use a URP shader so they do not render pink.");
        }
    }
}
