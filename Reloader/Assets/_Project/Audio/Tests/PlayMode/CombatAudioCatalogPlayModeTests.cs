using NUnit.Framework;
using Reloader.Audio;
using System.Reflection;
using UnityEngine;

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

        [Test]
        public void GetRandomFireClip_SkipsNullEntries()
        {
            var catalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            var clipA = AudioClip.Create("clip-a", 128, 1, 44100, false);
            SetPrivateField(catalog, "_defaultGunshotClips", new AudioClip[] { null, clipA, null });

            var resolved = catalog.GetRandomFireClip("weapon-kar98k");
            Assert.That(resolved, Is.SameAs(clipA));

            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void GetRandomFootstepClip_IsDeterministic_PerSurface()
        {
            var catalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            var clipA = AudioClip.Create("step-a", 128, 1, 44100, false);
            var clipB = AudioClip.Create("step-b", 128, 1, 44100, false);

            var groupType = typeof(CombatAudioCatalog).GetNestedType("SurfaceAudioGroup", BindingFlags.Public);
            var group = System.Activator.CreateInstance(groupType);
            SetPrivateField(group, "_surfaceId", "Default");
            SetPrivateField(group, "_clips", new[] { clipA, clipB });
            SetPrivateField(catalog, "_footstepGroups", new[] { group });

            var first = catalog.GetRandomFootstepClip("Default");
            var second = catalog.GetRandomFootstepClip("Default");
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));

            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void GetStableFireClip_ReturnsSameClip_ForSameWeapon()
        {
            var catalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            var clipA = AudioClip.Create("shot-a", 128, 1, 44100, false);
            var clipB = AudioClip.Create("shot-b", 128, 1, 44100, false);
            var clipC = AudioClip.Create("shot-c", 128, 1, 44100, false);
            SetPrivateField(catalog, "_defaultGunshotClips", new[] { clipA, clipB, clipC });

            var first = catalog.GetStableFireClip("weapon-kar98k");
            var second = catalog.GetStableFireClip("weapon-kar98k");
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));

            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
            Object.DestroyImmediate(clipC);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Resolve_DoesNotCache_CustomCatalogAsGlobalFallback()
        {
            SetResolverCachedCatalog(null);
            var custom = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            var customResolved = CombatAudioCatalogResolver.Resolve(custom);
            Assert.That(customResolved, Is.SameAs(custom));

            var fallbackResolved = CombatAudioCatalogResolver.Resolve(null);
            Assert.That(fallbackResolved, Is.Not.SameAs(custom), "Custom catalog should not become process-wide fallback cache.");

            Object.DestroyImmediate(custom);
            SetResolverCachedCatalog(null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }

        private static void SetResolverCachedCatalog(CombatAudioCatalog value)
        {
            var field = typeof(CombatAudioCatalogResolver).GetField("_cachedCatalog", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing CombatAudioCatalogResolver._cachedCatalog field.");
            field.SetValue(null, value);
        }
    }
}
