using System;
using Reloader.Weapons.Ballistics;

namespace Reloader.Weapons.Runtime
{
    public static class WeaponAmmoDefaults
    {
        public const string DefaultAmmoDisplayName = "Factory .308 147gr FMJ";
        public const string DefaultAmmoItemId = "ammo-factory-308-147-fmj";
        public const float DefaultMuzzleVelocityFps = 2780f;
        public const float DefaultVelocityStdDevFps = 55f;
        public const float DefaultProjectileMassGrains = 147f;
        public const float DefaultBallisticCoefficientG1 = 0.398f;
        public const float DefaultDispersionMoa = 4.5f;
        public const float DefaultCoverPenetrationPower = 0f;
        public const string SpecialtyAmmoDisplayName = ".308 150gr AP";
        public const string SpecialtyAmmoItemId = "ammo-specialty-308-150-ap";
        public const float SpecialtyProjectileMassGrains = 150f;
        public const float SpecialtyCoverPenetrationPower = 1f;

        public static string NormalizeAmmoItemId(string ammoItemId)
        {
            return string.IsNullOrWhiteSpace(ammoItemId) ? DefaultAmmoItemId : ammoItemId;
        }

        public static string NormalizeSpecialtyAmmoItemId(string ammoItemId)
        {
            return string.IsNullOrWhiteSpace(ammoItemId) ? SpecialtyAmmoItemId : ammoItemId;
        }

        public static string NormalizeDisplayName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? DefaultAmmoDisplayName : displayName;
        }

        public static AmmoBallisticSnapshot BuildDefaultRound()
        {
            return new AmmoBallisticSnapshot(
                AmmoSourceType.Factory,
                DefaultMuzzleVelocityFps,
                DefaultVelocityStdDevFps,
                DefaultProjectileMassGrains,
                DefaultBallisticCoefficientG1,
                DefaultDispersionMoa,
                DefaultAmmoDisplayName,
                Guid.NewGuid().ToString("N"),
                DefaultAmmoItemId,
                DefaultCoverPenetrationPower);
        }

        public static AmmoBallisticSnapshot BuildRoundFromTemplate(AmmoBallisticSnapshot template)
        {
            return new AmmoBallisticSnapshot(
                template.AmmoSource,
                template.MuzzleVelocityFps,
                template.VelocityStdDevFps,
                template.ProjectileMassGrains,
                template.BallisticCoefficientG1,
                template.DispersionMoa,
                NormalizeDisplayName(template.DisplayName),
                Guid.NewGuid().ToString("N"),
                NormalizeAmmoItemId(template.AmmoItemId),
                template.CoverPenetrationPower);
        }

        public static AmmoBallisticSnapshot BuildFactoryRound(string ammoItemId)
        {
            return new AmmoBallisticSnapshot(
                AmmoSourceType.Factory,
                DefaultMuzzleVelocityFps,
                DefaultVelocityStdDevFps,
                DefaultProjectileMassGrains,
                DefaultBallisticCoefficientG1,
                DefaultDispersionMoa,
                DefaultAmmoDisplayName,
                Guid.NewGuid().ToString("N"),
                NormalizeAmmoItemId(ammoItemId),
                DefaultCoverPenetrationPower);
        }

        public static AmmoBallisticSnapshot BuildSpecialtyRound(string ammoItemId = SpecialtyAmmoItemId)
        {
            return new AmmoBallisticSnapshot(
                AmmoSourceType.Factory,
                DefaultMuzzleVelocityFps,
                DefaultVelocityStdDevFps,
                SpecialtyProjectileMassGrains,
                DefaultBallisticCoefficientG1,
                DefaultDispersionMoa,
                SpecialtyAmmoDisplayName,
                Guid.NewGuid().ToString("N"),
                NormalizeSpecialtyAmmoItemId(ammoItemId),
                SpecialtyCoverPenetrationPower);
        }
    }
}
