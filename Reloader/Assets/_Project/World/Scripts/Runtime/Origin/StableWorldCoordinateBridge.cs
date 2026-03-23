using UnityEngine;

namespace Reloader.World.Runtime.Origin
{
    public sealed class StableWorldCoordinateBridge : MonoBehaviour
    {
        [SerializeField] private DynamicOriginRebaseState _state;

        public DynamicOriginRebaseState State => _state;

        public void Initialize(DynamicOriginRebaseState state)
        {
            _state = state;
        }

        public Vector3 LocalToStable(Vector3 localPosition)
        {
            return localPosition + GetStableOriginOffset();
        }

        public Vector3 StableToLocal(Vector3 stablePosition)
        {
            return stablePosition - GetStableOriginOffset();
        }

        public Vector3 LocalDirectionToStable(Vector3 localDirection)
        {
            return localDirection;
        }

        public float ComputeHorizontalDistanceFromLocalOrigin(Vector3 localPosition)
        {
            var horizontal = new Vector2(localPosition.x, localPosition.z);
            return horizontal.magnitude;
        }

        private Vector3 GetStableOriginOffset()
        {
            return _state != null ? _state.StableOriginOffset : Vector3.zero;
        }
    }
}
