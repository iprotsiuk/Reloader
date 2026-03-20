using NUnit.Framework;
using Reloader.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerLookConfigurationEditModeTests
    {
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab";

        [Test]
        public void PlayerRootMainTownPrefab_UsesReducedLookSmoothingSpeed()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRootPrefabPath);

            Assert.That(prefab, Is.Not.Null, "Expected PlayerRoot_MainTown prefab to exist.");

            var look = prefab!.GetComponent<PlayerLookController>();
            Assert.That(look, Is.Not.Null, "Expected PlayerLookController on PlayerRoot_MainTown.");

            var serialized = new SerializedObject(look);
            Assert.That(serialized.FindProperty("_lookSmoothingEnabled")?.boolValue, Is.True,
                "Shared mouse-look smoothing should be enabled on the canonical player prefab to avoid visible minimum-step yaw.");
            Assert.That(serialized.FindProperty("_lookSmoothingSpeed")?.floatValue, Is.EqualTo(10f).Within(0.001f),
                "MainTown should use a lower smoothing speed so tiny mouse deltas are filtered more aggressively before they move the camera subtree.");
        }

        [Test]
        public void MainTownScene_UsesReducedLookSmoothingSpeed()
        {
            const string scenePath = "Assets/_Project/World/Scenes/MainTown.unity";
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                var look = FindComponentInScene<PlayerLookController>(scene);
                Assert.That(look, Is.Not.Null, $"Expected PlayerLookController in scene '{scenePath}'.");

                var serialized = new SerializedObject(look!);
                Assert.That(serialized.FindProperty("_lookSmoothingEnabled")?.boolValue, Is.True,
                    $"Scene '{scenePath}' should not override PlayerLookController back to raw unsmoothed mouse delta.");
                Assert.That(serialized.FindProperty("_lookSmoothingSpeed")?.floatValue, Is.EqualTo(10f).Within(0.001f),
                    $"Scene '{scenePath}' should preserve the reduced look smoothing speed used to damp shared micro-step yaw.");
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

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i].GetComponentInChildren<T>(true);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
