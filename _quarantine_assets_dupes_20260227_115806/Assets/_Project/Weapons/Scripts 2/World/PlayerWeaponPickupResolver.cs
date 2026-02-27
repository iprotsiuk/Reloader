using Reloader.Inventory;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class PlayerWeaponPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _maxDistance = 4f;
        [SerializeField] private LayerMask _pickupMask = ~0;

        public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
        {
            target = null;
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }

            if (_playerCamera == null)
            {
                return false;
            }

            // Ensure transforms created/moved this frame are visible to physics queries.
            Physics.SyncTransforms();
            var ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _pickupMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            target = hit.collider.GetComponentInParent<IInventoryPickupTarget>();
            return target != null;
        }

        public void SetCameraForTests(Camera camera)
        {
            _playerCamera = camera;
        }
    }
}
