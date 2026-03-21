using Reloader.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reloader.World.Travel;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.World.Runtime
{
    public sealed class BootstrapWorldRoot : MonoBehaviour
    {
        public const string PlayerRootPrefabAssetPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";

        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const string MainTownSpawnEntryPointId = "entry.maintown.spawn";
        private const string RuntimePlayerRootInstanceName = "RuntimePlayerRoot";

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
            EnsureRuntimePlayerRoot(persistentRoot);
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

        private static void EnsureRuntimePlayerRoot(PersistentPlayerRoot persistentRoot)
        {
            if (persistentRoot == null || persistentRoot.PlayerRootTransform != null)
            {
                return;
            }

            var playerRootPrefab = LoadPlayerRootPrefab();
            if (playerRootPrefab == null)
            {
                Debug.LogError($"BootstrapWorldRoot failed: missing canonical runtime player prefab at '{PlayerRootPrefabAssetPath}'.");
                return;
            }

            var playerRootInstance = InstantiatePlayerRootPrefab(playerRootPrefab);
            if (playerRootInstance == null)
            {
                Debug.LogError($"BootstrapWorldRoot failed: could not instantiate canonical runtime player prefab '{PlayerRootPrefabAssetPath}'.");
                return;
            }

            playerRootInstance.name = RuntimePlayerRootInstanceName;
            persistentRoot.RegisterRuntimePlayerRoot(playerRootInstance.transform);
            playerRootInstance.GetComponent<PlayerCameraDefaults>()?.ApplyDefaults();
        }

        private static GameObject LoadPlayerRootPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRootPrefabAssetPath);
#else
            Debug.LogError($"BootstrapWorldRoot failed: runtime player prefab '{PlayerRootPrefabAssetPath}' requires Task 3 scene wiring outside the editor.");
            return null;
#endif
        }

        private static GameObject InstantiatePlayerRootPrefab(GameObject playerRootPrefab)
        {
            if (playerRootPrefab == null)
            {
                return null;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return PrefabUtility.InstantiatePrefab(playerRootPrefab) as GameObject;
            }
#endif
            return Instantiate(playerRootPrefab);
        }
    }
}
