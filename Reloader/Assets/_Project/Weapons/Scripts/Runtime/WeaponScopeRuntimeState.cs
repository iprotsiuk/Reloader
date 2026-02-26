using Reloader.Weapons.Data;

namespace Reloader.Weapons.Runtime
{
    public sealed class WeaponScopeRuntimeState
    {
        public WeaponScopeRuntimeState(WeaponScopeConfiguration configuration)
        {
            Configuration = configuration;
            CurrentZoom = configuration.DefaultZoom;
            CurrentZeroMeters = configuration.DefaultZeroMeters;
        }

        public WeaponScopeConfiguration Configuration { get; }
        public float CurrentZoom { get; private set; }
        public int CurrentZeroMeters { get; private set; }

        public void ApplyZoomDelta(float delta)
        {
            CurrentZoom = Configuration.ClampZoom(CurrentZoom + delta);
        }

        public void ApplyZeroSteps(int steps)
        {
            if (steps == 0)
            {
                return;
            }

            var nextZeroMeters = CurrentZeroMeters + (steps * Configuration.ZeroStepMeters);
            CurrentZeroMeters = Configuration.ClampZeroMeters(nextZeroMeters);
        }
    }
}
