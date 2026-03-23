using UnityEngine;

namespace Reloader.World.Runtime.Origin
{
    public sealed class DynamicOriginRebaseState : MonoBehaviour
    {
        [SerializeField] private Vector3 _stableOriginOffset;
        [SerializeField] private Vector3 _localOriginOffset;
        [SerializeField] private float _lastRebaseTime = float.NegativeInfinity;

        public Vector3 StableOriginOffset => _stableOriginOffset;
        public Vector3 LocalOriginOffset => _localOriginOffset;
        public float LastRebaseTime => _lastRebaseTime;

        public void ResetState()
        {
            _stableOriginOffset = Vector3.zero;
            _localOriginOffset = Vector3.zero;
            _lastRebaseTime = float.NegativeInfinity;
        }

        public void ApplyRebase(Vector3 localShift, Vector3 stableShift, float rebaseTime)
        {
            _localOriginOffset += localShift;
            _stableOriginOffset += stableShift;
            _lastRebaseTime = rebaseTime;
        }
    }
}
