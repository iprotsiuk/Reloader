using System;
using System.Collections.Generic;

namespace Reloader.Core.Persistence
{
    public sealed class WorldCleanupService
    {
        public int CleanupDailyResetForDayChange(
            int previousDay,
            int currentDay,
            WorldObjectStateStore stateStore,
            WorldScenePolicyRegistry policyRegistry,
            ReclaimStorageService reclaimStorage)
        {
            if (stateStore == null)
            {
                throw new ArgumentNullException(nameof(stateStore));
            }

            if (policyRegistry == null)
            {
                throw new ArgumentNullException(nameof(policyRegistry));
            }

            if (reclaimStorage == null)
            {
                throw new ArgumentNullException(nameof(reclaimStorage));
            }

            if (previousDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(previousDay), "previousDay cannot be negative.");
            }

            if (currentDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentDay), "currentDay cannot be negative.");
            }

            if (currentDay <= previousDay)
            {
                return 0;
            }

            var snapshot = stateStore.Snapshot();
            var keysToRemove = new List<(string scenePath, string objectId)>();

            for (var i = 0; i < snapshot.Count; i++)
            {
                var record = snapshot[i].Value;
                var key = snapshot[i].Key;
                if (record == null)
                {
                    continue;
                }

                var policy = policyRegistry.ResolvePolicy(key.scenePath);
                if (policy.Mode != WorldObjectPersistenceMode.DailyReset)
                {
                    continue;
                }

                if (record.LastUpdatedDay >= currentDay)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(record.ItemInstanceId))
                {
                    reclaimStorage.AddFromRecord(key.scenePath, record, currentDay);
                }

                keysToRemove.Add((key.scenePath, key.objectId));
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                var key = keysToRemove[i];
                stateStore.Remove(key.scenePath, key.objectId);
            }

            return keysToRemove.Count;
        }
    }
}
