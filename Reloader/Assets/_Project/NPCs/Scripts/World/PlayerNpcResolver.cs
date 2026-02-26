using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class PlayerNpcResolver : MonoBehaviour, IPlayerNpcResolver
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _maxDistance = 4f;
        [SerializeField] private LayerMask _npcMask = ~0;
        [SerializeField] private bool _excludeShopVendors = true;

        public bool TryResolveNpcAgent(out NpcAgent target)
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
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _npcMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            target = hit.collider.GetComponentInParent<NpcAgent>();
            if (_excludeShopVendors && target != null && target.GetComponent<ShopVendorTarget>() != null)
            {
                target = null;
                return false;
            }

            return target != null;
        }

        public void SetCameraForTests(Camera camera)
        {
            _playerCamera = camera;
        }

        public void SetExcludeShopVendorsForTests(bool excludeShopVendors)
        {
            _excludeShopVendors = excludeShopVendors;
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
