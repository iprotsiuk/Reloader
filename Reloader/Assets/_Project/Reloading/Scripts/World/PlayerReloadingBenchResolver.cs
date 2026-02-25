using UnityEngine;

namespace Reloader.Reloading.World
{
    public sealed class PlayerReloadingBenchResolver : MonoBehaviour, IPlayerReloadingBenchResolver
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _maxDistance = 4f;
        [SerializeField] private LayerMask _benchMask = ~0;

        public bool TryResolveBenchTarget(out IReloadingBenchTarget target)
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
            if (!Physics.Raycast(ray, out var hit, _maxDistance, _benchMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            target = hit.collider.GetComponentInParent<IReloadingBenchTarget>();
            return target != null;
        }

        public void SetCameraForTests(Camera camera)
        {
            _playerCamera = camera;
        }
    }
}
