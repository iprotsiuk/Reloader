using UnityEngine;

namespace Reloader.Audio
{
    public static class CombatAudioCatalogResolver
    {
        private const string DefaultCatalogResourcePath = "CombatAudioCatalog";
        private static CombatAudioCatalog _cachedCatalog;

        public static CombatAudioCatalog Resolve(CombatAudioCatalog current)
        {
            if (current != null)
            {
                return current;
            }

            if (_cachedCatalog != null)
            {
                return _cachedCatalog;
            }

            _cachedCatalog = Resources.Load<CombatAudioCatalog>(DefaultCatalogResourcePath);
            return _cachedCatalog;
        }
    }
}
