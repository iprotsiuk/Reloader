using NUnit.Framework;
using Reloader.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Audio.Tests.PlayMode
{
    public class CombatAudioCatalogPlayModeTests
    {
        [Test]
        public void CatalogAsset_Exists_AndCanBeLoaded()
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<CombatAudioCatalog>("Assets/_Project/Audio/Data/CombatAudioCatalog.asset");
            Assert.That(asset, Is.Not.Null, "CombatAudioCatalog asset should exist at Assets/_Project/Audio/Data/CombatAudioCatalog.asset.");
#else
            Assert.Pass("Editor-only AssetDatabase validation.");
#endif
        }
    }
}
