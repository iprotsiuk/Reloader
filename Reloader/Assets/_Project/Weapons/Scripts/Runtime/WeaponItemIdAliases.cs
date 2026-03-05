using System;

namespace Reloader.Weapons.Runtime
{
    public static class WeaponItemIdAliases
    {
        public const string Kar98k = "weapon-kar98k";
        public const string LegacyStarterRifle = "weapon-rifle-01";

        public static string Normalize(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return string.Empty;
            }

            return string.Equals(itemId, LegacyStarterRifle, StringComparison.Ordinal)
                ? Kar98k
                : itemId;
        }
    }
}
