using System;
using System.Collections.Generic;

namespace Reloader.Core.Persistence
{
    [Serializable]
    public sealed class ReclaimStorageEntry
    {
        public string ScenePath { get; set; } = string.Empty;
        public string ObjectId { get; set; } = string.Empty;
        public string ItemInstanceId { get; set; } = string.Empty;
        public int CleanedOnDay { get; set; }
        public bool Consumed { get; set; }
        public bool Destroyed { get; set; }
    }

    public sealed class ReclaimStorageService
    {
        private readonly Dictionary<string, ReclaimStorageEntry> _entriesByItemInstanceId =
            new Dictionary<string, ReclaimStorageEntry>(StringComparer.Ordinal);

        public int Count => _entriesByItemInstanceId.Count;

        public void AddFromRecord(string scenePath, WorldObjectStateRecord record, int cleanedOnDay)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRequired(scenePath, nameof(scenePath));
            EnsureRequired(record.ObjectId, nameof(record.ObjectId));
            EnsureRequired(record.ItemInstanceId, nameof(record.ItemInstanceId));
            EnsureNonNegative(cleanedOnDay, nameof(cleanedOnDay));

            _entriesByItemInstanceId[record.ItemInstanceId] = new ReclaimStorageEntry
            {
                ScenePath = scenePath,
                ObjectId = record.ObjectId,
                ItemInstanceId = record.ItemInstanceId,
                CleanedOnDay = cleanedOnDay,
                Consumed = record.Consumed,
                Destroyed = record.Destroyed
            };
        }

        public bool TryGetEntry(string itemInstanceId, out ReclaimStorageEntry entry)
        {
            EnsureRequired(itemInstanceId, nameof(itemInstanceId));
            return _entriesByItemInstanceId.TryGetValue(itemInstanceId, out entry);
        }

        public List<ReclaimStorageEntry> Snapshot()
        {
            var entries = new List<ReclaimStorageEntry>(_entriesByItemInstanceId.Count);
            foreach (var kvp in _entriesByItemInstanceId)
            {
                var source = kvp.Value;
                entries.Add(new ReclaimStorageEntry
                {
                    ScenePath = source.ScenePath,
                    ObjectId = source.ObjectId,
                    ItemInstanceId = source.ItemInstanceId,
                    CleanedOnDay = source.CleanedOnDay,
                    Consumed = source.Consumed,
                    Destroyed = source.Destroyed
                });
            }

            return entries;
        }

        public void Restore(IEnumerable<ReclaimStorageEntry> entries)
        {
            _entriesByItemInstanceId.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.ScenePath)
                    || string.IsNullOrWhiteSpace(entry.ObjectId)
                    || string.IsNullOrWhiteSpace(entry.ItemInstanceId)
                    || entry.CleanedOnDay < 0)
                {
                    continue;
                }

                _entriesByItemInstanceId[entry.ItemInstanceId] = new ReclaimStorageEntry
                {
                    ScenePath = entry.ScenePath,
                    ObjectId = entry.ObjectId,
                    ItemInstanceId = entry.ItemInstanceId,
                    CleanedOnDay = entry.CleanedOnDay,
                    Consumed = entry.Consumed,
                    Destroyed = entry.Destroyed
                };
            }
        }

        private static void EnsureRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
            }
        }

        private static void EnsureNonNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
            }
        }
    }
}
