using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevCommandDefinition
    {
        public DevCommandDefinition(string name, string description, IReadOnlyList<string> aliases = null)
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Aliases = aliases ?? Array.Empty<string>();
        }

        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<string> Aliases { get; }
    }
}
