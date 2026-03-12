using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.DevTools.Data
{
    [CreateAssetMenu(fileName = "DevNpcSpawnCatalog", menuName = "Reloader/DevTools/NPC Spawn Catalog")]
    public sealed class DevNpcSpawnCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _spawnId = string.Empty;
            [SerializeField] private string _displayName = string.Empty;
            [SerializeField] private GameObject _prefab;

            public Entry(string spawnId, string displayName, GameObject prefab)
            {
                _spawnId = spawnId ?? string.Empty;
                _displayName = displayName ?? string.Empty;
                _prefab = prefab;
            }

            public string SpawnId => _spawnId ?? string.Empty;
            public string DisplayName => _displayName ?? string.Empty;
            public GameObject Prefab => _prefab;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => _entries ?? Array.Empty<Entry>();

        public bool TryResolve(string spawnId, out Entry entry)
        {
            var normalizedSpawnId = Normalize(spawnId);
            var entries = _entries ?? Array.Empty<Entry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var candidate = entries[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(Normalize(candidate.SpawnId), normalizedSpawnId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public IReadOnlyList<Entry> GetSuggestions(string prefix)
        {
            var normalizedPrefix = Normalize(prefix);
            var matches = new List<Entry>();
            var entries = _entries ?? Array.Empty<Entry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var candidate = entries[i];
                if (candidate == null || candidate.Prefab == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(normalizedPrefix)
                    && !Normalize(candidate.SpawnId).StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(candidate);
            }

            return matches;
        }

        public void SetEntriesForTests(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
