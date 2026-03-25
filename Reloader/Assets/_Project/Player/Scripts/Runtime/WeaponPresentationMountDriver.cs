using System;
using System.Reflection;
using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    [DefaultExecutionOrder(12010)]
    [DisallowMultipleComponent]
    public sealed class WeaponPresentationMountDriver : MonoBehaviour
    {
        private const string AnimatedWeaponMountName = "ik_hand_gun";
        private const string AnimatedRightHandAnchorName = "ik_hand_r";

        [SerializeField] private Transform _weaponPresentationRoot;
        [SerializeField] private Transform _weaponPresentationMount;
        [SerializeField] private string _weaponPresentationMountPath;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private Animator _armsAnimator;
        [SerializeField] private Reloader.Game.Weapons.AdsStateController _adsStateController;
        [SerializeField] private ViewmodelAnimationAdapter _viewmodelAnimationAdapter;

        private Transform _resolvedWeaponPresentationMount;
        private Transform _resolvedAnimatedWeaponMount;
        private Transform _resolvedAnimatedRightHandAnchor;
        private RuntimeAnimatorController _resolvedAnimatorController;
        private Transform _capturedWeaponView;
        private Vector3 _animatedBaselineLocalPosition;
        private Quaternion _animatedBaselineLocalRotation = Quaternion.identity;
        private bool _hasAnimatedBaseline;
        private static Type s_weaponViewHandAnchorsType;
        private static MethodInfo s_tryGetHandTargetsMethod;

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
            _resolvedAnimatedWeaponMount = null;
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

            var liveWeaponView = ResolveLiveWeaponView(weaponPresentationRoot);
            if (TryResolveHandGripDrivenHipPose(weaponPresentationRoot, parentFrame, liveWeaponView, out var gripDrivenHipPose))
            {
                var gripDrivenAdsBlendT = ResolvePresentationBlendT();

                if (gripDrivenAdsBlendT <= 0f)
                {
                    ApplyLocalPose(weaponPresentationRoot, gripDrivenHipPose.localPosition, gripDrivenHipPose.localRotation);
                    return;
                }

                if (gripDrivenAdsBlendT >= 1f)
                {
                    ApplyLocalPose(weaponPresentationRoot, staticMount.localPosition, staticMount.localRotation);
                    return;
                }

                weaponPresentationRoot.localPosition = Vector3.Lerp(gripDrivenHipPose.localPosition, staticMount.localPosition, gripDrivenAdsBlendT);
                weaponPresentationRoot.localRotation = Quaternion.Slerp(gripDrivenHipPose.localRotation, staticMount.localRotation, gripDrivenAdsBlendT);
                return;
            }

            var animatedWeaponMount = ResolveAnimatedWeaponMount();
            if (liveWeaponView == null || animatedWeaponMount == null)
            {
                ResetAnimatedBaseline();
                ApplyLocalPose(weaponPresentationRoot, staticMount.localPosition, staticMount.localRotation);
                return;
            }

            if (!ReferenceEquals(_capturedWeaponView, liveWeaponView) || !_hasAnimatedBaseline)
            {
                CaptureAnimatedBaseline(parentFrame, liveWeaponView, animatedWeaponMount);
            }

            var hipPose = ResolveAnimatedDeltaPose(parentFrame, animatedWeaponMount, staticMount);
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
                _resolvedAnimatedWeaponMount = null;
                _resolvedAnimatedRightHandAnchor = null;
                ResetAnimatedBaseline();
                return;
            }

            var currentController = _armsAnimator.runtimeAnimatorController;
            if (!ReferenceEquals(_resolvedAnimatorController, currentController))
            {
                _resolvedAnimatorController = currentController;
                _resolvedAnimatedWeaponMount = null;
                _resolvedAnimatedRightHandAnchor = null;
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

        private Transform ResolveAnimatedWeaponMount()
        {
            if (_armsAnimator != null
                && _resolvedAnimatedWeaponMount != null
                && (_resolvedAnimatedWeaponMount == _armsAnimator.transform || _resolvedAnimatedWeaponMount.IsChildOf(_armsAnimator.transform)))
            {
                return _resolvedAnimatedWeaponMount;
            }

            _resolvedAnimatedWeaponMount = _armsAnimator != null
                ? FindDescendantByName(_armsAnimator.transform, AnimatedWeaponMountName)
                : null;
            return _resolvedAnimatedWeaponMount;
        }

        private bool TryResolveHandGripDrivenHipPose(
            Transform weaponPresentationRoot,
            Transform parentFrame,
            Transform liveWeaponView,
            out (Vector3 localPosition, Quaternion localRotation) gripDrivenHipPose)
        {
            gripDrivenHipPose = default;
            if (weaponPresentationRoot == null || parentFrame == null || liveWeaponView == null)
            {
                return false;
            }

            if (!TryGetWeaponViewHandTargets(liveWeaponView, out _, out var rightHandGrip))
            {
                return false;
            }

            var animatedWeaponMount = ResolveAnimatedWeaponMount();
            if (animatedWeaponMount == null)
            {
                return false;
            }

            var animatedRightHandAnchor = ResolveAnimatedHandAnchor(animatedWeaponMount, AnimatedRightHandAnchorName, ref _resolvedAnimatedRightHandAnchor);
            if (animatedRightHandAnchor == null)
            {
                return false;
            }

            var correctionRotation = animatedRightHandAnchor.rotation * Quaternion.Inverse(rightHandGrip.rotation);
            var correctionPosition = animatedRightHandAnchor.position - (correctionRotation * rightHandGrip.position);
            var rootWorldPosition = (correctionRotation * weaponPresentationRoot.position) + correctionPosition;
            var rootWorldRotation = correctionRotation * weaponPresentationRoot.rotation;
            gripDrivenHipPose = (
                parentFrame.InverseTransformPoint(rootWorldPosition),
                Quaternion.Inverse(parentFrame.rotation) * rootWorldRotation);
            return true;
        }

        private static Transform ResolveAnimatedHandAnchor(Transform animatedWeaponMount, string targetName, ref Transform cachedAnchor)
        {
            if (animatedWeaponMount != null
                && cachedAnchor != null
                && (cachedAnchor == animatedWeaponMount || cachedAnchor.IsChildOf(animatedWeaponMount)))
            {
                return cachedAnchor;
            }

            cachedAnchor = animatedWeaponMount != null
                ? FindDescendantByName(animatedWeaponMount, targetName)
                : null;
            return cachedAnchor;
        }

        private static bool TryGetWeaponViewHandTargets(Transform liveWeaponView, out Transform leftHandGrip, out Transform rightHandGrip)
        {
            leftHandGrip = null;
            rightHandGrip = null;
            if (liveWeaponView == null)
            {
                return false;
            }

            var handAnchorsType = ResolveWeaponViewHandAnchorsType();
            if (handAnchorsType == null)
            {
                return false;
            }

            var handAnchors = liveWeaponView.GetComponent(handAnchorsType);
            if (handAnchors == null)
            {
                return false;
            }

            s_tryGetHandTargetsMethod ??= handAnchorsType.GetMethod("TryGetHandTargets", BindingFlags.Instance | BindingFlags.Public);
            if (s_tryGetHandTargetsMethod == null)
            {
                return false;
            }

            var args = new object[] { null, null };
            if (s_tryGetHandTargetsMethod.Invoke(handAnchors, args) is not bool hasHandTargets || !hasHandTargets)
            {
                return false;
            }

            leftHandGrip = args[0] as Transform;
            rightHandGrip = args[1] as Transform;
            return leftHandGrip != null && rightHandGrip != null;
        }

        private static Type ResolveWeaponViewHandAnchorsType()
        {
            if (s_weaponViewHandAnchorsType != null)
            {
                return s_weaponViewHandAnchorsType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var candidate = assemblies[i].GetType("Reloader.Weapons.Runtime.WeaponViewHandAnchors");
                if (candidate == null)
                {
                    continue;
                }

                s_weaponViewHandAnchorsType = candidate;
                return s_weaponViewHandAnchorsType;
            }

            return null;
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
