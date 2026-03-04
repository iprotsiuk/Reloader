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

        public static string NormalizeAmmoItemId(string ammoItemId)
        {
            return string.IsNullOrWhiteSpace(ammoItemId) ? DefaultAmmoItemId : ammoItemId;
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
                DefaultAmmoItemId);
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
                NormalizeAmmoItemId(template.AmmoItemId));
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
                NormalizeAmmoItemId(ammoItemId));
        }
    }
}
