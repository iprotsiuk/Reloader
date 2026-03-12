namespace Reloader.DevTools.Runtime
{
    public sealed class DevConsoleSuggestion
    {
        public DevConsoleSuggestion(string token, string label, string description = "")
        {
            Token = token ?? string.Empty;
            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string Token { get; }
        public string Label { get; }
        public string Description { get; }
    }
}
