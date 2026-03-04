using System;
using UnityEngine;

namespace Reloader.Core.Persistence
{
    [DisallowMultipleComponent]
    public sealed class WorldObjectIdentity : MonoBehaviour
    {
        [SerializeField] private string _objectId = string.Empty;

        public string ObjectId
        {
            get
            {
                EnsureObjectId();
                return _objectId;
            }
        }

        public bool AssignObjectIdForRestore(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                return false;
            }

            _objectId = objectId.Trim();
            return true;
        }

        private void Awake()
        {
            EnsureObjectId();
        }

        private void Reset()
        {
            EnsureObjectId();
        }

        private void OnValidate()
        {
            EnsureObjectId();
        }

        private void EnsureObjectId()
        {
            if (!string.IsNullOrWhiteSpace(_objectId))
            {
                return;
            }

            _objectId = Guid.NewGuid().ToString("N");
        }
    }
}
