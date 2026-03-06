using NUnit.Framework;
using Reloader.Weapons.Runtime;
using UnityEngine;

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
        public void TryGetWeaponDefinition_ReturnsFalseWhenSerializedDefinitionsAreEmpty()
        {
            var root = new GameObject("WeaponRegistryFallbackResolutionTests");
            try
            {
                var registry = root.AddComponent<WeaponRegistry>();

                var resolved = registry.TryGetWeaponDefinition("weapon-kar98k", out var definition);

                Assert.That(resolved, Is.False);
                Assert.That(definition, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

    }
}
