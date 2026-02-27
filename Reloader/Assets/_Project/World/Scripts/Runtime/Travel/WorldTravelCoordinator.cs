using System.Collections.Generic;
using System;
using Reloader.World.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Travel
{
    public static class WorldTravelCoordinator
    {
        private static string _pendingSceneName;
        private static string _pendingEntryPointId;
        private static bool _isSubscribedToSceneLoaded;

        public static string LastResolvedEntryPointId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _pendingSceneName = null;
            _pendingEntryPointId = null;
            LastResolvedEntryPointId = null;
            _isSubscribedToSceneLoaded = false;
        }

        public static bool TryTravel(TravelContext context)
        {
            if (context == null)
            {
                return false;
            }

            try
            {
                context.Validate();
            }
            catch (ArgumentException ex)
            {
                Debug.LogWarning($"Travel context validation failed: {ex.Message}");
                return false;
            }

            return TryLoadSceneAtEntry(context.DestinationSceneName, context.DestinationEntryPointId);
        }

        public static bool TryLoadSceneAtEntry(string sceneName, string entryPointId)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(entryPointId))
            {
                return false;
            }

            EnsureSubscribed();
            _pendingSceneName = sceneName.Trim();
            _pendingEntryPointId = entryPointId.Trim();
            LastResolvedEntryPointId = null;
            SceneManager.LoadScene(_pendingSceneName, LoadSceneMode.Single);
            return true;
        }

        private static void EnsureSubscribed()
        {
            if (_isSubscribedToSceneLoaded)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _isSubscribedToSceneLoaded = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrWhiteSpace(_pendingSceneName) || string.IsNullOrWhiteSpace(_pendingEntryPointId))
            {
                return;
            }

            if (scene.name != _pendingSceneName)
            {
                return;
            }

            var candidates = new List<SceneEntryPoint>();
            var allEntryPoints = UnityEngine.Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < allEntryPoints.Length; i++)
            {
                var entryPoint = allEntryPoints[i];
                if (entryPoint != null && entryPoint.gameObject.scene == scene)
                {
                    candidates.Add(entryPoint);
                }
            }

            if (SceneEntryPoint.TryFindById(candidates, _pendingEntryPointId, out var resolvedEntryPoint))
            {
                LastResolvedEntryPointId = resolvedEntryPoint.EntryPointId;
                if (PersistentPlayerRoot.Instance != null)
                {
                    var rootTransform = PersistentPlayerRoot.Instance.transform;
                    rootTransform.position = resolvedEntryPoint.transform.position;
                    rootTransform.rotation = resolvedEntryPoint.transform.rotation;
                }
            }
            else
            {
                Debug.LogWarning($"Travel entry point '{_pendingEntryPointId}' was not found in scene '{scene.name}'.");
            }

            _pendingSceneName = null;
            _pendingEntryPointId = null;
        }
    }
}
