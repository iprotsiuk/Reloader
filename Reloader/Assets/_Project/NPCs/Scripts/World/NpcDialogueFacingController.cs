using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class NpcDialogueFacingController : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeedDegreesPerSecond = 540f;

        private Transform _target;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void StartFacing(Transform target)
        {
            _target = target;
        }

        public void StopFacing()
        {
            _target = null;
        }

        internal void Tick(float deltaTime)
        {
            if (_target == null)
            {
                return;
            }

            var sampleDeltaTime = deltaTime > 0f ? deltaTime : Time.deltaTime;
            RotateTowardTarget(_rotationSpeedDegreesPerSecond * sampleDeltaTime);
        }

        private void RotateTowardTarget(float maxDegreesDelta)
        {
            if (_target == null)
            {
                return;
            }

            var toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                maxDegreesDelta);
        }
    }
}
