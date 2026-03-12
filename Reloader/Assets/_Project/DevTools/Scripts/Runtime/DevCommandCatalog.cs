using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevCommandCatalog
    {
        private readonly Dictionary<string, DevCommandDefinition> _definitionsByLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _aliasesByCommandName = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<DevCommandDefinition> _definitionsInOrder = new();

        public void Register(DevCommandDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
            {
                return;
            }

            var commandName = definition.Name;
            if (_definitionsByLookup.TryGetValue(commandName, out var existingByLookup)
                && !string.Equals(existingByLookup.Name, commandName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Command name '{commandName}' is already registered as an alias.");
            }

            var aliases = new List<string>();
            for (var i = 0; i < definition.Aliases.Count; i++)
            {
                var alias = definition.Aliases[i];
                if (string.IsNullOrWhiteSpace(alias)
                    || string.Equals(alias, commandName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_definitionsByLookup.TryGetValue(alias, out var existingAliasOwner)
                    && !string.Equals(existingAliasOwner.Name, commandName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Alias '{alias}' is already registered to command '{existingAliasOwner.Name}'.");
                }

                aliases.Add(alias);
            }

            if (_aliasesByCommandName.TryGetValue(commandName, out var oldAliases))
            {
                for (var i = 0; i < oldAliases.Count; i++)
                {
                    _definitionsByLookup.Remove(oldAliases[i]);
                }
            }

            if (_definitionsByLookup.TryGetValue(commandName, out var existingDefinition))
            {
                var existingIndex = _definitionsInOrder.FindIndex(
                    existing => ReferenceEquals(existing, existingDefinition));
                if (existingIndex >= 0)
                {
                    _definitionsInOrder[existingIndex] = definition;
                }
            }
            else
            {
                _definitionsInOrder.Add(definition);
            }

            _definitionsByLookup[commandName] = definition;
            for (var i = 0; i < aliases.Count; i++)
            {
                var alias = aliases[i];
                _definitionsByLookup[alias] = definition;
            }

            _aliasesByCommandName[commandName] = aliases;
        }

        public bool Contains(string commandName)
        {
            return !string.IsNullOrWhiteSpace(commandName)
                   && _definitionsByLookup.ContainsKey(commandName);
        }

        public bool TryGet(string commandName, out DevCommandDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                definition = null;
                return false;
            }

            return _definitionsByLookup.TryGetValue(commandName, out definition);
        }

        public IReadOnlyCollection<DevCommandDefinition> GetDefinitions()
        {
            return _definitionsInOrder.ToArray();
        }

        public static DevCommandCatalog CreateDefault()
        {
            var catalog = new DevCommandCatalog();
            catalog.Register(new DevCommandDefinition("noclip", "Toggle collision-free movement."));
            catalog.Register(new DevCommandDefinition("give", "Grant an item or resource."));
            catalog.Register(new DevCommandDefinition("traces", "Control debug trace visualization."));
            catalog.Register(new DevCommandDefinition("spawn", "Spawn a configured runtime object."));
            return catalog;
        }
    }
}
