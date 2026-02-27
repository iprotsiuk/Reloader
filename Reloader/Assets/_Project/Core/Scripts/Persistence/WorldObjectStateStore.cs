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
            _records[key] = record;
        }

        public bool TryGet(string scenePath, string objectId, out WorldObjectStateRecord record)
        {
            var key = BuildKey(scenePath, objectId);
            return _records.TryGetValue(key, out record);
        }

        private static string BuildKey(string scenePath, string objectId)
        {
            return (scenePath ?? string.Empty) + "\u001f" + (objectId ?? string.Empty);
        }
    }
}
