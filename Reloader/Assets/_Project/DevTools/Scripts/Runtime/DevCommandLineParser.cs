using System;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    public static class DevCommandLineParser
    {
        public static DevCommandParseResult Parse(string input)
        {
            var tokens = Tokenize(input);
            if (tokens.Count == 0)
            {
                return DevCommandParseResult.Empty;
            }

            var commandName = tokens[0];
            var allTokens = tokens.ToArray();
            if (tokens.Count == 1)
            {
                return new DevCommandParseResult(commandName, Array.Empty<string>(), allTokens);
            }

            var arguments = tokens.GetRange(1, tokens.Count - 1).ToArray();
            return new DevCommandParseResult(commandName, arguments, allTokens);
        }

        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
            {
                return tokens;
            }

            var parts = input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                tokens.Add(parts[i]);
            }

            return tokens;
        }
    }
}
