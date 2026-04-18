namespace Reloader.Weapons.Ballistics
{
    public readonly struct AmmoBallisticSnapshot
    {
        public AmmoBallisticSnapshot(
            AmmoSourceType ammoSource,
            float muzzleVelocityFps,
            float velocityStdDevFps,
            float projectileMassGrains,
            float ballisticCoefficientG1,
            float dispersionMoa,
            string displayName = null,
            string cartridgeId = null,
            string ammoItemId = null,
            float coverPenetrationPower = 0f)
        {
            AmmoSource = ammoSource;
            MuzzleVelocityFps = muzzleVelocityFps;
            VelocityStdDevFps = velocityStdDevFps;
            ProjectileMassGrains = projectileMassGrains;
            BallisticCoefficientG1 = ballisticCoefficientG1;
            DispersionMoa = dispersionMoa;
            CoverPenetrationPower = coverPenetrationPower < 0f ? 0f : coverPenetrationPower;
            DisplayName = displayName;
            CartridgeId = cartridgeId;
            AmmoItemId = ammoItemId;
        }

        public AmmoSourceType AmmoSource { get; }
        public float MuzzleVelocityFps { get; }
        public float VelocityStdDevFps { get; }
        public float ProjectileMassGrains { get; }
        public float BallisticCoefficientG1 { get; }
        public float DispersionMoa { get; }
        public float CoverPenetrationPower { get; }
        public string DisplayName { get; }
        public string CartridgeId { get; }
        public string AmmoItemId { get; }
    }
}
