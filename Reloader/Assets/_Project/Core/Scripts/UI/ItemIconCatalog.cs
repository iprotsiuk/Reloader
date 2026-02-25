using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Core.UI
{
    [CreateAssetMenu(fileName = "ItemIconCatalog", menuName = "Reloader/UI/Item Icon Catalog")]
    public sealed class ItemIconCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private string _itemId;
            [SerializeField] private Sprite _icon;

            public Entry(string itemId, Sprite icon)
            {
                _itemId = itemId;
                _icon = icon;
            }

            public string ItemId => _itemId;
            public Sprite Icon => _icon;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<string, Sprite> _lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryGetIcon(string itemId, out Sprite icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            RebuildLookup();
            return _lookup.TryGetValue(itemId, out icon) && icon != null;
        }

        public void SetEntriesForTests(IEnumerable<Entry> entries)
        {
            _entries = entries != null ? new List<Entry>(entries) : new List<Entry>();
            RebuildLookup();
        }

#if UNITY_EDITOR
        public void ReplaceEntriesForEditor(List<Entry> entries)
        {
            _entries = entries ?? new List<Entry>();
            RebuildLookup();
        }
#endif

        private void OnValidate()
        {
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _lookup.Clear();
            if (_entries == null)
            {
                return;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (string.IsNullOrWhiteSpace(entry.ItemId) || entry.Icon == null)
                {
                    continue;
                }

                _lookup[entry.ItemId] = entry.Icon;
            }
        }
    }
}
