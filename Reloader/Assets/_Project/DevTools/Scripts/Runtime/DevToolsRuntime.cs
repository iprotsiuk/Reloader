using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevToolsRuntime
    {
        private readonly DevCommandCatalog _catalog;
        private readonly DevGiveItemCommand _giveItemCommand = new();

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

            if (!_catalog.TryGet(parseResult.CommandName, out var definition))
            {
                resultMessage = $"Unknown command '{parseResult.CommandName}'.";
                return false;
            }

            if (string.Equals(definition.Name, "give", StringComparison.OrdinalIgnoreCase))
            {
                return _giveItemCommand.TryExecute(Context, parseResult, out resultMessage);
            }

            resultMessage = $"Command '{parseResult.CommandName}' is registered.";
            return true;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, int highlightedIndex)
        {
            var parseResult = DevCommandLineParser.Parse(input);
            if (parseResult.HasCommand
                && _catalog.TryGet(parseResult.CommandName, out var definition)
                && string.Equals(definition.Name, "give", StringComparison.OrdinalIgnoreCase))
            {
                return _giveItemCommand.GetSuggestions(Context, input, parseResult);
            }

            var prefix = parseResult.HasCommand ? parseResult.CommandName : input?.Trim() ?? string.Empty;
            var suggestions = new List<DevConsoleSuggestion>();

            foreach (var catalogDefinition in _catalog.GetDefinitions())
            {
                if (!TryResolveSuggestionLookup(catalogDefinition, prefix, out var lookupToken))
                {
                    continue;
                }

                var label = string.Equals(lookupToken, catalogDefinition.Name, StringComparison.OrdinalIgnoreCase)
                    ? catalogDefinition.Name
                    : $"{catalogDefinition.Name} ({lookupToken})";
                suggestions.Add(new DevConsoleSuggestion(lookupToken, label, catalogDefinition.Description, lookupToken));
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
