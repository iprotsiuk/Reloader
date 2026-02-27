using System;
using System.Collections.Generic;

namespace Reloader.Core.Persistence
{
    public sealed class WorldObjectStateStore
    {
        private readonly Dictionary<(string scenePath, string objectId), WorldObjectStateRecord> _records =
            new Dictionary<(string scenePath, string objectId), WorldObjectStateRecord>();

        public int Count => _records.Count;

        public void Upsert(string scenePath, WorldObjectStateRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRequired(scenePath, nameof(scenePath));
            EnsureRequired(record.ObjectId, nameof(record.ObjectId));
            var key = BuildKey(scenePath, record.ObjectId);
            _records[key] = record;
        }

        public bool TryGet(string scenePath, string objectId, out WorldObjectStateRecord record)
        {
            EnsureRequired(scenePath, nameof(scenePath));
            EnsureRequired(objectId, nameof(objectId));
            var key = BuildKey(scenePath, objectId);
            return _records.TryGetValue(key, out record);
        }

        private static (string scenePath, string objectId) BuildKey(string scenePath, string objectId)
        {
            return (scenePath, objectId);
        }

        private static void EnsureRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
            }
        }
    }
}
