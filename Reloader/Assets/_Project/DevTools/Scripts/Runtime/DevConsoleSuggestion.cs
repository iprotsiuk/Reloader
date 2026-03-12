namespace Reloader.DevTools.Runtime
{
    public sealed class DevConsoleSuggestion
    {
        public DevConsoleSuggestion(string token, string label, string description = "", string applyText = null)
        {
            Token = token ?? string.Empty;
            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
            ApplyText = string.IsNullOrWhiteSpace(applyText) ? Token : applyText;
        }

        public string Token { get; }
        public string Label { get; }
        public string Description { get; }
        public string ApplyText { get; }
    }
}
