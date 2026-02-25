using UnityEngine;

namespace Reloader.Player
{
    public sealed class PlayerLookController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private Transform _pitchTransform;
        [SerializeField] private Vector2 _lookSensitivity = Vector2.one;
        [SerializeField] private Vector2 _pitchClamp = new Vector2(-85f, 85f);
        [SerializeField] private bool _scaleByDeltaTime;

        private IPlayerInputSource _inputSource;
        private float _yaw;
        private float _pitch;

        public Vector2 LookSensitivity
        {
            get => _lookSensitivity;
            set => _lookSensitivity = value;
        }

        public Vector2 PitchClamp
        {
            get => _pitchClamp;
            set => _pitchClamp = value;
        }

        private void Awake()
        {
            ResolveReferences();
            _yaw = transform.eulerAngles.y;
            _pitch = _pitchTransform != null ? Mathf.DeltaAngle(0f, _pitchTransform.localEulerAngles.x) : 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(IPlayerInputSource inputSource, Transform pitchTransform)
        {
            _inputSource = inputSource;
            _pitchTransform = pitchTransform;
            _yaw = transform.eulerAngles.y;
            _pitch = _pitchTransform != null ? Mathf.DeltaAngle(0f, _pitchTransform.localEulerAngles.x) : 0f;
        }

        public void SetInputSource(MonoBehaviour source)
        {
            _inputSourceBehaviour = source;
            _inputSource = source as IPlayerInputSource;
        }

        public void SetPitchTransform(Transform pitchTransform)
        {
            _pitchTransform = pitchTransform;
        }

        public void Tick(float deltaTime)
        {
            ResolveReferences();
            if (_inputSource == null)
            {
                return;
            }

            var lookInput = _inputSource.LookInput;
            var scale = _scaleByDeltaTime ? deltaTime : 1f;

            _yaw += lookInput.x * _lookSensitivity.x * scale;
            _pitch -= lookInput.y * _lookSensitivity.y * scale;
            _pitch = Mathf.Clamp(_pitch, _pitchClamp.x, _pitchClamp.y);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_pitchTransform != null)
            {
                _pitchTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
        }
    }
}
