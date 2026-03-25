using UnityEngine;

namespace Reloader.Player
{
    public sealed class FpsViewmodelAnimatorDriver : MonoBehaviour
    {
        private static readonly Vector3 ExpectedViewmodelLocalPosition = new Vector3(0f, -0.027f, 0.1f);
        private static readonly Quaternion ExpectedViewmodelLocalRotation = Quaternion.identity;
        private static readonly Vector3 ExpectedViewmodelLocalScale = new Vector3(0.42f, 0.42f, 0.42f);

        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private Transform _viewmodelRoot;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerMovementSettings _movementSettings = new PlayerMovementSettings();
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private string _movementParameter = "Movement";
        [SerializeField] private string _runningParameter = "Running";
        [SerializeField] private float _damping = 10f;
        [SerializeField] private bool _lockViewmodelRootPose;

        private int _speedHash;
        private int _movementHash;
        private int _runningHash;
        private float _current;

        public bool LockViewmodelRootPose
        {
            get => _lockViewmodelRootPose;
            set => _lockViewmodelRootPose = value;
        }

        private void Awake()
        {
            CacheParameter();
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (_animator == null || _characterController == null)
            {
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                _animator = ResolveViewmodelAnimator();
                if (_animator == null)
                {
                    return;
                }
            }

            var horizontalVelocity = _characterController.velocity;
            horizontalVelocity.y = 0f;
            var target = NormalizeSpeed(
                horizontalVelocity.magnitude,
                _movementSettings != null ? _movementSettings.WalkSpeed : 0f,
                _movementSettings != null ? _movementSettings.SprintSpeed : 0f);
            _current = Mathf.Lerp(_current, target, Time.deltaTime * _damping);
            if (!HasParameter(_animator, _speedHash, AnimatorControllerParameterType.Float))
            {
                if (HasParameter(_animator, _movementHash, AnimatorControllerParameterType.Float))
                {
                    _animator.SetFloat(_movementHash, _current);
                }
            }
            else
            {
                _animator.SetFloat(_speedHash, _current);
            }

            if (HasParameter(_animator, _runningHash, AnimatorControllerParameterType.Bool))
            {
                _animator.SetBool(_runningHash, _current > 0.1f);
            }
        }

        private void LateUpdate()
        {
            ResolveReferences();
            StabilizeViewmodelRootPose();
        }

        public void Configure(Animator animator, CharacterController characterController)
        {
            _animator = animator;
            _characterController = characterController;
            CacheParameter();
        }

        private void ResolveReferences()
        {
            _cameraDefaults ??= GetComponent<PlayerCameraDefaults>();
            _animator = ResolveViewmodelAnimator();
            _viewmodelRoot = ResolveViewmodelRoot();
            EnsureViewmodelAnimatorRuntimeState();

            _characterController ??= GetComponent<CharacterController>();
        }

        private Animator ResolveViewmodelAnimator()
        {
            if (_cameraDefaults != null
                && _cameraDefaults.TryGetPlayerArmsAnimator(out var playerArmsAnimator)
                && IsAnimatorOnPlayerHierarchy(playerArmsAnimator))
            {
                return playerArmsAnimator;
            }

            return IsAnimatorOnPlayerHierarchy(_animator) ? _animator : null;
        }

        private bool IsAnimatorOnPlayerHierarchy(Animator animator)
        {
            return animator != null
                && animator.transform != null
                && (animator.transform == transform || animator.transform.IsChildOf(transform));
        }

        private bool IsTransformOnPlayerHierarchy(Transform candidate)
        {
            return candidate != null && (candidate == transform || candidate.IsChildOf(transform));
        }

        private void EnsureViewmodelAnimatorRuntimeState()
        {
            if (_animator == null)
            {
                return;
            }

            if (_animator.applyRootMotion)
            {
                _animator.applyRootMotion = false;
            }

            if (_animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void StabilizeViewmodelRootPose()
        {
            if (!_lockViewmodelRootPose)
            {
                return;
            }

            if (_animator == null)
            {
                return;
            }

            var viewmodelRoot = ResolveViewmodelRoot();
            if (viewmodelRoot == null)
            {
                return;
            }

            if (_animator.transform != viewmodelRoot && !_animator.transform.IsChildOf(viewmodelRoot))
            {
                return;
            }

            if (_animator.applyRootMotion)
            {
                _animator.applyRootMotion = false;
            }

            if (_animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            if (!viewmodelRoot.gameObject.activeSelf)
            {
                viewmodelRoot.gameObject.SetActive(true);
            }

            if ((viewmodelRoot.localPosition - ExpectedViewmodelLocalPosition).sqrMagnitude > 0.000001f)
            {
                viewmodelRoot.localPosition = ExpectedViewmodelLocalPosition;
            }

            if (Quaternion.Angle(viewmodelRoot.localRotation, ExpectedViewmodelLocalRotation) > 0.1f)
            {
                viewmodelRoot.localRotation = ExpectedViewmodelLocalRotation;
            }

            if ((viewmodelRoot.localScale - ExpectedViewmodelLocalScale).sqrMagnitude > 0.000001f)
            {
                viewmodelRoot.localScale = ExpectedViewmodelLocalScale;
            }

            var renderers = viewmodelRoot.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && !renderer.enabled)
                {
                    renderer.enabled = true;
                }
            }
        }

        private Transform ResolveViewmodelRoot()
        {
            if (_cameraDefaults != null
                && _cameraDefaults.TryGetPlayerArmsRoot(out var playerArmsRoot)
                && IsTransformOnPlayerHierarchy(playerArmsRoot))
            {
                return playerArmsRoot;
            }

            return IsTransformOnPlayerHierarchy(_viewmodelRoot) ? _viewmodelRoot : null;
        }

        private static bool HasParameter(Animator animator, int hash, AnimatorControllerParameterType type)
        {
            if (animator == null)
            {
                return false;
            }

            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.nameHash == hash && parameter.type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheParameter()
        {
            _speedHash = Animator.StringToHash(string.IsNullOrWhiteSpace(_speedParameter) ? "Speed" : _speedParameter);
            _movementHash = Animator.StringToHash(string.IsNullOrWhiteSpace(_movementParameter) ? "Movement" : _movementParameter);
            _runningHash = Animator.StringToHash(string.IsNullOrWhiteSpace(_runningParameter) ? "Running" : _runningParameter);
        }

        public static float NormalizeSpeed(float horizontalSpeed, float walkSpeed, float sprintSpeed)
        {
            var referenceMaxSpeed = Mathf.Max(0f, walkSpeed, sprintSpeed);
            if (referenceMaxSpeed <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(horizontalSpeed / referenceMaxSpeed);
        }
    }
}
