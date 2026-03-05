using NUnit.Framework;
using Reloader.Weapons.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Core.Tests
{
    public class WeaponRegistryFallbackResolutionTests
    {
        [Test]
        public void TryGetWeaponDefinition_ReturnsFalse_WhenItemIdIsNullOrWhitespace()
        {
            var root = new GameObject("WeaponRegistryNullGuardTests");
            try
            {
                var registry = root.AddComponent<WeaponRegistry>();
                Assert.That(registry.TryGetWeaponDefinition(null, out var nullDefinition), Is.False);
                Assert.That(nullDefinition, Is.Null);
                Assert.That(registry.TryGetWeaponDefinition(" ", out var whitespaceDefinition), Is.False);
                Assert.That(whitespaceDefinition, Is.Null);
                Assert.That(registry.TryGetWeaponDefinition("att-kar98k-scope-remote-a", out var nonWeaponDefinition), Is.False);
                Assert.That(nonWeaponDefinition, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TryGetWeaponDefinition_ResolvesStarterRifleWhenSerializedDefinitionsAreEmpty()
        {
#if UNITY_EDITOR
            var root = new GameObject("WeaponRegistryFallbackResolutionTests");
            try
            {
                var registry = root.AddComponent<WeaponRegistry>();

                var resolved = registry.TryGetWeaponDefinition("weapon-kar98k", out var definition);

                Assert.That(resolved, Is.True);
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.ItemId, Is.EqualTo("weapon-kar98k"));
                Assert.That(
                    AssetDatabase.GetAssetPath(definition),
                    Is.EqualTo("Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
#else
            Assert.Ignore("WeaponRegistry editor fallback only runs in editor.");
#endif
        }

    }
}
