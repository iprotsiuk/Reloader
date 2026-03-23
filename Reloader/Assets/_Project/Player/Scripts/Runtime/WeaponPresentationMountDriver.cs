using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    [DefaultExecutionOrder(12010)]
    [DisallowMultipleComponent]
    public sealed class WeaponPresentationMountDriver : MonoBehaviour
    {
        private const string AnimatedRightHandAnchorName = "ik_hand_gun";

        [SerializeField] private Transform _weaponPresentationRoot;
        [SerializeField] private Transform _weaponPresentationMount;
        [SerializeField] private string _weaponPresentationMountPath;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private Animator _armsAnimator;
        [SerializeField] private Reloader.Game.Weapons.AdsStateController _adsStateController;
        [SerializeField] private ViewmodelAnimationAdapter _viewmodelAnimationAdapter;

        private Transform _resolvedWeaponPresentationMount;
        private Transform _resolvedAnimatedRightHandAnchor;
        private RuntimeAnimatorController _resolvedAnimatorController;
        private Transform _capturedWeaponView;
        private Vector3 _animatedBaselineLocalPosition;
        private Quaternion _animatedBaselineLocalRotation = Quaternion.identity;
        private bool _hasAnimatedBaseline;

        private void Awake()
        {
            ResolveDependencies();
            SyncWeaponPresentationRoot();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            SyncWeaponPresentationRoot();
        }

        private void LateUpdate()
        {
            ResolveDependencies();
            SyncWeaponPresentationRoot();
        }

        public void Configure(Transform weaponPresentationRoot, Transform weaponPresentationMount)
        {
            _weaponPresentationRoot = weaponPresentationRoot;
            _weaponPresentationMount = weaponPresentationMount;
            _weaponPresentationMountPath = string.Empty;
            _resolvedWeaponPresentationMount = null;
            _resolvedAnimatedRightHandAnchor = null;
            ResetAnimatedBaseline();
            SyncWeaponPresentationRoot();
        }

        private void SyncWeaponPresentationRoot()
        {
            var weaponPresentationRoot = ResolveWeaponPresentationRoot();
            if (weaponPresentationRoot == null)
            {
                return;
            }

            var staticMount = ResolveWeaponPresentationMount();
            if (staticMount == null)
            {
                return;
            }

            var parentFrame = weaponPresentationRoot.parent != null
                ? weaponPresentationRoot.parent
                : staticMount.parent;
            if (parentFrame == null)
            {
                weaponPresentationRoot.SetPositionAndRotation(staticMount.position, staticMount.rotation);
                return;
            }

            var animatedRightHandAnchor = ResolveAnimatedRightHandAnchor();
            var liveWeaponView = ResolveLiveWeaponView(weaponPresentationRoot);
            if (liveWeaponView == null || animatedRightHandAnchor == null)
            {
                ResetAnimatedBaseline();
                ApplyLocalPose(weaponPresentationRoot, staticMount.localPosition, staticMount.localRotation);
                return;
            }

            if (!ReferenceEquals(_capturedWeaponView, liveWeaponView) || !_hasAnimatedBaseline)
            {
                CaptureAnimatedBaseline(parentFrame, liveWeaponView, animatedRightHandAnchor);
            }

            var hipPose = ResolveAnimatedDeltaPose(parentFrame, animatedRightHandAnchor, staticMount);
            var adsBlendT = ResolvePresentationBlendT();

            if (adsBlendT <= 0f)
            {
                ApplyLocalPose(weaponPresentationRoot, hipPose.localPosition, hipPose.localRotation);
                return;
            }

            if (adsBlendT >= 1f)
            {
                ApplyLocalPose(weaponPresentationRoot, staticMount.localPosition, staticMount.localRotation);
                return;
            }

            weaponPresentationRoot.localPosition = Vector3.Lerp(hipPose.localPosition, staticMount.localPosition, adsBlendT);
            weaponPresentationRoot.localRotation = Quaternion.Slerp(hipPose.localRotation, staticMount.localRotation, adsBlendT);
        }

        private void ResolveDependencies()
        {
            _cameraDefaults ??= GetComponent<PlayerCameraDefaults>();
            _adsStateController ??= GetComponent<Reloader.Game.Weapons.AdsStateController>();
            _viewmodelAnimationAdapter ??= GetComponent<ViewmodelAnimationAdapter>();

            if (_armsAnimator == null && _cameraDefaults != null && _cameraDefaults.TryGetPlayerArmsAnimator(out var playerArmsAnimator))
            {
                _armsAnimator = playerArmsAnimator;
            }

            if (_armsAnimator == null)
            {
                _resolvedAnimatorController = null;
                ResetAnimatedBaseline();
                return;
            }

            var currentController = _armsAnimator.runtimeAnimatorController;
            if (!ReferenceEquals(_resolvedAnimatorController, currentController))
            {
                _resolvedAnimatorController = currentController;
                ResetAnimatedBaseline();
            }
        }

        private Transform ResolveWeaponPresentationRoot()
        {
            if (_weaponPresentationRoot != null)
            {
                return _weaponPresentationRoot;
            }

            if (_cameraDefaults != null && _cameraDefaults.TryGetWeaponPresentationRoot(out var weaponPresentationRoot))
            {
                _weaponPresentationRoot = weaponPresentationRoot;
            }

            return _weaponPresentationRoot;
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

        private Transform ResolveAnimatedRightHandAnchor()
        {
            if (_armsAnimator != null
                && _resolvedAnimatedRightHandAnchor != null
                && (_resolvedAnimatedRightHandAnchor == _armsAnimator.transform || _resolvedAnimatedRightHandAnchor.IsChildOf(_armsAnimator.transform)))
            {
                return _resolvedAnimatedRightHandAnchor;
            }

            _resolvedAnimatedRightHandAnchor = _armsAnimator != null
                ? FindDescendantByName(_armsAnimator.transform, AnimatedRightHandAnchorName)
                : null;
            return _resolvedAnimatedRightHandAnchor;
        }

        private void CaptureAnimatedBaseline(Transform parentFrame, Transform liveWeaponView, Transform animatedRightHandAnchor)
        {
            _capturedWeaponView = liveWeaponView;
            _animatedBaselineLocalPosition = parentFrame.InverseTransformPoint(animatedRightHandAnchor.position);
            _animatedBaselineLocalRotation = Quaternion.Inverse(parentFrame.rotation) * animatedRightHandAnchor.rotation;
            _hasAnimatedBaseline = true;
        }

        private (Vector3 localPosition, Quaternion localRotation) ResolveAnimatedDeltaPose(
            Transform parentFrame,
            Transform animatedRightHandAnchor,
            Transform staticMount)
        {
            if (!_hasAnimatedBaseline)
            {
                return (staticMount.localPosition, staticMount.localRotation);
            }

            var currentAnimatedLocalPosition = parentFrame.InverseTransformPoint(animatedRightHandAnchor.position);
            var currentAnimatedLocalRotation = Quaternion.Inverse(parentFrame.rotation) * animatedRightHandAnchor.rotation;
            var deltaRotation = currentAnimatedLocalRotation * Quaternion.Inverse(_animatedBaselineLocalRotation);
            var deltaPosition = currentAnimatedLocalPosition - (deltaRotation * _animatedBaselineLocalPosition);
            var localPosition = (deltaRotation * staticMount.localPosition) + deltaPosition;
            var localRotation = deltaRotation * staticMount.localRotation;
            return (localPosition, localRotation);
        }

        private float ResolvePresentationBlendT()
        {
            if (_viewmodelAnimationAdapter != null && _viewmodelAnimationAdapter.IsReloadingDebug)
            {
                return 0f;
            }

            return _adsStateController != null ? Mathf.Clamp01(_adsStateController.AdsT) : 0f;
        }

        private static void ApplyLocalPose(Transform target, Vector3 localPosition, Quaternion localRotation)
        {
            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }

        private Transform ResolveLiveWeaponView(Transform weaponPresentationRoot)
        {
            if (weaponPresentationRoot == null || weaponPresentationRoot.childCount <= 0)
            {
                return null;
            }

            return weaponPresentationRoot.GetChild(0);
        }

        private void ResetAnimatedBaseline()
        {
            _capturedWeaponView = null;
            _hasAnimatedBaseline = false;
            _animatedBaselineLocalPosition = Vector3.zero;
            _animatedBaselineLocalRotation = Quaternion.identity;
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var match = FindDescendantByName(child, targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
