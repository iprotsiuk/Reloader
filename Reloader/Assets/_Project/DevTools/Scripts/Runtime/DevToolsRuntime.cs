using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevToolsRuntime
    {
        private readonly DevCommandCatalog _catalog;

        public DevToolsRuntime()
            : this(DevCommandCatalog.CreateDefault(), new DevToolsState(), new DevCommandContext())
        {
        }

        public DevToolsRuntime(DevCommandCatalog catalog, DevToolsState state, DevCommandContext context)
        {
            _catalog = catalog ?? DevCommandCatalog.CreateDefault();
            State = state ?? new DevToolsState();
            Context = context ?? new DevCommandContext();
        }

        public DevToolsState State { get; }
        public DevCommandContext Context { get; }
        public bool IsConsoleVisible { get; private set; }

        public bool TryExecute(string input, out string resultMessage)
        {
            var parseResult = DevCommandLineParser.Parse(input);
            if (!parseResult.HasCommand)
            {
                resultMessage = "No command provided.";
                return false;
            }

            if (!_catalog.TryGet(parseResult.CommandName, out _))
            {
                resultMessage = $"Unknown command '{parseResult.CommandName}'.";
                return false;
            }

            resultMessage = $"Command '{parseResult.CommandName}' is registered.";
            return true;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, int highlightedIndex)
        {
            var parseResult = DevCommandLineParser.Parse(input);
            var prefix = parseResult.HasCommand ? parseResult.CommandName : input?.Trim() ?? string.Empty;
            var suggestions = new List<DevConsoleSuggestion>();

            foreach (var definition in _catalog.GetDefinitions())
            {
                if (!TryResolveSuggestionLookup(definition, prefix, out var lookupToken))
                {
                    continue;
                }

                var label = string.Equals(lookupToken, definition.Name, StringComparison.OrdinalIgnoreCase)
                    ? definition.Name
                    : $"{definition.Name} ({lookupToken})";
                suggestions.Add(new DevConsoleSuggestion(lookupToken, label, definition.Description));
            }

            return suggestions;
        }

        public void SetConsoleVisible(bool isVisible)
        {
            IsConsoleVisible = isVisible;
        }

        private static bool TryResolveSuggestionLookup(
            DevCommandDefinition definition,
            string prefix,
            out string lookupToken)
        {
            lookupToken = definition?.Name ?? string.Empty;
            if (definition == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return true;
            }

            if (definition.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                lookupToken = definition.Name;
                return true;
            }

            for (var i = 0; i < definition.Aliases.Count; i++)
            {
                var alias = definition.Aliases[i];
                if (!string.IsNullOrWhiteSpace(alias)
                    && alias.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    lookupToken = alias;
                    return true;
                }
            }

            return false;
        }
    }
}
