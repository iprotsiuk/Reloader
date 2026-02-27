using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.PlayMode
{
    public class SceneTopologySmokeTests
    {
        private static readonly string[] RequiredFirstThreeScenes =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/_Project/World/Scenes/MainTown.unity",
            "Assets/_Project/World/Scenes/IndoorRangeInstance.unity"
        };

        [Test]
        public void BuildSettings_FirstThreeScenes_MatchWorldTopologyContract()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            Assert.That(
                sceneCount,
                Is.GreaterThanOrEqualTo(RequiredFirstThreeScenes.Length),
                $"Scene topology contract requires at least {RequiredFirstThreeScenes.Length} build scenes, but found {sceneCount}. " +
                "Expected first three: Bootstrap, MainTown, IndoorRangeInstance.");

            for (var i = 0; i < RequiredFirstThreeScenes.Length; i++)
            {
                var actualPath = SceneUtility.GetScenePathByBuildIndex(i);
                Assert.That(
                    actualPath,
                    Is.EqualTo(RequiredFirstThreeScenes[i]),
                    $"Build Settings scene at index {i} must be '{RequiredFirstThreeScenes[i]}' for world topology contract, " +
                    $"but was '{actualPath}'.");
            }
        }
    }
}
