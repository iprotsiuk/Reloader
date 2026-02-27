using Reloader.Inventory;
using System;
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
            var hits = Physics.RaycastAll(ray, _maxDistance, _pickupMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var collider = hits[i].collider;
                if (collider == null)
                {
                    continue;
                }

                target = collider.GetComponentInParent<IInventoryPickupTarget>();
                if (target != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetCameraForTests(Camera camera)
        {
            _playerCamera = camera;
        }
    }
}
