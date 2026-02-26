using UnityEngine;
using System;

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
            var hits = Physics.RaycastAll(ray, _maxDistance, _vendorMask, QueryTriggerInteraction.Collide);
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

                target = collider.GetComponentInParent<IShopVendorTarget>();
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

        private static Camera ResolveFallbackCamera()
        {
            var taggedMain = Camera.main;
            if (taggedMain != null)
            {
                return taggedMain;
            }

            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
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
