using UnityEngine;
using UnityEngine.SceneManagement;
using Reloader.World.Travel;

namespace Reloader.World.Runtime
{
    public sealed class BootstrapWorldRoot : MonoBehaviour
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const string MainTownSpawnEntryPointId = "entry.maintown.spawn";

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
            return PersistentPlayerRoot.EnsureInstance();
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
    }
}
