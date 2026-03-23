namespace Reloader.UI.Toolkit.EscMenu
{
    public static class ScopedOpticsSettings
    {
        public const int MinPipResolutionPercent = 10;
        public const int MaxPipResolutionPercent = 400;
        public const int DefaultPipResolutionPercent = 100;

        public const int MinPeripheralBlurPercent = 0;
        public const int MaxPeripheralBlurPercent = 100;
        public const int DefaultPeripheralBlurPercent = 50;
    }

    public readonly struct ScopedOpticsSettingsSnapshot
    {
        public ScopedOpticsSettingsSnapshot(int pipResolutionPercent, int peripheralBlurPercent)
        {
            PipResolutionPercent = pipResolutionPercent;
            PeripheralBlurPercent = peripheralBlurPercent;
        }

        public int PipResolutionPercent { get; }
        public int PeripheralBlurPercent { get; }
    }

    public interface IScopedOpticsSettingsSource
    {
        ScopedOpticsSettingsSnapshot GetScopedOpticsSettingsSnapshot();
        int GetScopedPipResolutionPercent();
        int GetPeripheralBlurPercent();
    }
}
