using System;
using System.Collections.Generic;
using Reloader.DevTools.Data;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevSpawnNpcCommand
    {
        private const string SpawnTypeToken = "npc";
        private const string RandomSpawnToken = "random";
        private const string RandomContractSpawnToken = "randomContract";

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
            if (parseResult.Arguments.Length < 1
                || !string.Equals(parseResult.Arguments[0], SpawnTypeToken, StringComparison.OrdinalIgnoreCase))
            {
                resultMessage = "Usage: spawn npc <spawn-id>";
                return false;
            }

            if (parseResult.Arguments.Length < 2)
            {
                resultMessage = "Usage: spawn npc <spawn-id>";
                return false;
            }

            var spawnToken = parseResult.Arguments[1];
            var spawnService = ResolveSpawnService(context);
            if (spawnService == null)
            {
                return Fail(out resultMessage);
            }

            if (string.Equals(spawnToken, RandomContractSpawnToken, StringComparison.OrdinalIgnoreCase))
            {
                var civilianBridge = context?.ResolveCivilianPopulationRuntimeBridge();
                if (civilianBridge == null)
                {
                    resultMessage = "No civilian population runtime is available for randomContract spawning.";
                    return false;
                }

                if (!spawnService.TryResolveSpawnPose(out var spawnPosition, out var spawnRotation, out resultMessage))
                {
                    return false;
                }

                return civilianBridge.TrySpawnDebugContractCivilian(spawnPosition, spawnRotation, out _, out resultMessage);
            }

            return string.Equals(spawnToken, RandomSpawnToken, StringComparison.OrdinalIgnoreCase)
                ? spawnService.TrySpawnRandom(out _, out resultMessage)
                : spawnService.TrySpawn(spawnToken, out _, out resultMessage);
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
                return new[] { new DevConsoleSuggestion(SpawnTypeToken, SpawnTypeToken, applyText: "spawn npc") };
            }

            if (!SpawnTypeToken.StartsWith(parseResult.Arguments[0], StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            if (parseResult.Arguments.Length == 1)
            {
                return IsNextTokenInput(input)
                    ? BuildSpawnTargetSuggestions(catalog, string.Empty)
                    : new[] { new DevConsoleSuggestion(SpawnTypeToken, SpawnTypeToken, applyText: "spawn npc") };
            }

            return BuildSpawnTargetSuggestions(catalog, parseResult.Arguments[1]);
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

        private static IReadOnlyList<DevConsoleSuggestion> BuildSpawnTargetSuggestions(
            DevNpcSpawnCatalog catalog,
            string prefix)
        {
            var suggestions = new List<DevConsoleSuggestion>();

            AddSuggestionIfMatch(
                suggestions,
                RandomSpawnToken,
                "Spawn a random configured NPC.",
                prefix);
            AddSuggestionIfMatch(
                suggestions,
                RandomContractSpawnToken,
                "Spawn a contract-eligible NPC through the shared contract seam.",
                prefix);

            var entries = catalog.GetSuggestions(prefix);
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

        private static void AddSuggestionIfMatch(
            ICollection<DevConsoleSuggestion> suggestions,
            string token,
            string description,
            string prefix)
        {
            if (!string.IsNullOrWhiteSpace(prefix)
                && !token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            suggestions.Add(new DevConsoleSuggestion(
                token,
                token,
                description,
                $"spawn npc {token}"));
        }

        private static bool IsNextTokenInput(string input)
        {
            return !string.IsNullOrEmpty(input) && char.IsWhiteSpace(input[input.Length - 1]);
        }
    }
}
