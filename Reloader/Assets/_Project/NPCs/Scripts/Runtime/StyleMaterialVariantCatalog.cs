using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    [DisallowMultipleComponent]
    public sealed class StyleMaterialVariantCatalog : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string ChildName;
            public Material[] Materials;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private Dictionary<string, Material[]> _lookup;

        public IReadOnlyList<Material> GetVariants(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
            {
                return Array.Empty<Material>();
            }

            EnsureLookup();
            return _lookup.TryGetValue(childName.Trim(), out var materials)
                ? materials
                : Array.Empty<Material>();
        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
            _entries = entries?
                .Where(entry => !string.IsNullOrWhiteSpace(entry.ChildName))
                .Select(entry => new Entry
                {
                    ChildName = entry.ChildName.Trim(),
                    Materials = entry.Materials?
                        .Where(material => material != null)
                        .Distinct()
                        .ToArray() ?? Array.Empty<Material>()
                })
                .Where(entry => entry.Materials.Length > 0)
                .ToArray() ?? Array.Empty<Entry>();

            _lookup = null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, Material[]>(StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrWhiteSpace(entry.ChildName))
                {
                    continue;
                }

                var materials = entry.Materials?
                    .Where(material => material != null)
                    .Distinct()
                    .ToArray();
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                _lookup[entry.ChildName.Trim()] = materials;
            }
        }
    }
}
