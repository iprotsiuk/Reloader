using System;
using System.Collections.Generic;
using System.Linq;
using Reloader.Core.Items;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevItemLookupService
    {
        private readonly IReadOnlyList<ItemDefinition> _definitions;

        public DevItemLookupService(IReadOnlyList<ItemDefinition> definitions)
        {
            _definitions = definitions ?? Array.Empty<ItemDefinition>();
        }

        public ItemDefinition Resolve(string rawToken)
        {
            return TryResolve(rawToken, out var definition) ? definition : null;
        }

        public bool TryResolve(string rawToken, out ItemDefinition definition)
        {
            definition = null;
            var matches = FindMatches(rawToken);
            if (matches.Count == 0)
            {
                return false;
            }

            definition = matches[0].Definition;
            return definition != null;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string rawToken)
        {
            var matches = FindMatches(rawToken);
            var suggestions = new List<DevConsoleSuggestion>(matches.Count);
            for (var i = 0; i < matches.Count; i++)
            {
                var definition = matches[i].Definition;
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(definition.DisplayName)
                    ? definition.DefinitionId
                    : $"{definition.DefinitionId} | {definition.DisplayName}";
                suggestions.Add(new DevConsoleSuggestion(definition.DefinitionId, label));
            }

            return suggestions;
        }

        private List<LookupMatch> FindMatches(string rawToken)
        {
            var normalizedToken = Normalize(rawToken);
            var matches = new List<LookupMatch>();

            for (var i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                var score = GetMatchScore(definition, normalizedToken);
                if (score < 0)
                {
                    continue;
                }

                matches.Add(new LookupMatch(definition, score));
            }

            return matches
                .OrderBy(static match => match.Score)
                .ThenBy(static match => match.Definition.DefinitionId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int GetMatchScore(ItemDefinition definition, string normalizedToken)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
            {
                return -1;
            }

            if (string.IsNullOrEmpty(normalizedToken))
            {
                return 5;
            }

            var normalizedId = Normalize(definition.DefinitionId);
            var normalizedDisplayName = Normalize(definition.DisplayName);
            if (normalizedId == normalizedToken)
            {
                return 0;
            }

            if (normalizedDisplayName == normalizedToken)
            {
                return 1;
            }

            if (normalizedId.StartsWith(normalizedToken, StringComparison.Ordinal))
            {
                return 2;
            }

            if (normalizedDisplayName.StartsWith(normalizedToken, StringComparison.Ordinal))
            {
                return 3;
            }

            if (normalizedId.Contains(normalizedToken, StringComparison.Ordinal))
            {
                return 4;
            }

            if (normalizedDisplayName.Contains(normalizedToken, StringComparison.Ordinal))
            {
                return 5;
            }

            return -1;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private readonly struct LookupMatch
        {
            public LookupMatch(ItemDefinition definition, int score)
            {
                Definition = definition;
                Score = score;
            }

            public ItemDefinition Definition { get; }
            public int Score { get; }
        }
    }
}
