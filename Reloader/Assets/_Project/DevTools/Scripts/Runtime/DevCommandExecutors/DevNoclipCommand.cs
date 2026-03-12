using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevNoclipCommand
    {
        private readonly DevPlayerMovementOverride _movementOverride;

        public DevNoclipCommand(DevPlayerMovementOverride movementOverride)
        {
            _movementOverride = movementOverride ?? new DevPlayerMovementOverride();
        }

        public bool TryExecute(DevCommandParseResult parseResult, out string resultMessage)
        {
            if (parseResult.Arguments.Length == 0)
            {
                _movementOverride.ToggleNoclip();
                resultMessage = _movementOverride.IsNoclipEnabled ? "Noclip enabled." : "Noclip disabled.";
                return true;
            }

            var firstArgument = parseResult.Arguments[0];
            if (string.Equals(firstArgument, "speed", StringComparison.OrdinalIgnoreCase))
            {
                if (parseResult.Arguments.Length < 2
                    || !float.TryParse(parseResult.Arguments[1], out var parsedSpeed)
                    || parsedSpeed <= 0f)
                {
                    resultMessage = "Usage: noclip speed <value>";
                    return false;
                }

                _movementOverride.SetNoclipSpeed(parsedSpeed);
                resultMessage = $"Noclip speed set to {_movementOverride.NoclipSpeed:0.##}.";
                return true;
            }

            if (TryParseToggle(firstArgument, out var enabled, out var isToggle))
            {
                if (isToggle)
                {
                    _movementOverride.ToggleNoclip();
                }
                else
                {
                    _movementOverride.SetNoclipEnabled(enabled);
                }

                resultMessage = _movementOverride.IsNoclipEnabled ? "Noclip enabled." : "Noclip disabled.";
                return true;
            }

            resultMessage = "Usage: noclip [on|off|toggle|speed <value>]";
            return false;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, DevCommandParseResult parseResult)
        {
            var suggestions = new List<DevConsoleSuggestion>();
            var argumentPrefix = parseResult.Arguments.Length == 0
                ? string.Empty
                : parseResult.Arguments[0];

            AddSuggestionIfMatch(suggestions, "on", argumentPrefix);
            AddSuggestionIfMatch(suggestions, "off", argumentPrefix);
            AddSuggestionIfMatch(suggestions, "toggle", argumentPrefix);
            AddSuggestionIfMatch(suggestions, "speed", argumentPrefix);
            return suggestions;
        }

        private static bool TryParseToggle(string argument, out bool enabled, out bool isToggle)
        {
            enabled = false;
            isToggle = false;
            if (string.Equals(argument, "on", StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                return true;
            }

            if (string.Equals(argument, "off", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(argument, "toggle", StringComparison.OrdinalIgnoreCase))
            {
                isToggle = true;
                return true;
            }

            return false;
        }

        private static void AddSuggestionIfMatch(ICollection<DevConsoleSuggestion> suggestions, string value, string prefix)
        {
            if (!string.IsNullOrWhiteSpace(prefix)
                && !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            suggestions.Add(new DevConsoleSuggestion(value, value, applyText: $"noclip {value}"));
        }
    }
}
