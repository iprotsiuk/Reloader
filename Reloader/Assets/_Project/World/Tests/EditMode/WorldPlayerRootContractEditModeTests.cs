using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public sealed class WorldPlayerRootContractEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";

        [TestCase(MainTownScenePath)]
        [TestCase(IndoorRangeScenePath)]
        public void Scene_DoesNotAuthorPlayerRoot(string scenePath)
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                var playerRoot = FindRootGameObject(scene, "PlayerRoot");
                Assert.That(playerRoot, Is.Null,
                    $"Scene '{scenePath}' should expose anchors and local services only. PlayerRoot must be runtime-owned.");
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

        private static GameObject FindRootGameObject(Scene scene, string rootName)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i];
                if (candidate != null && candidate.name == rootName)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
