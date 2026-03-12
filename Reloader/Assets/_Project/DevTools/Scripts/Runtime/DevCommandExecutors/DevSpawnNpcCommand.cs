using System;
using System.Collections.Generic;
using Reloader.DevTools.Data;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevSpawnNpcCommand
    {
        private readonly DevNpcSpawnService _spawnService;
        private readonly DevNpcSpawnCatalog _catalog;

        public DevSpawnNpcCommand(DevNpcSpawnService spawnService = null, DevNpcSpawnCatalog catalog = null)
        {
            _spawnService = spawnService;
            _catalog = catalog;
        }

        public bool TryExecute(DevCommandParseResult parseResult, out string resultMessage)
        {
            return TryExecute(null, parseResult, out resultMessage);
        }

        public bool TryExecute(DevCommandContext context, DevCommandParseResult parseResult, out string resultMessage)
        {
            if (parseResult.Arguments.Length < 2
                || !string.Equals(parseResult.Arguments[0], "npc", StringComparison.OrdinalIgnoreCase))
            {
                resultMessage = "Usage: spawn npc <spawn-id>";
                return false;
            }

            return ResolveSpawnService(context) != null
                ? ResolveSpawnService(context).TrySpawn(parseResult.Arguments[1], out _, out resultMessage)
                : Fail(out resultMessage);
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, DevCommandParseResult parseResult)
        {
            return GetSuggestions(null, input, parseResult);
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(
            DevCommandContext context,
            string input,
            DevCommandParseResult parseResult)
        {
            var catalog = ResolveCatalog(context);
            if (catalog == null)
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            if (parseResult.Arguments.Length == 0)
            {
                return new[] { new DevConsoleSuggestion("npc", "npc", applyText: "spawn npc") };
            }

            if (!"npc".StartsWith(parseResult.Arguments[0], StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            if (parseResult.Arguments.Length == 1)
            {
                return new[] { new DevConsoleSuggestion("npc", "npc", applyText: "spawn npc") };
            }

            var suggestions = new List<DevConsoleSuggestion>();
            var entries = catalog.GetSuggestions(parseResult.Arguments[1]);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                suggestions.Add(new DevConsoleSuggestion(
                    entry.SpawnId,
                    entry.DisplayName,
                    applyText: $"spawn npc {entry.SpawnId}"));
            }

            return suggestions;
        }

        private DevNpcSpawnCatalog ResolveCatalog(DevCommandContext context)
        {
            return _catalog ?? context?.ResolveNpcSpawnCatalog();
        }

        private DevNpcSpawnService ResolveSpawnService(DevCommandContext context)
        {
            return _spawnService ?? context?.ResolveNpcSpawnService();
        }

        private static bool Fail(out string resultMessage)
        {
            resultMessage = "NPC spawn service is unavailable.";
            return false;
        }
    }
}
