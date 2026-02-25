using System;
using System.Collections.Generic;

namespace Reloader.UI.Toolkit.Runtime
{
    public sealed class UiScreenRegistry
    {
        private readonly Dictionary<string, string> _screenModules = new(StringComparer.Ordinal);

        public void Register(string screenId, string moduleTypeName)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                throw new ArgumentException("Screen id is required.", nameof(screenId));
            }

            if (string.IsNullOrWhiteSpace(moduleTypeName))
            {
                throw new ArgumentException("Module type name is required.", nameof(moduleTypeName));
            }

            _screenModules[screenId] = moduleTypeName;
        }

        public bool TryGet(string screenId, out string moduleTypeName)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                moduleTypeName = null;
                return false;
            }

            return _screenModules.TryGetValue(screenId, out moduleTypeName);
        }
    }
}
