using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

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
            if (parseResult.Arguments.Length != 1)
            {
                resultMessage = "Usage: trace <seconds|-1|clear>";
                return false;
            }

            if (string.Equals(parseResult.Arguments[0], "clear", StringComparison.OrdinalIgnoreCase))
            {
                _traceRuntime?.ClearVisibleTraces();
                resultMessage = "Visible traces cleared.";
                return true;
            }

            if (!TryParseTtl(parseResult.Arguments[0], out var ttlSeconds))
            {
                resultMessage = "Usage: trace <seconds|-1|clear>";
                return false;
            }

            _state.TraceTtlSeconds = ttlSeconds;
            _traceRuntime?.SetTraceTtlSeconds(ttlSeconds);
            resultMessage = FormatResultMessage(ttlSeconds);
            return true;
        }

        public IReadOnlyList<DevConsoleSuggestion> GetSuggestions(string input, DevCommandParseResult parseResult)
        {
            if (parseResult.Arguments.Length == 0)
            {
                return BuildTtlSuggestions(string.Empty);
            }

            if (parseResult.Arguments.Length > 1)
            {
                return Array.Empty<DevConsoleSuggestion>();
            }

            return BuildTtlSuggestions(parseResult.Arguments[0]);
        }

        private static IReadOnlyList<DevConsoleSuggestion> BuildTtlSuggestions(string prefix)
        {
            var suggestions = new List<DevConsoleSuggestion>();
            AddSuggestion(suggestions, "clear", "clear visible traces", prefix);
            AddSuggestion(suggestions, "-1", "permanent", prefix);
            AddSuggestion(suggestions, "0", "disable", prefix);
            AddSuggestion(suggestions, "1", "1 second", prefix);
            AddSuggestion(suggestions, "5", "5 seconds", prefix);
            return suggestions;
        }

        private static void AddSuggestion(ICollection<DevConsoleSuggestion> suggestions, string token, string label, string prefix)
        {
            if (!string.IsNullOrWhiteSpace(prefix)
                && !token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            suggestions.Add(new DevConsoleSuggestion(token, label, applyText: $"trace {token}"));
        }

        private static bool TryParseTtl(string value, out float ttlSeconds)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out ttlSeconds))
            {
                ttlSeconds = 0f;
                return false;
            }

            if (ttlSeconds < 0f && !Mathf.Approximately(ttlSeconds, -1f))
            {
                ttlSeconds = 0f;
                return false;
            }

            return true;
        }

        private static string FormatResultMessage(float ttlSeconds)
        {
            if (Mathf.Approximately(ttlSeconds, 0f))
            {
                return "Trace TTL disabled and visible traces cleared.";
            }

            if (ttlSeconds < 0f)
            {
                return "Trace TTL set to permanent.";
            }

            var wholeSeconds = Mathf.RoundToInt(ttlSeconds);
            if (Mathf.Approximately(ttlSeconds, wholeSeconds))
            {
                var suffix = wholeSeconds == 1 ? "second" : "seconds";
                return $"Trace TTL set to {wholeSeconds} {suffix}.";
            }

            return $"Trace TTL set to {ttlSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds.";
        }
    }
}
