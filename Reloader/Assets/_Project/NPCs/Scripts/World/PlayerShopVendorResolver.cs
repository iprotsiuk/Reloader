using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class PlayerShopVendorResolver : MonoBehaviour, IPlayerShopVendorResolver
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _maxDistance = 4f;
        [SerializeField] private LayerMask _vendorMask = ~0;

        public bool TryResolveVendorTarget(out IShopVendorTarget target)
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

            Physics.SyncTransforms();
            var ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _vendorMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            target = hit.collider.GetComponentInParent<IShopVendorTarget>();
            return target != null;
        }

        public void SetCameraForTests(Camera camera)
        {
            _playerCamera = camera;
        }
    }
}
