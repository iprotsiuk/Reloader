using Reloader.Weapons.Data;

namespace Reloader.Weapons.Runtime
{
    public readonly struct WeaponScopeAdjustmentSnapshot
    {
        public WeaponScopeAdjustmentSnapshot(
            float zoom,
            int zeroMeters,
            int windageClicks,
            int elevationClicks)
        {
            Zoom = zoom;
            ZeroMeters = zeroMeters;
            WindageClicks = windageClicks;
            ElevationClicks = elevationClicks;
        }

        public float Zoom { get; }
        public int ZeroMeters { get; }
        public int WindageClicks { get; }
        public int ElevationClicks { get; }
    }

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
        public int CurrentWindageClicks { get; private set; }
        public int CurrentElevationClicks { get; private set; }

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

        public void ApplyWindageClicks(int clicks)
        {
            if (clicks == 0)
            {
                return;
            }

            CurrentWindageClicks += clicks;
        }

        public void ApplyElevationClicks(int clicks)
        {
            if (clicks == 0)
            {
                return;
            }

            CurrentElevationClicks += clicks;
        }

        public WeaponScopeAdjustmentSnapshot CreateAdjustmentSnapshot()
        {
            return new WeaponScopeAdjustmentSnapshot(
                CurrentZoom,
                CurrentZeroMeters,
                CurrentWindageClicks,
                CurrentElevationClicks);
        }

        public void RestoreAdjustmentSnapshot(WeaponScopeAdjustmentSnapshot snapshot)
        {
            CurrentZoom = Configuration.ClampZoom(snapshot.Zoom);
            CurrentZeroMeters = Configuration.ClampZeroMeters(snapshot.ZeroMeters);
            CurrentWindageClicks = snapshot.WindageClicks;
            CurrentElevationClicks = snapshot.ElevationClicks;
        }
    }
}
