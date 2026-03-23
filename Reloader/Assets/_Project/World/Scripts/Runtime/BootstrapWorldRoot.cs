using Reloader.Player;
using UnityEngine;
using Reloader.World.Travel;
using Reloader.World.Runtime.Origin;

namespace Reloader.World.Runtime
{
    public sealed class BootstrapWorldRoot : MonoBehaviour
    {
        public const string PlayerRootPrefabAssetPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";
        private const string RuntimePlayerRootInstanceName = "RuntimePlayerRoot";

        [SerializeField] private GameObject _playerRootPrefab;

        public static PersistentPlayerRoot Initialize()
        {
            var bootstrapWorldRoot = FindBootstrapWorldRoot();
            if (bootstrapWorldRoot == null)
            {
                Debug.LogError("BootstrapWorldRoot failed: no loaded BootstrapWorldRoot instance is available to provide the canonical runtime player prefab.");
                return null;
            }

            var persistentRoot = PersistentPlayerRoot.EnsureInstance();
            bootstrapWorldRoot.EnsureRuntimePlayerRoot(persistentRoot);
            bootstrapWorldRoot.EnsureOriginSeams(persistentRoot);
            return persistentRoot;
        }

        private void EnsureRuntimePlayerRoot(PersistentPlayerRoot persistentRoot)
        {
            if (persistentRoot == null || persistentRoot.PlayerRootTransform != null)
            {
                return;
            }

            if (_playerRootPrefab == null)
            {
                Debug.LogError($"BootstrapWorldRoot failed: missing canonical runtime player prefab reference. Expected '{PlayerRootPrefabAssetPath}' on the BootstrapWorldRoot scene component.");
                return;
            }

            var playerRootInstance = InstantiatePlayerRootPrefab(_playerRootPrefab);
            if (playerRootInstance == null)
            {
                Debug.LogError($"BootstrapWorldRoot failed: could not instantiate canonical runtime player prefab '{PlayerRootPrefabAssetPath}'.");
                return;
            }

            playerRootInstance.name = RuntimePlayerRootInstanceName;
            persistentRoot.RegisterRuntimePlayerRoot(playerRootInstance.transform);
            if (Application.isPlaying)
            {
                playerRootInstance.GetComponent<PlayerCameraDefaults>()?.ApplyDefaults();
            }
        }

        private void EnsureOriginSeams(PersistentPlayerRoot persistentRoot)
        {
            if (persistentRoot == null)
            {
                return;
            }

            var owner = persistentRoot.gameObject;
            var rebaseState = EnsureSingleComponent<DynamicOriginRebaseState>(owner);
            var coordinateBridge = EnsureSingleComponent<StableWorldCoordinateBridge>(owner);
            var rebaseController = EnsureSingleComponent<DynamicOriginRebaseController>(owner);

            rebaseState.ResetState();
            coordinateBridge.Initialize(rebaseState);
            rebaseController.Configure(persistentRoot, rebaseState, coordinateBridge);
            rebaseController.ResetState();
        }

        private static BootstrapWorldRoot FindBootstrapWorldRoot()
        {
            return Object.FindFirstObjectByType<BootstrapWorldRoot>(FindObjectsInactive.Include);
        }

        private static GameObject InstantiatePlayerRootPrefab(GameObject playerRootPrefab)
        {
            if (playerRootPrefab == null)
            {
                return null;
            }

            return Instantiate(playerRootPrefab);
        }

        private static T EnsureSingleComponent<T>(GameObject owner) where T : Component
        {
            var components = owner.GetComponents<T>();
            if (components.Length == 0)
            {
                return owner.AddComponent<T>();
            }

            var canonical = components[0];
            for (var i = 1; i < components.Length; i++)
            {
                DestroyComponent(components[i]);
            }

            return canonical;
        }

        private static void DestroyComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(component);
                return;
            }

            DestroyImmediate(component);
        }
    }
}
