using UnityEngine;

namespace Reloader.Player
{
    public sealed class FpsViewmodelAnimatorDriver : MonoBehaviour
    {
        private static readonly Vector3 ExpectedViewmodelLocalPosition = new Vector3(0f, -0.027f, 0.1f);
        private static readonly Quaternion ExpectedViewmodelLocalRotation = Quaternion.identity;
        private static readonly Vector3 ExpectedViewmodelLocalScale = new Vector3(0.42f, 0.42f, 0.42f);

        [SerializeField] private Animator _animator;
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
                // Travel/scene swaps can momentarily leave stale animator refs.
                _animator = FindAnimatorWithController(transform);
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
            if (!IsAnimatorOnPlayerHierarchy(_animator))
            {
                _animator = ResolveViewmodelAnimator();
            }

            _characterController ??= GetComponent<CharacterController>();
        }

        private Animator ResolveViewmodelAnimator()
        {
            var explicitPath = transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual");
            if (explicitPath != null)
            {
                var explicitAnimator = explicitPath.GetComponent<Animator>() ?? explicitPath.GetComponentInChildren<Animator>(true);
                if (explicitAnimator != null)
                {
                    return explicitAnimator;
                }
            }

            var byName = FindDescendantByName(transform, "PlayerArmsVisual");
            if (byName != null)
            {
                var namedAnimator = byName.GetComponent<Animator>() ?? byName.GetComponentInChildren<Animator>(true);
                if (namedAnimator != null)
                {
                    return namedAnimator;
                }
            }

            return GetComponentInChildren<Animator>(true);
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendantByName(root.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private bool IsAnimatorOnPlayerHierarchy(Animator animator)
        {
            return animator != null
                && animator.transform != null
                && (animator.transform == transform || animator.transform.IsChildOf(transform));
        }

        private static Animator FindAnimatorWithController(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var animators = root.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var candidate = animators[i];
                if (candidate != null && candidate.runtimeAnimatorController != null)
                {
                    return candidate;
                }
            }

            return null;
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

            var viewmodelRoot = ResolveViewmodelRoot(_animator.transform);
            if (viewmodelRoot == null)
            {
                return;
            }

            var parent = viewmodelRoot.parent;
            if (parent == null || parent.name != "CameraPivot")
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

        private static Transform ResolveViewmodelRoot(Transform animatorTransform)
        {
            if (animatorTransform == null)
            {
                return null;
            }

            if (animatorTransform.name == "PlayerArms")
            {
                return animatorTransform;
            }

            var current = animatorTransform.parent;
            while (current != null)
            {
                if (current.name == "PlayerArms")
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
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
