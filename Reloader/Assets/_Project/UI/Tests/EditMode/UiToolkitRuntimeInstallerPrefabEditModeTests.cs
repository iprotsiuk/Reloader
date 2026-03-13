using NUnit.Framework;
using Reloader.UI.Toolkit.Runtime;
using UnityEditor;
using UnityEngine;

namespace Reloader.UI.Tests.EditMode
{
    public class UiToolkitRuntimeInstallerPrefabEditModeTests
    {
        private const string BeltHudPrefabPath = "Assets/_Project/UI/Prefabs/BeltHud.prefab";

        [Test]
        public void BeltHudPrefab_RuntimeInstaller_AssignsAllRuntimeTreeReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeltHudPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Expected prefab at '{BeltHudPrefabPath}'.");

            var installer = prefab.GetComponent<UiToolkitRuntimeInstaller>();
            Assert.That(installer, Is.Not.Null, "Expected BeltHud prefab to include UiToolkitRuntimeInstaller.");

            var serialized = new SerializedObject(installer);
            AssertTreeAssigned(serialized, "_beltHudTree");
            AssertTreeAssigned(serialized, "_compassHudTree");
            AssertTreeAssigned(serialized, "_ammoHudTree");
            AssertTreeAssigned(serialized, "_tabInventoryTree");
            AssertTreeAssigned(serialized, "_escMenuTree");
            AssertTreeAssigned(serialized, "_chestInventoryTree");
            AssertTreeAssigned(serialized, "_tradeTree");
            AssertTreeAssigned(serialized, "_reloadingTree");
            AssertTreeAssigned(serialized, "_interactionHintTree");
            AssertTreeAssigned(serialized, "_dialogueOverlayTree");
            AssertTreeAssigned(serialized, "_devConsoleTree");
        }

        private static void AssertTreeAssigned(SerializedObject serialized, string propertyName)
        {
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Expected serialized property '{propertyName}'.");
            Assert.That(property!.objectReferenceValue, Is.Not.Null, $"Expected '{propertyName}' to be assigned on the runtime installer prefab.");
        }
    }
}
