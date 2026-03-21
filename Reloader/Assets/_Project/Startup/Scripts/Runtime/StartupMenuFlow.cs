using System;
using System.IO;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.World.Travel;
using UnityEngine;

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

    public sealed class StartupMenuFlow : IStartupMenuFlow
    {
        private const string NewGameSceneName = "MainTown";
        private const string NewGameEntryPointId = "entry.maintown.spawn";
        private const string SaveDirectoryName = "Saves";
        private const string PlayerStateModuleKey = "PlayerState";

        private readonly SaveFileRepository _fileRepository;
        private readonly IStartupMenuSceneTravel _sceneTravel;
        private readonly IStartupSaveLoader _saveLoader;
        private readonly string _saveDirectoryPath;

        public StartupMenuFlow()
            : this(
                new SaveFileRepository(),
                new UnityStartupMenuSceneTravel(),
                new SaveCoordinatorSaveLoader(SaveBootstrapper.CreateDefaultCoordinator()),
                GetDefaultSaveDirectoryPath())
        {
        }

        public StartupMenuFlow(
            SaveFileRepository fileRepository,
            IStartupMenuSceneTravel sceneTravel,
            IStartupSaveLoader saveLoader,
            string saveDirectoryPath)
        {
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _sceneTravel = sceneTravel ?? throw new ArgumentNullException(nameof(sceneTravel));
            _saveLoader = saveLoader ?? throw new ArgumentNullException(nameof(saveLoader));
            _saveDirectoryPath = string.IsNullOrWhiteSpace(saveDirectoryPath)
                ? throw new ArgumentException("Save directory path is required.", nameof(saveDirectoryPath))
                : saveDirectoryPath;
        }

        public StartupMenuState RefreshState()
        {
            if (!_fileRepository.TryFindLatestSavePath(_saveDirectoryPath, out var latestSavePath))
            {
                return StartupMenuState.Empty;
            }

            try
            {
                var envelope = _fileRepository.ReadEnvelope(latestSavePath);
                return BuildStateFromEnvelope(latestSavePath, envelope);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Startup menu failed to read latest save '{latestSavePath}': {ex.Message}");
                return StartupMenuState.Empty;
            }
        }

        public bool TryStartNewGame()
        {
            return _sceneTravel.TryLoadSceneAtEntry(NewGameSceneName, NewGameEntryPointId);
        }

        public bool TryContinueLatest()
        {
            var state = RefreshState();
            if (!state.CanContinue)
            {
                return false;
            }

            if (!_sceneTravel.TryLoadSceneAtEntry(state.CurrentSceneName, state.CurrentAnchorId))
            {
                return false;
            }

            _saveLoader.Load(state.LatestSavePath);
            return true;
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
}
