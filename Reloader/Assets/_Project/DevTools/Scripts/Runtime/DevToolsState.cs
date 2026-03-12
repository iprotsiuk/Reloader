namespace Reloader.DevTools.Runtime
{
    public sealed class DevToolsState
    {
        public bool NoclipEnabled { get; set; }
        public float NoclipSpeed { get; set; } = 8f;
        public float TraceTtlSeconds { get; set; }
    }
}
