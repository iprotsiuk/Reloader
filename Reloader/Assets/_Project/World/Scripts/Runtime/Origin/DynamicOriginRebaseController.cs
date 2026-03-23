using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Runtime.Origin
{
    [DefaultExecutionOrder(10000)]
    public sealed class DynamicOriginRebaseController : MonoBehaviour
    {
        [SerializeField] private PersistentPlayerRoot _persistentPlayerRoot;
        [SerializeField] private DynamicOriginRebaseState _rebaseState;
        [SerializeField] private StableWorldCoordinateBridge _coordinateBridge;
        [SerializeField] private float _rebaseDistanceMeters = 500f;
        [SerializeField] private float _rebaseCooldownSeconds = 1f;
        [SerializeField] private float _lastRebaseTime = float.NegativeInfinity;
        [SerializeField] private Vector3 _playerHorizontalBaseline;

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
            CapturePlayerHorizontalBaseline();
            _lastRebaseTime = float.NegativeInfinity;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            TryRebaseIfNeeded(Time.unscaledTime);
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

            var localShift = new Vector3(
                _playerHorizontalBaseline.x - playerRoot.position.x,
                0f,
                _playerHorizontalBaseline.z - playerRoot.position.z);
            if (new Vector2(localShift.x, localShift.z).sqrMagnitude <= 0f)
            {
                return false;
            }

            var stableShift = -localShift;
            var affectedScenes = CollectAffectedScenes(playerRoot.gameObject.scene);
            var participants = CollectParticipants(affectedScenes);

            NotifyBefore(participants, localShift, stableShift);
            ShiftSceneRoots(affectedScenes, localShift);
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

        private void CapturePlayerHorizontalBaseline()
        {
            var playerRoot = ResolveCanonicalPlayerRoot();
            if (playerRoot == null)
            {
                _playerHorizontalBaseline = Vector3.zero;
                return;
            }

            _playerHorizontalBaseline = new Vector3(playerRoot.position.x, 0f, playerRoot.position.z);
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

        private static Scene[] CollectAffectedScenes(Scene playerScene)
        {
            var handles = new System.Collections.Generic.HashSet<int>();
            var scenes = new System.Collections.Generic.List<Scene>();

            AddScene(playerScene, scenes, handles);
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                AddScene(SceneManager.GetSceneAt(i), scenes, handles);
            }

            return scenes.ToArray();
        }

        private static IOriginRebaseParticipant[] CollectParticipants(Scene[] scenes)
        {
            var sceneHandles = new System.Collections.Generic.HashSet<int>();
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].IsValid())
                {
                    sceneHandles.Add(scenes[i].handle);
                }
            }

            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var participants = new System.Collections.Generic.List<IOriginRebaseParticipant>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || !sceneHandles.Contains(behaviour.gameObject.scene.handle))
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

        private static void AddScene(Scene scene, System.Collections.Generic.List<Scene> scenes, System.Collections.Generic.HashSet<int> handles)
        {
            if (!scene.IsValid() || !scene.isLoaded || !handles.Add(scene.handle))
            {
                return;
            }

            scenes.Add(scene);
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

        private static void ShiftSceneRoots(Scene[] scenes, Vector3 localShift)
        {
            for (var i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    roots[rootIndex].transform.position += localShift;
                }
            }
        }
    }
}
