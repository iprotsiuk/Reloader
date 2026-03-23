using NUnit.Framework;
using Reloader.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerLookConfigurationEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";

        [TestCase(MainTownScenePath)]
        [TestCase(IndoorRangeScenePath)]
        public void Scene_DoesNotAuthorPlayerLookController(string scenePath)
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                var look = FindComponentInScene<PlayerLookController>(scene);
                Assert.That(look, Is.Null,
                    $"Scene '{scenePath}' should not author PlayerLookController. The canonical runtime-owned player root must own look configuration.");
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
