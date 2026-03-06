using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownPeripheralScopeWiringEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab";

        private static readonly Type PeripheralScopeEffectsType = FindType("Reloader.Game.Weapons.PeripheralScopeEffects");
        private static readonly Type PeripheralScopeScreenMaskType = FindType("Reloader.Game.Weapons.PeripheralScopeScreenMask");

        [Test]
        public void PlayerRootMainTownPrefab_WiresPeripheralScopeEffectsToScreenMask()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertPeripheralScopeWiring(prefabRoot, "PlayerRoot_MainTown prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void MainTownScene_PlayerRoot_WiresPeripheralScopeEffectsToScreenMask()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var playerRoot = FindRoot(scene, "PlayerRoot");
                AssertPeripheralScopeWiring(playerRoot, "MainTown scene PlayerRoot");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static void AssertPeripheralScopeWiring(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(PeripheralScopeEffectsType, Is.Not.Null, "Expected PeripheralScopeEffects type.");
            Assert.That(PeripheralScopeScreenMaskType, Is.Not.Null, "Expected PeripheralScopeScreenMask type.");

            var peripheralEffects = root.GetComponent(PeripheralScopeEffectsType);
            Assert.That(peripheralEffects, Is.Not.Null, $"{context} should include PeripheralScopeEffects.");

            var screenMask = root.GetComponent(PeripheralScopeScreenMaskType);
            Assert.That(screenMask, Is.Not.Null, $"{context} should include PeripheralScopeScreenMask.");

            var serializedEffects = new SerializedObject(peripheralEffects);
            var scopedBehaviours = serializedEffects.FindProperty("_scopedBehaviours");
            Assert.That(scopedBehaviours, Is.Not.Null, $"{context} should serialize scoped behaviours.");
            Assert.That(scopedBehaviours.arraySize, Is.GreaterThan(0), $"{context} should author at least one scoped behaviour.");

            var firstBehaviour = scopedBehaviours.GetArrayElementAtIndex(0).objectReferenceValue as Behaviour;
            Assert.That(firstBehaviour, Is.SameAs(screenMask), $"{context} should route PeripheralScopeEffects to the authored screen mask.");
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
