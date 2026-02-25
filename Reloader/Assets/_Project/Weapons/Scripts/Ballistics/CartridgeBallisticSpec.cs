namespace Reloader.Weapons.Ballistics
{
    public readonly struct CartridgeBallisticSpec
    {
        public CartridgeBallisticSpec(
            float muzzleVelocityFps,
            float projectileMassGrains,
            float ballisticCoefficientG1,
            float dispersionMoa)
        {
            MuzzleVelocityFps = muzzleVelocityFps;
            ProjectileMassGrains = projectileMassGrains;
            BallisticCoefficientG1 = ballisticCoefficientG1;
            DispersionMoa = dispersionMoa;
        }

        public float MuzzleVelocityFps { get; }
        public float ProjectileMassGrains { get; }
        public float BallisticCoefficientG1 { get; }
        public float DispersionMoa { get; }
    }
}
