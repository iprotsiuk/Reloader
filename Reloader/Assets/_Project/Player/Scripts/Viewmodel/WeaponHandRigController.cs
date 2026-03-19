using System;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    [DefaultExecutionOrder(12000)]
    [DisallowMultipleComponent]
    public sealed class WeaponHandRigController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private Transform _rightHandTarget;

        [Header("Behavior")]
        [SerializeField] private bool _enabledInPlayMode = true;

        [Header("Debug")]
        [SerializeField] private string _equippedViewName = string.Empty;
        [SerializeField] private bool _hasResolvedWeaponAnchors;

        private Transform _equippedWeaponViewOverride;
        private object _cachedWeaponController;
        private Transform _cachedEquippedWeaponView;
        private WeaponViewHandAnchors _cachedAnchors;
        private Vector3 _leftHandRestLocalPosition;
        private Quaternion _leftHandRestLocalRotation = Quaternion.identity;
        private Vector3 _rightHandRestLocalPosition;
        private Quaternion _rightHandRestLocalRotation = Quaternion.identity;

        public bool HasResolvedWeaponAnchors => _hasResolvedWeaponAnchors;

        private void Awake()
        {
            CacheRestPose();
        }

        private void OnEnable()
        {
            CacheRestPose();
        }

        private void LateUpdate()
        {
            if (!_enabledInPlayMode)
            {
                return;
            }

            SyncHandTargets();
        }

        public void ConfigureTargets(Transform leftHandTarget, Transform rightHandTarget)
        {
            _leftHandTarget = leftHandTarget;
            _rightHandTarget = rightHandTarget;
            CacheRestPose();
        }

        public void SetEquippedWeaponViewForTests(Transform weaponView)
        {
            _equippedWeaponViewOverride = weaponView;
            _cachedEquippedWeaponView = null;
            _cachedAnchors = null;
        }

        public void SyncHandTargets()
        {
            var weaponView = ResolveEquippedWeaponView();
            if (weaponView == null)
            {
                ClearRuntimeState();
                RestoreHandTargets();
                return;
            }

            if (!ReferenceEquals(_cachedEquippedWeaponView, weaponView))
            {
                _cachedEquippedWeaponView = weaponView;
                _cachedAnchors = weaponView.GetComponentInChildren<WeaponViewHandAnchors>(true);
                _equippedViewName = weaponView.name;
            }

            if (_cachedAnchors == null || !_cachedAnchors.TryGetHandTargets(out var leftGrip, out var rightGrip))
            {
                ClearRuntimeState();
                RestoreHandTargets();
                return;
            }

            _hasResolvedWeaponAnchors = true;
            PushTargetPose(_leftHandTarget, leftGrip);
            PushTargetPose(_rightHandTarget, rightGrip);
        }

        private Transform ResolveEquippedWeaponView()
        {
            if (_equippedWeaponViewOverride != null)
            {
                return _equippedWeaponViewOverride;
            }

            if (_cachedWeaponController == null)
            {
                _cachedWeaponController = ResolveWeaponController();
            }

            if (_cachedWeaponController == null)
            {
                return null;
            }

            var controllerType = _cachedWeaponController.GetType();
            var equippedViewProperty = controllerType.GetProperty("EquippedWeaponViewTransform", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (equippedViewProperty == null)
            {
                return null;
            }

            return equippedViewProperty.GetValue(_cachedWeaponController) as Transform;
        }

        private static object ResolveWeaponController()
        {
            var controllerType = ResolveTypeByName("Reloader.Weapons.Controllers.PlayerWeaponController");
            if (controllerType == null)
            {
                return null;
            }

            var controllers = UnityEngine.Object.FindObjectsByType(controllerType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            return controllers != null && controllers.Length > 0 ? controllers[0] : null;
        }

        private static Type ResolveTypeByName(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private void CacheRestPose()
        {
            if (_leftHandTarget != null)
            {
                _leftHandRestLocalPosition = _leftHandTarget.localPosition;
                _leftHandRestLocalRotation = _leftHandTarget.localRotation;
            }

            if (_rightHandTarget != null)
            {
                _rightHandRestLocalPosition = _rightHandTarget.localPosition;
                _rightHandRestLocalRotation = _rightHandTarget.localRotation;
            }
        }

        private void ClearRuntimeState()
        {
            _cachedAnchors = null;
            _cachedEquippedWeaponView = null;
            _equippedViewName = string.Empty;
            _hasResolvedWeaponAnchors = false;
        }

        private void RestoreHandTargets()
        {
            RestoreTargetPose(_leftHandTarget, _leftHandRestLocalPosition, _leftHandRestLocalRotation);
            RestoreTargetPose(_rightHandTarget, _rightHandRestLocalPosition, _rightHandRestLocalRotation);
        }

        private static void RestoreTargetPose(Transform target, Vector3 localPosition, Quaternion localRotation)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }

        private static void PushTargetPose(Transform target, Transform source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.position = source.position;
            target.rotation = source.rotation;
        }
    }
}
