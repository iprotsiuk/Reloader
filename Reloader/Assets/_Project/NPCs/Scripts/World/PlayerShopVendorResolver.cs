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
                _playerCamera = ResolveFallbackCamera();
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

        private static Camera ResolveFallbackCamera()
        {
            var taggedMain = Camera.main;
            if (taggedMain != null)
            {
                return taggedMain;
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
                {
                    continue;
                }

                return camera;
            }

            return null;
        }
    }
}
