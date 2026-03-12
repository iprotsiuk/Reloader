using System;

namespace Reloader.DevTools.Runtime
{
    public readonly struct DevCommandParseResult
    {
        public static readonly DevCommandParseResult Empty = new(string.Empty, Array.Empty<string>(), Array.Empty<string>());

        public DevCommandParseResult(string commandName, string[] arguments, string[] tokens)
        {
            CommandName = commandName ?? string.Empty;
            Arguments = arguments ?? Array.Empty<string>();
            Tokens = tokens ?? Array.Empty<string>();
        }

        public string CommandName { get; }
        public string[] Arguments { get; }
        public string[] Tokens { get; }
        public bool HasCommand => !string.IsNullOrWhiteSpace(CommandName);
    }
}
