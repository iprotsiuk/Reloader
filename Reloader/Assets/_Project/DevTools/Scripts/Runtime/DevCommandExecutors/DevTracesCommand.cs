using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevTracesCommand
    {
        private readonly DevToolsState _state;
        private readonly DevTraceRuntime _traceRuntime;

        public DevTracesCommand(DevToolsState state = null, DevTraceRuntime traceRuntime = null)
        {
            _state = state ?? new DevToolsState();
            _traceRuntime = traceRuntime;
        }

        public bool TryExecute(DevCommandParseResult parseResult, out string resultMessage)
        {
            if (parseResult.Arguments.Length < 2
                || !string.Equals(parseResult.Arguments[0], "persistent", StringComparison.OrdinalIgnoreCase)
                || !TryParseToggle(parseResult.Arguments[1], out var isEnabled))
            {
                resultMessage = "Usage: traces persistent [on|off|toggle]";
                return false;
            }

            _state.PersistentTracesEnabled = isEnabled;
            _traceRuntime?.SetPersistentTracesEnabled(isEnabled);
            resultMessage = isEnabled ? "Persistent traces enabled." : "Persistent traces disabled.";
            return true;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, DevCommandParseResult parseResult)
        {
            if (parseResult.Arguments.Length == 0)
            {
                return new[] { new DevConsoleSuggestion("persistent", "persistent", applyText: "traces persistent") };
            }

            if (!"persistent".StartsWith(parseResult.Arguments[0], StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            if (parseResult.Arguments.Length == 1)
            {
                return new[] { new DevConsoleSuggestion("persistent", "persistent", applyText: "traces persistent") };
            }

            var suggestions = new List<DevConsoleSuggestion>();
            AddToggleSuggestion(suggestions, "on", parseResult.Arguments[1]);
            AddToggleSuggestion(suggestions, "off", parseResult.Arguments[1]);
            AddToggleSuggestion(suggestions, "toggle", parseResult.Arguments[1]);
            return suggestions;
        }

        private static void AddToggleSuggestion(ICollection<DevConsoleSuggestion> suggestions, string token, string prefix)
        {
            if (!string.IsNullOrWhiteSpace(prefix)
                && !token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            suggestions.Add(new DevConsoleSuggestion(token, token, applyText: $"traces persistent {token}"));
        }

        private bool TryParseToggle(string value, out bool isEnabled)
        {
            if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            {
                isEnabled = true;
                return true;
            }

            if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
            {
                isEnabled = false;
                return true;
            }

            if (string.Equals(value, "toggle", StringComparison.OrdinalIgnoreCase))
            {
                isEnabled = !_state.PersistentTracesEnabled;
                return true;
            }

            isEnabled = false;
            return false;
        }
    }
}
