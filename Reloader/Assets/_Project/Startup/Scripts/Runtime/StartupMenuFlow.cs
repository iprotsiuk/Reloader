using System;
using System.IO;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Startup.Runtime
{
    public interface IStartupMenuFlow
    {
        StartupMenuState RefreshState();
        bool TryStartNewGame();
        bool TryContinueLatest();
    }

    public interface IStartupMenuSceneTravel
    {
        bool TryLoadSceneAtEntry(string sceneName, string entryPointId);
    }

    public interface IStartupSaveLoader
    {
        void Load(string absolutePath);
    }

    public interface IStartupRuntimeBootstrapper
    {
        void EnsureCanonicalRuntimePlayerRoot();
    }

    public interface IStartupDeferredContinueLoad
    {
        void Schedule(string sceneName, string entryPointId, Action restoreAction);
        void CancelPending();
    }

    public sealed class StartupMenuFlow : IStartupMenuFlow
    {
        private const string NewGameSceneName = "MainTown";
        private const string NewGameEntryPointId = "entry.maintown.spawn";
        private const string SaveDirectoryName = "Saves";
        private const string PlayerStateModuleKey = "PlayerState";

        private readonly SaveFileRepository _fileRepository;
        private readonly IStartupMenuSceneTravel _sceneTravel;
        private readonly IStartupSaveLoader _saveLoader;
        private readonly IStartupRuntimeBootstrapper _runtimeBootstrapper;
        private readonly IStartupDeferredContinueLoad _deferredContinueLoad;
        private readonly string _saveDirectoryPath;

        public StartupMenuFlow()
            : this(
                new SaveFileRepository(),
                new UnityStartupMenuSceneTravel(),
                new SaveCoordinatorSaveLoader(SaveBootstrapper.CreateDefaultCoordinator()),
                new UnityStartupRuntimeBootstrapper(),
                new UnityStartupDeferredContinueLoad(),
                GetDefaultSaveDirectoryPath())
        {
        }

        public StartupMenuFlow(
            SaveFileRepository fileRepository,
            IStartupMenuSceneTravel sceneTravel,
            IStartupSaveLoader saveLoader,
            IStartupRuntimeBootstrapper runtimeBootstrapper,
            IStartupDeferredContinueLoad deferredContinueLoad,
            string saveDirectoryPath)
        {
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _sceneTravel = sceneTravel ?? throw new ArgumentNullException(nameof(sceneTravel));
            _saveLoader = saveLoader ?? throw new ArgumentNullException(nameof(saveLoader));
            _runtimeBootstrapper = runtimeBootstrapper ?? throw new ArgumentNullException(nameof(runtimeBootstrapper));
            _deferredContinueLoad = deferredContinueLoad ?? throw new ArgumentNullException(nameof(deferredContinueLoad));
            _saveDirectoryPath = string.IsNullOrWhiteSpace(saveDirectoryPath)
                ? throw new ArgumentException("Save directory path is required.", nameof(saveDirectoryPath))
                : saveDirectoryPath;
        }

        public StartupMenuState RefreshState()
        {
            var savePaths = _fileRepository.GetSavePathsNewestFirst(_saveDirectoryPath);
            for (var i = 0; i < savePaths.Length; i++)
            {
                var state = TryBuildStateFromSave(savePaths[i]);
                if (state.CanContinue)
                {
                    return state;
                }
            }

            return StartupMenuState.Empty;
        }

        public bool TryStartNewGame()
        {
            _runtimeBootstrapper.EnsureCanonicalRuntimePlayerRoot();
            return _sceneTravel.TryLoadSceneAtEntry(NewGameSceneName, NewGameEntryPointId);
        }

        public bool TryContinueLatest()
        {
            var state = RefreshState();
            if (!state.CanContinue)
            {
                return false;
            }

            _runtimeBootstrapper.EnsureCanonicalRuntimePlayerRoot();
            _deferredContinueLoad.Schedule(state.CurrentSceneName, state.CurrentAnchorId, () =>
            {
                try
                {
                    _saveLoader.Load(state.LatestSavePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Startup menu failed to continue from save '{state.LatestSavePath}': {ex.Message}");
                }
            });

            if (!_sceneTravel.TryLoadSceneAtEntry(state.CurrentSceneName, state.CurrentAnchorId))
            {
                _deferredContinueLoad.CancelPending();
                return false;
            }

            return true;
        }

        private StartupMenuState TryBuildStateFromSave(string savePath)
        {
            try
            {
                var envelope = _fileRepository.ReadEnvelope(savePath);
                return BuildStateFromEnvelope(savePath, envelope);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Startup menu failed to read save '{savePath}': {ex.Message}");
                return StartupMenuState.Empty;
            }
        }

        private static StartupMenuState BuildStateFromEnvelope(string latestSavePath, SaveEnvelope envelope)
        {
            if (envelope?.Modules == null || !envelope.Modules.TryGetValue(PlayerStateModuleKey, out var playerStateBlock) || playerStateBlock == null)
            {
                return StartupMenuState.Empty;
            }

            var playerState = new PlayerStateModule();
            try
            {
                playerState.RestoreModuleStateFromJson(playerStateBlock.PayloadJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Startup menu failed to parse PlayerState from '{latestSavePath}': {ex.Message}");
                return StartupMenuState.Empty;
            }

            var currentScenePath = playerState.CurrentScenePath?.Trim() ?? string.Empty;
            var currentAnchorId = playerState.CurrentAnchorId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentScenePath) || string.IsNullOrWhiteSpace(currentAnchorId))
            {
                return StartupMenuState.Empty;
            }

            var statusMessage = $"Continue: {Path.GetFileNameWithoutExtension(currentScenePath)} @ {currentAnchorId}";
            return new StartupMenuState(latestSavePath, currentScenePath, true, statusMessage, currentAnchorId);
        }

        private static string GetDefaultSaveDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, SaveDirectoryName);
        }
    }

    internal sealed class UnityStartupMenuSceneTravel : IStartupMenuSceneTravel
    {
        public bool TryLoadSceneAtEntry(string sceneName, string entryPointId)
        {
            return WorldTravelCoordinator.TryLoadSceneAtEntry(sceneName, entryPointId);
        }
    }

    internal sealed class SaveCoordinatorSaveLoader : IStartupSaveLoader
    {
        private readonly SaveCoordinator _saveCoordinator;

        public SaveCoordinatorSaveLoader(SaveCoordinator saveCoordinator)
        {
            _saveCoordinator = saveCoordinator ?? throw new ArgumentNullException(nameof(saveCoordinator));
        }

        public void Load(string absolutePath)
        {
            _saveCoordinator.Load(absolutePath);
        }
    }

    internal sealed class UnityStartupRuntimeBootstrapper : IStartupRuntimeBootstrapper
    {
        public void EnsureCanonicalRuntimePlayerRoot()
        {
            Reloader.World.Runtime.BootstrapWorldRoot.Initialize();
        }
    }

    internal sealed class UnityStartupDeferredContinueLoad : IStartupDeferredContinueLoad
    {
        private string _pendingSceneName;
        private string _pendingEntryPointId;
        private Action _restoreAction;

        public void Schedule(string sceneName, string entryPointId, Action restoreAction)
        {
            if (restoreAction == null)
            {
                throw new ArgumentNullException(nameof(restoreAction));
            }

            ClearPending();
            _pendingSceneName = sceneName?.Trim() ?? string.Empty;
            _pendingEntryPointId = entryPointId?.Trim() ?? string.Empty;
            _restoreAction = restoreAction;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        public void CancelPending()
        {
            ClearPending();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_restoreAction == null || !IsMatchingPendingScene(scene, _pendingSceneName))
            {
                return;
            }

            var shouldRestore = string.Equals(WorldTravelCoordinator.LastResolvedEntryPointId, _pendingEntryPointId, StringComparison.Ordinal);
            if (shouldRestore)
            {
                InvokeRestoreAndClear();
            }
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (_restoreAction == null || !IsMatchingPendingScene(nextScene, _pendingSceneName))
            {
                return;
            }

            InvokeRestoreAndClear();
        }

        private void InvokeRestoreAndClear()
        {
            var restoreAction = _restoreAction;
            ClearPending();
            restoreAction?.Invoke();
        }

        private void ClearPending()
        {
            if (_restoreAction != null)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            }

            _pendingSceneName = string.Empty;
            _pendingEntryPointId = string.Empty;
            _restoreAction = null;
        }

        private static bool IsMatchingPendingScene(Scene loadedScene, string pendingSceneIdentifier)
        {
            if (string.IsNullOrWhiteSpace(pendingSceneIdentifier))
            {
                return false;
            }

            var pending = pendingSceneIdentifier.Trim();
            if (string.Equals(loadedScene.name, pending, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var loadedScenePath = loadedScene.path;
            if (string.IsNullOrWhiteSpace(loadedScenePath))
            {
                return false;
            }

            var normalizedPending = pending.Replace('\\', '/');
            return string.Equals(loadedScenePath, normalizedPending, StringComparison.OrdinalIgnoreCase);
        }
    }
}
