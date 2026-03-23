using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WeaponViewHandAnchors : MonoBehaviour
    {
        [SerializeField] private Transform _leftHandGrip;
        [SerializeField] private Transform _rightHandGrip;

        public Transform LeftHandGrip => _leftHandGrip;
        public Transform RightHandGrip => _rightHandGrip;

        public bool HasCompleteHandAnchorSet => _leftHandGrip != null && _rightHandGrip != null;

        public void SetHandTargets(Transform leftHandGrip, Transform rightHandGrip)
        {
            _leftHandGrip = leftHandGrip;
            _rightHandGrip = rightHandGrip;
        }

        public bool TryGetHandTargets(out Transform leftHandGrip, out Transform rightHandGrip)
        {
            leftHandGrip = _leftHandGrip;
            rightHandGrip = _rightHandGrip;
            return HasCompleteHandAnchorSet;
        }
    }
}
