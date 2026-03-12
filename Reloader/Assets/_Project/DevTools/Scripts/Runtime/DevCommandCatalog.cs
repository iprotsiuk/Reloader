using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevCommandCatalog
    {
        private readonly Dictionary<string, DevCommandDefinition> _definitionsByName = new(StringComparer.OrdinalIgnoreCase);

        public void Register(DevCommandDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
            {
                return;
            }

            _definitionsByName[definition.Name] = definition;
            for (var i = 0; i < definition.Aliases.Count; i++)
            {
                var alias = definition.Aliases[i];
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                _definitionsByName[alias] = definition;
            }
        }

        public bool Contains(string commandName)
        {
            return !string.IsNullOrWhiteSpace(commandName)
                   && _definitionsByName.ContainsKey(commandName);
        }

        public bool TryGet(string commandName, out DevCommandDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                definition = null;
                return false;
            }

            return _definitionsByName.TryGetValue(commandName, out definition);
        }

        public IReadOnlyCollection<DevCommandDefinition> GetDefinitions()
        {
            var uniqueDefinitions = new HashSet<DevCommandDefinition>(_definitionsByName.Values);
            return new List<DevCommandDefinition>(uniqueDefinitions);
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
