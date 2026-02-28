using System;
using UnityEngine;

namespace Reloader.Inventory
{
    public sealed class PlayerStorageContainerResolver : MonoBehaviour, IPlayerStorageContainerResolver
    {
        private const int MaxSphereHits = 16;

        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _maxDistance = 4f;
        [SerializeField] private float _interactionRadius = 0.22f;
        [SerializeField] private LayerMask _containerMask = ~0;

        private readonly RaycastHit[] _sphereHits = new RaycastHit[MaxSphereHits];

        public bool TryResolveStorageContainer(out WorldStorageContainer container)
        {
            container = null;
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
            var hitCount = Physics.SphereCastNonAlloc(
                ray,
                Mathf.Max(0.01f, _interactionRadius),
                _sphereHits,
                _maxDistance,
                _containerMask,
                QueryTriggerInteraction.Collide);

            if (hitCount <= 0)
            {
                return false;
            }

            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < hitCount && i < _sphereHits.Length; i++)
            {
                var candidate = _sphereHits[i].collider != null
                    ? _sphereHits[i].collider.GetComponentInParent<WorldStorageContainer>()
                    : null;
                if (candidate == null)
                {
                    continue;
                }

                if (_sphereHits[i].distance < bestDistance)
                {
                    bestDistance = _sphereHits[i].distance;
                    container = candidate;
                }
            }

            return container != null;
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
