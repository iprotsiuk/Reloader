using UnityEngine;

namespace Reloader.Player
{
    public sealed class FpsViewmodelAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerMovementSettings _movementSettings = new PlayerMovementSettings();
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private float _damping = 10f;

        private int _speedHash;
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
            _animator.SetFloat(_speedHash, _current);
        }

        public void Configure(Animator animator, CharacterController characterController)
        {
            _animator = animator;
            _characterController = characterController;
            CacheParameter();
        }

        private void ResolveReferences()
        {
            _animator ??= GetComponentInChildren<Animator>(true);
            _characterController ??= GetComponent<CharacterController>();
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

        private void CacheParameter()
        {
            _speedHash = Animator.StringToHash(string.IsNullOrWhiteSpace(_speedParameter) ? "Speed" : _speedParameter);
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
