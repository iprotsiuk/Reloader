using Reloader.Player;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Reloader.Player.Viewmodel
{
    [DefaultExecutionOrder(12000)]
    [DisallowMultipleComponent]
    public sealed class WeaponHandRigController : MonoBehaviour
    {
        private const string TargetRootName = "WeaponHandRigTargets";
        private const string LeftTargetName = "LeftHandTarget";
        private const string LeftHintName = "LeftElbowHint";
        private const string RightTargetName = "RightHandTarget";
        private const string RightHintName = "RightElbowHint";
        private const string RigName = "WeaponHandRig";
        private const string LeftConstraintName = "LeftHandConstraint";
        private const string RightConstraintName = "RightHandConstraint";

        [Header("References")]
        [SerializeField] private Animator _armsAnimator;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private RigBuilder _rigBuilder;
        [SerializeField] private Rig _weaponHandRig;
        [SerializeField] private TwoBoneIKConstraint _leftHandConstraint;
        [SerializeField] private TwoBoneIKConstraint _rightHandConstraint;
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private Transform _leftHandHint;
        [SerializeField] private Transform _rightHandTarget;
        [SerializeField] private Transform _rightHandHint;
        [SerializeField] private Transform _handTargetRoot;

        [Header("Behavior")]
        [SerializeField] private bool _enabledInPlayMode = true;
        [SerializeField] private bool _driveLeftHand = true;
        [SerializeField] private bool _driveRightHand;
        [SerializeField] private bool _releaseLeftHandDuringReload = true;
        [SerializeField, Range(0f, 1f)] private float _leftHandActiveWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _leftHandReloadWeight = 0f;
        [SerializeField, Range(0f, 1f)] private float _rightHandActiveWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _rightHandReloadWeight = 1f;
        [SerializeField, Min(0.01f)] private float _hintDistance = 0.18f;

        [Header("Debug")]
        [SerializeField] private string _equippedViewName = string.Empty;
        [SerializeField] private bool _hasResolvedWeaponAnchors;

        private PlayerWeaponController _weaponController;
        private ViewmodelAnimationAdapter _viewmodelAnimationAdapter;
        private Transform _equippedWeaponViewOverride;
        private Transform _cachedEquippedWeaponView;
        private WeaponViewHandAnchors _cachedAnchors;
        private Vector3 _leftHandRestLocalPosition;
        private Quaternion _leftHandRestLocalRotation = Quaternion.identity;
        private Vector3 _rightHandRestLocalPosition;
        private Quaternion _rightHandRestLocalRotation = Quaternion.identity;
        private bool _hasCapturedRestPose;
        private bool? _reloadingOverrideForTests;

        public bool HasResolvedWeaponAnchors => _hasResolvedWeaponAnchors;
        public Transform LeftHandTarget => _leftHandTarget;
        public Transform LeftHandHint => _leftHandHint;
        public Transform RightHandTarget => _rightHandTarget;
        public TwoBoneIKConstraint LeftHandConstraint => _leftHandConstraint;
        public RigBuilder RigBuilder => _rigBuilder;

        private void Awake()
        {
            ResolveLocalDependencies();
            TryEnsureRigSetup();
            CaptureRestPose(force: true);
        }

        private void OnEnable()
        {
            ResolveLocalDependencies();
            TryEnsureRigSetup();
            CaptureRestPose(force: true);
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
            CaptureRestPose(force: true);
        }

        public void SetEquippedWeaponViewForTests(Transform weaponView)
        {
            _equippedWeaponViewOverride = weaponView;
            _cachedEquippedWeaponView = null;
            _cachedAnchors = null;
        }

        public void SetReloadingOverrideForTests(bool? isReloading)
        {
            _reloadingOverrideForTests = isReloading;
        }

        public void SyncHandTargets()
        {
            ResolveLocalDependencies();
            if (!TryEnsureRigSetup())
            {
                ClearRuntimeState();
                RestoreHandTargets();
                ApplyConstraintWeights(hasWeaponAnchors: false);
                return;
            }

            var weaponView = ResolveEquippedWeaponView();
            if (weaponView == null)
            {
                ClearRuntimeState();
                RestoreHandTargets();
                ApplyConstraintWeights(hasWeaponAnchors: false);
                return;
            }

            if (!ReferenceEquals(_cachedEquippedWeaponView, weaponView))
            {
                _cachedEquippedWeaponView = weaponView;
                _cachedAnchors = weaponView.GetComponent<WeaponViewHandAnchors>();
                _equippedViewName = weaponView.name;
            }

            if (_cachedAnchors == null)
            {
                ClearRuntimeState();
                RestoreHandTargets();
                ApplyConstraintWeights(hasWeaponAnchors: false);
                return;
            }

            var leftGrip = _cachedAnchors.LeftHandGrip;
            var rightGrip = _cachedAnchors.RightHandGrip;
            var hasDrivenGrip = (_driveLeftHand && leftGrip != null) || (_driveRightHand && rightGrip != null);
            if (!hasDrivenGrip
                || (_driveLeftHand && leftGrip == null)
                || (_driveRightHand && rightGrip == null))
            {
                ClearRuntimeState();
                RestoreHandTargets();
                ApplyConstraintWeights(hasWeaponAnchors: false);
                return;
            }

            _hasResolvedWeaponAnchors = true;
            if (leftGrip != null)
            {
                PushTargetPose(_leftHandTarget, leftGrip);
            }

            if (rightGrip != null)
            {
                PushTargetPose(_rightHandTarget, rightGrip);
            }
            ApplyConstraintWeights(hasWeaponAnchors: true);
        }

        private void ResolveLocalDependencies()
        {
            _weaponController ??= GetComponent<PlayerWeaponController>();
            _viewmodelAnimationAdapter ??= GetComponent<ViewmodelAnimationAdapter>();
            _cameraDefaults ??= GetComponent<PlayerCameraDefaults>();

            if (!IsAnimatorOnPlayerHierarchy(_armsAnimator))
            {
                if (_cameraDefaults != null && _cameraDefaults.TryGetPlayerArmsAnimator(out var playerArmsAnimator))
                {
                    _armsAnimator = playerArmsAnimator;
                }
                else
                {
                    _armsAnimator = null;
                }
            }

            if (_rigBuilder == null && _armsAnimator != null)
            {
                _rigBuilder = _armsAnimator.GetComponent<RigBuilder>();
            }
        }

        private bool TryEnsureRigSetup()
        {
            if (_armsAnimator == null)
            {
                return false;
            }

            var targetRoot = ResolveTargetRoot();
            if (targetRoot == null)
            {
                return false;
            }

            if (_rigBuilder == null)
            {
                _rigBuilder = _armsAnimator.GetComponent<RigBuilder>();
            }

            if (_rigBuilder == null)
            {
                return false;
            }

            if (_weaponHandRig == null)
            {
                _weaponHandRig = _armsAnimator.transform.Find(RigName)?.GetComponent<Rig>();
            }

            if (_weaponHandRig == null)
            {
                return false;
            }

            if (_driveLeftHand)
            {
                if (_leftHandTarget == null)
                {
                    _leftHandTarget = targetRoot.Find(LeftTargetName);
                }

                if (_leftHandHint == null)
                {
                    _leftHandHint = targetRoot.Find(LeftHintName);
                }

                if (_leftHandConstraint == null)
                {
                    _leftHandConstraint = _weaponHandRig.transform.Find(LeftConstraintName)?.GetComponent<TwoBoneIKConstraint>();
                }

                if (_leftHandTarget == null || _leftHandHint == null || _leftHandConstraint == null)
                {
                    return false;
                }

                if (!ConfigureConstraint(
                        _leftHandConstraint,
                        rootBoneName: "upperarm_l",
                        midBoneName: "lowerarm_l",
                        tipBoneName: "hand_l",
                        _leftHandTarget,
                        _leftHandHint))
                {
                    return false;
                }
            }

            if (_driveRightHand)
            {
                if (_rightHandTarget == null)
                {
                    _rightHandTarget = targetRoot.Find(RightTargetName);
                }

                if (_rightHandHint == null)
                {
                    _rightHandHint = targetRoot.Find(RightHintName);
                }

                if (_rightHandConstraint == null)
                {
                    _rightHandConstraint = _weaponHandRig.transform.Find(RightConstraintName)?.GetComponent<TwoBoneIKConstraint>();
                }

                if (_rightHandTarget == null || _rightHandHint == null || _rightHandConstraint == null)
                {
                    return false;
                }

                if (!ConfigureConstraint(
                        _rightHandConstraint,
                        rootBoneName: "upperarm_r",
                        midBoneName: "lowerarm_r",
                        tipBoneName: "hand_r",
                        _rightHandTarget,
                        _rightHandHint))
                {
                    return false;
                }
            }

            if (!HasRigLayer(_rigBuilder, _weaponHandRig))
            {
                return false;
            }

            return true;
        }

        private Transform ResolveTargetRoot()
        {
            if (!IsReferenceOnPlayerHierarchy(_handTargetRoot))
            {
                return null;
            }

            return _handTargetRoot;
        }

        private bool IsReferenceOnPlayerHierarchy(Transform candidate)
        {
            return candidate != null && (candidate == transform || candidate.IsChildOf(transform));
        }

        private bool IsAnimatorOnPlayerHierarchy(Animator animator)
        {
            return animator != null
                && animator.transform != null
                && (animator.transform == transform || animator.transform.IsChildOf(transform));
        }

        private bool ConfigureConstraint(
            TwoBoneIKConstraint constraint,
            string rootBoneName,
            string midBoneName,
            string tipBoneName,
            Transform target,
            Transform hint)
        {
            if (constraint == null || target == null || hint == null || _armsAnimator == null)
            {
                return false;
            }

            var rootBone = FindDescendantByName(_armsAnimator.transform, rootBoneName);
            var midBone = FindDescendantByName(_armsAnimator.transform, midBoneName);
            var tipBone = FindDescendantByName(_armsAnimator.transform, tipBoneName);
            if (rootBone == null || midBone == null || tipBone == null)
            {
                return false;
            }

            var data = constraint.data;
            data.root = rootBone;
            data.mid = midBone;
            data.tip = tipBone;
            data.target = target;
            data.hint = hint;
            data.targetPositionWeight = 1f;
            data.targetRotationWeight = 1f;
            data.hintWeight = 1f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = false;

            constraint.data = data;

            target.position = tipBone.position;
            target.rotation = tipBone.rotation;
            hint.position = ResolveDefaultHintPosition(rootBone, midBone, tipBone);
            hint.rotation = tipBone.rotation;
            return true;
        }

        private void RebuildRigGraph()
        {
            if (_rigBuilder == null || !Application.isPlaying)
            {
                return;
            }

            if (_armsAnimator == null || _armsAnimator.avatar == null)
            {
                return;
            }

            _rigBuilder.Clear();
            _rigBuilder.Build();
        }

        private static bool HasRigLayer(RigBuilder rigBuilder, Rig rig)
        {
            if (rigBuilder == null || rig == null)
            {
                return false;
            }

            var layers = rigBuilder.layers;
            for (var i = 0; i < layers.Count; i++)
            {
                if (ReferenceEquals(layers[i].rig, rig))
                {
                    return true;
                }
            }

            return false;
        }

        private Transform ResolveEquippedWeaponView()
        {
            if (_equippedWeaponViewOverride != null)
            {
                return _equippedWeaponViewOverride;
            }

            return _weaponController != null ? _weaponController.EquippedWeaponViewTransform : null;
        }

        private void CaptureRestPose(bool force)
        {
            if (_hasCapturedRestPose && !force)
            {
                return;
            }

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

            _hasCapturedRestPose = true;
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

        private void ApplyConstraintWeights(bool hasWeaponAnchors)
        {
            var isReloading = ResolveIsReloading();
            var leftWeight = hasWeaponAnchors && _driveLeftHand
                ? (isReloading && _releaseLeftHandDuringReload ? _leftHandReloadWeight : _leftHandActiveWeight)
                : 0f;
            var rightWeight = hasWeaponAnchors && _driveRightHand
                ? (isReloading ? _rightHandReloadWeight : _rightHandActiveWeight)
                : 0f;

            if (_leftHandConstraint != null)
            {
                _leftHandConstraint.weight = leftWeight;
            }

            if (_rightHandConstraint != null)
            {
                _rightHandConstraint.weight = rightWeight;
            }

            if (_weaponHandRig != null)
            {
                _weaponHandRig.weight = Mathf.Max(leftWeight, rightWeight);
            }
        }

        private bool ResolveIsReloading()
        {
            if (_reloadingOverrideForTests.HasValue)
            {
                return _reloadingOverrideForTests.Value;
            }

            return _viewmodelAnimationAdapter != null && _viewmodelAnimationAdapter.IsReloadingDebug;
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

        private Vector3 ResolveDefaultHintPosition(Transform rootBone, Transform midBone, Transform tipBone)
        {
            var rootToTip = tipBone.position - rootBone.position;
            if (rootToTip.sqrMagnitude <= 0.000001f)
            {
                return midBone.position + (_armsAnimator != null ? _armsAnimator.transform.right : Vector3.right) * _hintDistance;
            }

            var projectedMid = rootBone.position + Vector3.Project(midBone.position - rootBone.position, rootToTip);
            var bendDirection = midBone.position - projectedMid;
            if (bendDirection.sqrMagnitude <= 0.000001f)
            {
                bendDirection = _armsAnimator != null ? _armsAnimator.transform.right : Vector3.right;
            }

            return midBone.position + bendDirection.normalized * _hintDistance;
        }
    }
}
