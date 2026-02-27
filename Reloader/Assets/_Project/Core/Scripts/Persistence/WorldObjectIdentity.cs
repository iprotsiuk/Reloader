using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Core.Persistence
{
    [DisallowMultipleComponent]
    public sealed class WorldObjectIdentity : MonoBehaviour
    {
        private static readonly Dictionary<int, string> ReservedIdsByInstanceId = new Dictionary<int, string>();
        private static readonly HashSet<string> ReservedIds = new HashSet<string>();

        [SerializeField] private string _objectId = string.Empty;

        public string ObjectId
        {
            get
            {
                EnsureRuntimeUniqueObjectId();
                return _objectId;
            }
        }

        private void Awake()
        {
            EnsureRuntimeUniqueObjectId();
        }

        private void Reset()
        {
            EnsureRuntimeUniqueObjectId();
        }

        private void OnValidate()
        {
            EnsureRuntimeUniqueObjectId();
        }

        private void OnDestroy()
        {
            ReleaseReservedObjectId();
        }

        private void EnsureRuntimeUniqueObjectId()
        {
            if (!string.IsNullOrWhiteSpace(_objectId))
            {
                ReserveOrRegenerateForConflicts();
                return;
            }

            _objectId = Guid.NewGuid().ToString("N");
            ReserveOrRegenerateForConflicts();
        }

        private void ReserveOrRegenerateForConflicts()
        {
            var instanceId = GetInstanceID();
            if (ReservedIdsByInstanceId.TryGetValue(instanceId, out var reservedId))
            {
                if (string.Equals(reservedId, _objectId, StringComparison.Ordinal))
                {
                    return;
                }

                ReservedIds.Remove(reservedId);
                ReservedIdsByInstanceId.Remove(instanceId);
            }

            while (ReservedIds.Contains(_objectId))
            {
                _objectId = Guid.NewGuid().ToString("N");
            }

            ReservedIds.Add(_objectId);
            ReservedIdsByInstanceId[instanceId] = _objectId;
        }

        private void ReleaseReservedObjectId()
        {
            var instanceId = GetInstanceID();
            if (!ReservedIdsByInstanceId.TryGetValue(instanceId, out var reservedId))
            {
                return;
            }

            ReservedIdsByInstanceId.Remove(instanceId);
            ReservedIds.Remove(reservedId);
        }
    }
}
