using Reloader.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reloader.World.Travel;

namespace Reloader.World.Runtime
{
    public sealed class BootstrapWorldRoot : MonoBehaviour
    {
        public const string PlayerRootPrefabAssetPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";

        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const string MainTownSpawnEntryPointId = "entry.maintown.spawn";
        private const string RuntimePlayerRootInstanceName = "RuntimePlayerRoot";

        [SerializeField] private GameObject _playerRootPrefab;

        private void Awake()
        {
            Initialize();
            TryLoadMainTownFromBootstrap();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMainTownLoadedFromBootstrap()
        {
            TryLoadMainTownFromBootstrap();
        }

        public static PersistentPlayerRoot Initialize()
        {
            var persistentRoot = PersistentPlayerRoot.EnsureInstance();
            var bootstrapWorldRoot = FindBootstrapWorldRoot();
            if (bootstrapWorldRoot == null)
            {
                if (persistentRoot.PlayerRootTransform == null)
                {
                    Debug.LogError("BootstrapWorldRoot failed: no loaded BootstrapWorldRoot instance is available to provide the canonical runtime player prefab.");
                }

                return persistentRoot;
            }

            bootstrapWorldRoot.EnsureRuntimePlayerRoot(persistentRoot);
            return persistentRoot;
        }

        private static void TryLoadMainTownFromBootstrap()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != BootstrapSceneName)
            {
                return;
            }

            if (SceneManager.GetSceneByName(MainTownSceneName).isLoaded)
            {
                return;
            }

            WorldTravelCoordinator.TryLoadSceneAtEntry(MainTownSceneName, MainTownSpawnEntryPointId);
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
    }
}
