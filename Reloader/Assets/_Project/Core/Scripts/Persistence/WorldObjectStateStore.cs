using System;
using System.Collections.Generic;

namespace Reloader.Core.Persistence
{
    public sealed class WorldObjectStateStore
    {
        private static readonly StringComparer KeyComparer = StringComparer.Ordinal;
        private readonly Dictionary<string, WorldObjectStateRecord> _records = new Dictionary<string, WorldObjectStateRecord>(KeyComparer);

        public int Count => _records.Count;

        public void Upsert(string scenePath, WorldObjectStateRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var key = BuildKey(scenePath, record.ObjectId);
            _records[key] = record.Copy();
        }

        public bool TryGet(string scenePath, string objectId, out WorldObjectStateRecord record)
        {
            var key = BuildKey(scenePath, objectId);
            if (_records.TryGetValue(key, out var stored))
            {
                record = stored.Copy();
                return true;
            }

            record = null;
            return false;
        }

        private static string BuildKey(string scenePath, string objectId)
        {
            var normalizedScenePath = NormalizeRequired(scenePath, nameof(scenePath));
            var normalizedObjectId = NormalizeRequired(objectId, nameof(objectId));
            return normalizedScenePath + "\u001f" + normalizedObjectId;
        }

        private static string NormalizeRequired(string value, string paramName)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
            }

            return normalized;
        }
    }
}
