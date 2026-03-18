using System.Linq;
using NUnit.Framework;
using Reloader.DevTools.Data;
using Reloader.DevTools.Runtime;
using UnityEngine;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class DevNpcSpawnCatalogTests
    {
        [Test]
        public void Catalog_ResolvesStableSpawnIdToPrefab()
        {
            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcPolicePrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.police", "Police Officer", prefab)
            });

            var resolved = catalog.TryResolve("npc.police", out var entry);

            Assert.That(resolved, Is.True);
            Assert.That(entry.SpawnId, Is.EqualTo("npc.police"));
            Assert.That(entry.DisplayName, Is.EqualTo("Police Officer"));
            Assert.That(entry.Prefab, Is.SameAs(prefab));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void SpawnCommand_SuggestionsComeDirectlyFromCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var policePrefab = new GameObject("NpcPolicePrefab");
            policePrefab.SetActive(false);
            var clerkPrefab = new GameObject("NpcClerkPrefab");
            clerkPrefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.police", "Police Officer", policePrefab),
                new DevNpcSpawnCatalog.Entry("npc.front-desk-clerk", "Front Desk Clerk", clerkPrefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            var suggestions = command
                .GetSuggestions("spawn npc npc.f", DevCommandLineParser.Parse("spawn npc npc.f"))
                .ToArray();

            Assert.That(suggestions.Select(static suggestion => suggestion.Token), Is.EqualTo(new[] { "npc.front-desk-clerk" }));
            Assert.That(suggestions[0].ApplyText, Is.EqualTo("spawn npc npc.front-desk-clerk"));
            Assert.That(suggestions[0].Label, Is.EqualTo("Front Desk Clerk"));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(policePrefab);
            Object.DestroyImmediate(clerkPrefab);
        }

        [Test]
        public void SpawnCommand_SuggestionsForTrailingSpaceIncludeViableTokens()
        {
            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var policePrefab = new GameObject("NpcPolicePrefab");
            policePrefab.SetActive(false);
            var clerkPrefab = new GameObject("NpcClerkPrefab");
            clerkPrefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.police", "Police Officer", policePrefab),
                new DevNpcSpawnCatalog.Entry("npc.front-desk-clerk", "Front Desk Clerk", clerkPrefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            var suggestions = command
                .GetSuggestions("spawn npc ", DevCommandLineParser.Parse("spawn npc "))
                .ToArray();

            Assert.That(suggestions.Select(static suggestion => suggestion.Token), Does.Contain("npc.police"));
            Assert.That(suggestions.Select(static suggestion => suggestion.Token), Does.Contain("npc.front-desk-clerk"));
            Assert.That(suggestions.Select(static suggestion => suggestion.Token), Does.Contain("random"));
            Assert.That(suggestions.Select(static suggestion => suggestion.Token), Does.Contain("randomContract"));
            Assert.That(suggestions.First(suggestion => suggestion.Token == "random").ApplyText, Is.EqualTo("spawn npc random"));
            Assert.That(suggestions.First(suggestion => suggestion.Token == "randomContract").ApplyText, Is.EqualTo("spawn npc randomContract"));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(policePrefab);
            Object.DestroyImmediate(clerkPrefab);
        }

        [Test]
        public void SpawnCommand_RandomContract_RequiresCivilianRuntimeBridge()
        {
            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var policePrefab = new GameObject("NpcPolicePrefab");
            policePrefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.police", "Police Officer", policePrefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            var executed = command.TryExecute(DevCommandLineParser.Parse("spawn npc randomContract"), out var resultMessage);

            Assert.That(executed, Is.False);
            Assert.That(resultMessage, Is.EqualTo("No civilian population runtime is available for randomContract spawning."));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(policePrefab);
        }
    }
}
