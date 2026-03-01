using System;
using Reloader.Core.Persistence;
using UnityEngine;

namespace Reloader.Inventory
{
    [RequireComponent(typeof(WorldObjectIdentity))]
    [DisallowMultipleComponent]
    public sealed class RuntimeDroppedObjectPersistenceTracker : MonoBehaviour
    {
        [SerializeField] private string _itemDefinitionId;
        [SerializeField] private int _stackQuantity = 1;
        [SerializeField] private string _runtimeDropInstanceId;
        [SerializeField] private int _persistIntervalFrames = 2;

        private WorldObjectIdentity _identity;
        private Vector3 _lastPersistedPosition;
        private Quaternion _lastPersistedRotation = Quaternion.identity;
        private int _lastPersistedFrame;
        private bool _hasPersistedAtLeastOnce;

        public void Configure(string itemDefinitionId, int stackQuantity, string runtimeDropInstanceId = null)
        {
            _itemDefinitionId = itemDefinitionId ?? string.Empty;
            _stackQuantity = Mathf.Max(1, stackQuantity);
            _runtimeDropInstanceId = !string.IsNullOrWhiteSpace(runtimeDropInstanceId)
                ? runtimeDropInstanceId
                : Guid.NewGuid().ToString("N");

            EnsureReferences();
            PersistNow();
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void LateUpdate()
        {
            if (string.IsNullOrWhiteSpace(_itemDefinitionId))
            {
                return;
            }

            EnsureReferences();
            if (_identity == null)
            {
                return;
            }

            if (Time.frameCount - _lastPersistedFrame >= Mathf.Max(1, _persistIntervalFrames))
            {
                PersistIfTransformChanged();
            }
        }

        private void OnDisable()
        {
            if (!isActiveAndEnabled)
            {
                PersistIfTransformChanged(force: true);
            }
        }

        private void EnsureReferences()
        {
            if (_identity == null)
            {
                _identity = GetComponent<WorldObjectIdentity>();
            }
        }

        private void PersistIfTransformChanged(bool force = false)
        {
            var position = transform.position;
            var rotation = transform.rotation;

            var positionChanged = Vector3.Distance(position, _lastPersistedPosition) > 0.01f;
            var rotationChanged = Quaternion.Angle(rotation, _lastPersistedRotation) > 0.5f;
            if (!force && _hasPersistedAtLeastOnce && !positionChanged && !rotationChanged)
            {
                return;
            }

            PersistNow();
        }

        private void PersistNow()
        {
            if (_identity == null || string.IsNullOrWhiteSpace(_itemDefinitionId))
            {
                return;
            }

            var scene = gameObject.scene;
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                return;
            }

            WorldObjectPersistenceRuntimeBridge.MarkRuntimeSpawned(
                scene.path,
                _identity.ObjectId,
                _itemDefinitionId,
                _stackQuantity,
                transform.position,
                transform.rotation,
                _runtimeDropInstanceId);

            _lastPersistedPosition = transform.position;
            _lastPersistedRotation = transform.rotation;
            _lastPersistedFrame = Time.frameCount;
            _hasPersistedAtLeastOnce = true;
        }
    }
}
