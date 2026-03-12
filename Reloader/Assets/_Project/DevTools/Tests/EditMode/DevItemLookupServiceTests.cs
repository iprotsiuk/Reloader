using System.Linq;
using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.DevTools.Runtime;
using UnityEngine;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class DevItemLookupServiceTests
    {
        [Test]
        public void Suggestions_RankDefinitionIdPrefixBeforeDisplayNameContains()
        {
            var ammo = ScriptableObject.CreateInstance<ItemDefinition>();
            var cleaner = ScriptableObject.CreateInstance<ItemDefinition>();
            ammo.SetValuesForTests(
                "ammo-308",
                ItemCategory.Consumable,
                "308 Winchester FMJ",
                ItemStackPolicy.StackByDefinition,
                maxStack: 999);
            cleaner.SetValuesForTests(
                "tool-cleaner",
                ItemCategory.Tool,
                "Ammo Bench Cleaner");

            try
            {
                var lookup = new DevItemLookupService(new[] { cleaner, ammo });

                var suggestions = lookup.GetSuggestions("ammo")
                    .Select(static suggestion => suggestion.Token)
                    .ToArray();

                Assert.That(suggestions, Is.EqualTo(new[] { "ammo-308", "tool-cleaner" }));
            }
            finally
            {
                Object.DestroyImmediate(ammo);
                Object.DestroyImmediate(cleaner);
            }
        }

        [Test]
        public void Resolve_ByDisplayName_ReturnsStableDefinitionId()
        {
            var ammo = ScriptableObject.CreateInstance<ItemDefinition>();
            ammo.SetValuesForTests(
                "ammo-308",
                ItemCategory.Consumable,
                "308 Winchester FMJ",
                ItemStackPolicy.StackByDefinition,
                maxStack: 999);

            try
            {
                var lookup = new DevItemLookupService(new[] { ammo });

                var resolved = lookup.Resolve("308 Winchester FMJ");

                Assert.That(resolved, Is.Not.Null);
                Assert.That(resolved.DefinitionId, Is.EqualTo("ammo-308"));
            }
            finally
            {
                Object.DestroyImmediate(ammo);
            }
        }
    }
}
