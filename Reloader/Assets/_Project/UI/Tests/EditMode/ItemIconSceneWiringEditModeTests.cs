using System.Linq;
using NUnit.Framework;
using Reloader.UI.Editor;

namespace Reloader.UI.Tests.EditMode
{
    public class ItemIconSceneWiringEditModeTests
    {
        [Test]
        public void CandidateWorldScenePaths_AreUniqueAndPreferActiveTopologyBeforeMainWorldFallback()
        {
            var scenePaths = ItemIconSceneWiring.GetCandidateWorldScenePaths();
            var orderedPaths = scenePaths.ToList();

            Assert.That(scenePaths, Is.Not.Null);
            Assert.That(scenePaths.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(scenePaths.Distinct().Count(), Is.EqualTo(scenePaths.Count));
            Assert.That(orderedPaths[0], Is.EqualTo("Assets/Scenes/Bootstrap.unity"));
            Assert.That(scenePaths.Any(path => path == "Assets/_Project/World/Scenes/MainTown.unity"), Is.True);
            Assert.That(scenePaths.Any(path => path == "Assets/Scenes/MainWorld.unity"), Is.True);

            var mainTownIndex = orderedPaths.IndexOf("Assets/_Project/World/Scenes/MainTown.unity");
            var mainWorldIndex = orderedPaths.IndexOf("Assets/Scenes/MainWorld.unity");
            Assert.That(mainTownIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(mainWorldIndex, Is.GreaterThan(mainTownIndex));
        }
    }
}
