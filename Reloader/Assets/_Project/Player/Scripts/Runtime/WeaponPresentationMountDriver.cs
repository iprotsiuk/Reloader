using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    [DefaultExecutionOrder(12010)]
    [DisallowMultipleComponent]
    public sealed class WeaponPresentationMountDriver : MonoBehaviour
    {
        [SerializeField] private Transform _weaponPresentationRoot;
        [SerializeField] private Transform _weaponPresentationMount;
        [SerializeField] private string _weaponPresentationMountPath;

        private Transform _resolvedWeaponPresentationMount;

        private void Awake()
        {
            SyncWeaponPresentationRoot();
        }

        private void OnEnable()
        {
            SyncWeaponPresentationRoot();
        }

        private void LateUpdate()
        {
            SyncWeaponPresentationRoot();
        }

        public void Configure(Transform weaponPresentationRoot, Transform weaponPresentationMount)
        {
            _weaponPresentationRoot = weaponPresentationRoot;
            _weaponPresentationMount = weaponPresentationMount;
            _weaponPresentationMountPath = string.Empty;
            _resolvedWeaponPresentationMount = null;
            SyncWeaponPresentationRoot();
        }

        private void SyncWeaponPresentationRoot()
        {
            var weaponPresentationMount = ResolveWeaponPresentationMount();
            if (_weaponPresentationRoot == null || weaponPresentationMount == null)
            {
                return;
            }

            _weaponPresentationRoot.SetPositionAndRotation(weaponPresentationMount.position, weaponPresentationMount.rotation);
        }

        private Transform ResolveWeaponPresentationMount()
        {
            if (_weaponPresentationMount != null)
            {
                return _weaponPresentationMount;
            }

            if (_resolvedWeaponPresentationMount != null
                && (_resolvedWeaponPresentationMount == transform || _resolvedWeaponPresentationMount.IsChildOf(transform)))
            {
                return _resolvedWeaponPresentationMount;
            }

            if (string.IsNullOrWhiteSpace(_weaponPresentationMountPath))
            {
                return null;
            }

            _resolvedWeaponPresentationMount = transform.Find(_weaponPresentationMountPath);
            return _resolvedWeaponPresentationMount;
        }
    }
}
