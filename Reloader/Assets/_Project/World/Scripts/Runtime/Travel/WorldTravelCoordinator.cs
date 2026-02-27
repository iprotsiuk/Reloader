using System.Collections.Generic;
using System;
using System.Reflection;
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

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"Travel scene '{sceneName}' is not available in build settings.");
                return false;
            }

            EnsureSubscribed();
            EnsureTravelPlayerRootCaptured();
            _pendingSceneName = sceneName.Trim();
            _pendingEntryPointId = entryPointId.Trim();
            LastResolvedEntryPointId = null;
            try
            {
                SceneManager.LoadScene(_pendingSceneName, LoadSceneMode.Single);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load travel scene '{_pendingSceneName}': {ex.Message}");
                _pendingSceneName = null;
                _pendingEntryPointId = null;
                return false;
            }
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

            if (!IsMatchingPendingScene(scene, _pendingSceneName))
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
                RepositionPlayerToEntryPoint(scene, resolvedEntryPoint.transform);
            }
            else
            {
                Debug.LogWarning($"Travel entry point '{_pendingEntryPointId}' was not found in scene '{scene.name}'.");
            }

            _pendingSceneName = null;
            _pendingEntryPointId = null;
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

        private static void RepositionPlayerToEntryPoint(Scene scene, Transform entryPointTransform)
        {
            if (entryPointTransform == null)
            {
                return;
            }

            var activeScenePlayerRoot = ResolveTravelPlayerRoot(scene);
            if (activeScenePlayerRoot == null)
            {
                activeScenePlayerRoot = FindPlayerRootInScene(scene);
            }

            if (activeScenePlayerRoot != null)
            {
                activeScenePlayerRoot.position = entryPointTransform.position;
                activeScenePlayerRoot.rotation = entryPointTransform.rotation;
            }

            if (PersistentPlayerRoot.Instance != null)
            {
                var persistentRootTransform = PersistentPlayerRoot.Instance.transform;
                persistentRootTransform.position = entryPointTransform.position;
                persistentRootTransform.rotation = entryPointTransform.rotation;
            }
        }

        private static void EnsureTravelPlayerRootCaptured()
        {
            var persistentRoot = PersistentPlayerRoot.EnsureInstance();
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            var capturedPlayerRoot = persistentRoot.CaptureOrAdoptPlayerRootForScene(activeScene, preferSceneRoot: true);
            SetTravelSensitiveControllersEnabled(capturedPlayerRoot, false);
        }

        private static Transform ResolveTravelPlayerRoot(Scene destinationScene)
        {
            var persistentRoot = PersistentPlayerRoot.EnsureInstance();
            var playerRoot = persistentRoot.CaptureOrAdoptPlayerRootForScene(destinationScene);
            RebindTravelPlayerRootDependencies(playerRoot);
            SetTravelSensitiveControllersEnabled(playerRoot, true);
            return playerRoot;
        }

        private static void SetTravelSensitiveControllersEnabled(Transform playerRootTransform, bool isEnabled)
        {
            if (playerRootTransform == null)
            {
                return;
            }

            var playerWeaponController = playerRootTransform.GetComponent("PlayerWeaponController") as Behaviour;
            if (playerWeaponController != null)
            {
                playerWeaponController.enabled = isEnabled;
            }
        }

        private static void RebindTravelPlayerRootDependencies(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return;
            }

            InvokePrivateMethodIfPresent(playerRootTransform.GetComponent("PlayerInventoryController"), "ResolveReferences");
            var playerWeaponController = playerRootTransform.GetComponent("PlayerWeaponController");
            InvokePrivateMethodIfPresent(playerWeaponController, "ResolveReferences");
            RebindPlayerWeaponRegistry(playerWeaponController);
        }

        private static void RebindPlayerWeaponRegistry(Component playerWeaponController)
        {
            if (playerWeaponController == null)
            {
                return;
            }

            var weaponControllerType = playerWeaponController.GetType();
            var weaponRegistryField = weaponControllerType.GetField("_weaponRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            if (weaponRegistryField == null)
            {
                return;
            }

            var currentValue = weaponRegistryField.GetValue(playerWeaponController) as UnityEngine.Object;
            if (currentValue != null)
            {
                return;
            }

            var weaponRegistryType = Type.GetType("Reloader.Weapons.Runtime.WeaponRegistry, Reloader.Weapons");
            if (weaponRegistryType == null)
            {
                return;
            }

            var registry = UnityEngine.Object.FindFirstObjectByType(weaponRegistryType);
            if (registry == null)
            {
                return;
            }

            weaponRegistryField.SetValue(playerWeaponController, registry);
        }

        private static void InvokePrivateMethodIfPresent(Component component, string methodName)
        {
            if (component == null || string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(component, null);
        }

        private static Transform FindPlayerRootInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var rootObjects = scene.GetRootGameObjects();
            for (var i = 0; i < rootObjects.Length; i++)
            {
                var root = rootObjects[i];
                if (root != null && root.name == "PlayerRoot")
                {
                    return root.transform;
                }
            }

            return null;
        }
    }
}
