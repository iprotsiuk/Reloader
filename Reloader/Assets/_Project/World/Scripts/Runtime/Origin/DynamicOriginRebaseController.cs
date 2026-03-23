using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Runtime.Origin
{
    public sealed class DynamicOriginRebaseController : MonoBehaviour
    {
        [SerializeField] private PersistentPlayerRoot _persistentPlayerRoot;
        [SerializeField] private DynamicOriginRebaseState _rebaseState;
        [SerializeField] private StableWorldCoordinateBridge _coordinateBridge;
        [SerializeField] private float _rebaseDistanceMeters = 500f;
        [SerializeField] private float _rebaseCooldownSeconds = 1f;
        [SerializeField] private float _lastRebaseTime = float.NegativeInfinity;

        public PersistentPlayerRoot PersistentPlayerRoot => _persistentPlayerRoot;
        public DynamicOriginRebaseState RebaseState => _rebaseState;
        public StableWorldCoordinateBridge CoordinateBridge => _coordinateBridge;
        public float RebaseDistanceMeters => _rebaseDistanceMeters;
        public float RebaseCooldownSeconds => _rebaseCooldownSeconds;
        public float LastRebaseTime => _lastRebaseTime;

        public void Configure(PersistentPlayerRoot persistentPlayerRoot, DynamicOriginRebaseState rebaseState, StableWorldCoordinateBridge coordinateBridge)
        {
            _persistentPlayerRoot = persistentPlayerRoot;
            _rebaseState = rebaseState;
            _coordinateBridge = coordinateBridge;
        }

        public void ResetState()
        {
            _lastRebaseTime = float.NegativeInfinity;
        }

        public bool TryRebaseIfNeeded(float currentTimeSeconds)
        {
            var playerRoot = ResolveCanonicalPlayerRoot();
            if (playerRoot == null || _rebaseState == null || _coordinateBridge == null)
            {
                return false;
            }

            if (!IsOutsideRebaseDistance(playerRoot.position))
            {
                return false;
            }

            if (!IsPastCooldown(currentTimeSeconds))
            {
                return false;
            }

            var localShift = new Vector3(-playerRoot.position.x, 0f, -playerRoot.position.z);
            if (new Vector2(localShift.x, localShift.z).sqrMagnitude <= 0f)
            {
                return false;
            }

            var stableShift = -localShift;
            var participants = CollectParticipants(playerRoot.gameObject.scene);

            NotifyBefore(participants, localShift, stableShift);
            ShiftSceneRoots(playerRoot.gameObject.scene, localShift);
            _rebaseState.ApplyRebase(localShift, stableShift, currentTimeSeconds);
            _lastRebaseTime = currentTimeSeconds;
            NotifyAfter(participants, localShift, stableShift);
            return true;
        }

        private Transform ResolveCanonicalPlayerRoot()
        {
            if (_persistentPlayerRoot != null && _persistentPlayerRoot.PlayerRootTransform != null)
            {
                return _persistentPlayerRoot.PlayerRootTransform;
            }

            return PersistentPlayerRoot.Instance?.PlayerRootTransform;
        }

        private bool IsOutsideRebaseDistance(Vector3 localPosition)
        {
            return _coordinateBridge.ComputeHorizontalDistanceFromLocalOrigin(localPosition) >= _rebaseDistanceMeters;
        }

        private bool IsPastCooldown(float currentTimeSeconds)
        {
            if (float.IsNegativeInfinity(_lastRebaseTime))
            {
                return true;
            }

            return currentTimeSeconds - _lastRebaseTime >= _rebaseCooldownSeconds;
        }

        private static IOriginRebaseParticipant[] CollectParticipants(Scene scene)
        {
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var participants = new System.Collections.Generic.List<IOriginRebaseParticipant>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.gameObject.scene != scene)
                {
                    continue;
                }

                if (behaviour is IOriginRebaseParticipant participant)
                {
                    participants.Add(participant);
                }
            }

            return participants.ToArray();
        }

        private static void NotifyBefore(IOriginRebaseParticipant[] participants, Vector3 localShift, Vector3 stableShift)
        {
            for (var i = 0; i < participants.Length; i++)
            {
                participants[i].OnBeforeOriginRebase(localShift, stableShift);
            }
        }

        private static void NotifyAfter(IOriginRebaseParticipant[] participants, Vector3 localShift, Vector3 stableShift)
        {
            for (var i = 0; i < participants.Length; i++)
            {
                participants[i].OnAfterOriginRebase(localShift, stableShift);
            }
        }

        private static void ShiftSceneRoots(Scene scene, Vector3 localShift)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                roots[i].transform.position += localShift;
            }
        }
    }
}
