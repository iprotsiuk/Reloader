using System;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [CreateAssetMenu(fileName = "BloodVfxCatalog", menuName = "Reloader/NPCs/Blood VFX Catalog")]
    public sealed class BloodVfxCatalog : ScriptableObject
    {
        [Serializable]
        private struct BloodEffectEntry
        {
            public BloodEffectKind Kind;
            public GameObject Prefab;
        }

        [SerializeField] private BloodEffectEntry[] _effectEntries = Array.Empty<BloodEffectEntry>();

        public bool TryGetPrefab(BloodEffectKind kind, out GameObject prefab)
        {
            for (var i = 0; i < _effectEntries.Length; i++)
            {
                if (_effectEntries[i].Kind == kind)
                {
                    prefab = _effectEntries[i].Prefab;
                    return prefab != null;
                }
            }

            prefab = null;
            return false;
        }
    }
}
