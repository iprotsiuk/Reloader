using UnityEngine;

namespace Reloader.World.Runtime.Origin
{
    public interface IOriginRebaseParticipant
    {
        void OnBeforeOriginRebase(Vector3 localShift, Vector3 stableShift);
        void OnAfterOriginRebase(Vector3 localShift, Vector3 stableShift);
    }
}
