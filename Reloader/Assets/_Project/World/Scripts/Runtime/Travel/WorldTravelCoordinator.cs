using System.Collections.Generic;
using System;
using System.Collections;
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
        private static float _travelSuppressedUntilRealtime;
        private static Dictionary<string, int> _pendingInventoryQuantities = new();
        private static readonly List<WeaponRuntimeSnapshotCapture> _pendingWeaponSnapshots = new();
        private const string FpsArmsPrefabResourcePath = "Viewmodels/Characters/FPS_Arms";
        private const string FpsArmsControllerResourcePath = "Viewmodels/Characters/ViewmodelArms";

        public static string LastResolvedEntryPointId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _pendingSceneName = null;
            _pendingEntryPointId = null;
            LastResolvedEntryPointId = null;
            _isSubscribedToSceneLoaded = false;
            _travelSuppressedUntilRealtime = 0f;
            _pendingInventoryQuantities = new Dictionary<string, int>();
            _pendingWeaponSnapshots.Clear();
        }

        public static bool TryTravel(TravelContext context)
        {
            if (context == null)
            {
                return false;
            }

            if (Time.realtimeSinceStartup < _travelSuppressedUntilRealtime)
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
            CaptureInventorySnapshotForTravel();
            CaptureWeaponRuntimeSnapshotForTravel();
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
            _travelSuppressedUntilRealtime = Time.realtimeSinceStartup + 1f;
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

            var activeScenePlayerRoot = FindPlayerRootInScene(scene);

            if (activeScenePlayerRoot != null)
            {
                ApplyInventorySnapshotAfterTravel(activeScenePlayerRoot);
                ApplyWeaponRuntimeSnapshotAfterTravel(activeScenePlayerRoot);
                activeScenePlayerRoot.position = entryPointTransform.position;
                activeScenePlayerRoot.rotation = entryPointTransform.rotation;
                EnsureViewmodelRigAfterTravel(activeScenePlayerRoot);
            }

            if (PersistentPlayerRoot.Instance != null)
            {
                var persistentRootTransform = PersistentPlayerRoot.Instance.transform;
                persistentRootTransform.position = entryPointTransform.position;
                persistentRootTransform.rotation = entryPointTransform.rotation;
            }
        }

        private static void CaptureInventorySnapshotForTravel()
        {
            _pendingInventoryQuantities.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var playerRoot = FindPlayerRootInScene(activeScene);
            if (playerRoot == null)
            {
                return;
            }

            var inventoryController = playerRoot.GetComponent("PlayerInventoryController");
            if (inventoryController == null)
            {
                return;
            }

            var runtime = GetRuntimeFromInventoryController(inventoryController);
            if (runtime == null)
            {
                return;
            }

            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            CollectItemIdsFromRuntimeCollection(runtime, "BeltSlotItemIds", itemIds);
            CollectItemIdsFromRuntimeCollection(runtime, "BackpackItemIds", itemIds);

            var getItemQuantity = runtime.GetType().GetMethod("GetItemQuantity", BindingFlags.Instance | BindingFlags.Public);
            if (getItemQuantity == null)
            {
                return;
            }

            foreach (var itemId in itemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                var quantity = (int)getItemQuantity.Invoke(runtime, new object[] { itemId });
                if (quantity > 0)
                {
                    _pendingInventoryQuantities[itemId] = quantity;
                }
            }
        }

        private static void ApplyInventorySnapshotAfterTravel(Transform playerRootTransform)
        {
            if (_pendingInventoryQuantities == null || _pendingInventoryQuantities.Count == 0 || playerRootTransform == null)
            {
                return;
            }

            var inventoryController = playerRootTransform.GetComponent("PlayerInventoryController");
            if (inventoryController == null)
            {
                _pendingInventoryQuantities.Clear();
                return;
            }

            var runtime = GetRuntimeFromInventoryController(inventoryController);
            if (runtime == null)
            {
                _pendingInventoryQuantities.Clear();
                return;
            }

            var getItemQuantity = runtime.GetType().GetMethod("GetItemQuantity", BindingFlags.Instance | BindingFlags.Public);
            var tryAddStackItem = runtime.GetType().GetMethod("TryAddStackItem", BindingFlags.Instance | BindingFlags.Public);
            var tryStoreItem = runtime.GetType().GetMethod("TryStoreItem", BindingFlags.Instance | BindingFlags.Public);
            if (getItemQuantity == null || (tryAddStackItem == null && tryStoreItem == null))
            {
                _pendingInventoryQuantities.Clear();
                return;
            }

            foreach (var pair in _pendingInventoryQuantities)
            {
                var itemId = pair.Key;
                var targetQuantity = pair.Value;
                if (string.IsNullOrWhiteSpace(itemId) || targetQuantity <= 0)
                {
                    continue;
                }

                var currentQuantity = (int)getItemQuantity.Invoke(runtime, new object[] { itemId });
                var missingQuantity = targetQuantity - currentQuantity;
                if (missingQuantity <= 0)
                {
                    continue;
                }

                if (tryAddStackItem != null)
                {
                    var addArgs = new object[] { itemId, missingQuantity, null, null, null };
                    var added = (bool)tryAddStackItem.Invoke(runtime, addArgs);
                    if (added)
                    {
                        continue;
                    }
                }

                if (tryStoreItem == null)
                {
                    continue;
                }

                for (var i = 0; i < missingQuantity; i++)
                {
                    var storeArgs = new object[] { itemId, null, null, null };
                    var stored = (bool)tryStoreItem.Invoke(runtime, storeArgs);
                    if (!stored)
                    {
                        break;
                    }
                }
            }

            _pendingInventoryQuantities.Clear();
        }

        private static void CaptureWeaponRuntimeSnapshotForTravel()
        {
            _pendingWeaponSnapshots.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var playerRoot = FindPlayerRootInScene(activeScene);
            if (playerRoot == null)
            {
                return;
            }

            var weaponController = playerRoot.GetComponent("PlayerWeaponController");
            if (weaponController == null)
            {
                return;
            }

            var getSnapshots = weaponController.GetType().GetMethod("GetRuntimeStateSnapshots", BindingFlags.Instance | BindingFlags.Public);
            if (getSnapshots == null)
            {
                return;
            }

            if (getSnapshots.Invoke(weaponController, null) is not IEnumerable snapshots)
            {
                return;
            }

            foreach (var snapshot in snapshots)
            {
                if (snapshot == null)
                {
                    continue;
                }

                var snapshotType = snapshot.GetType();
                var itemId = snapshotType.GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot) as string;
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                var chamberLoaded = (bool?)snapshotType.GetProperty("ChamberLoaded", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot) ?? false;
                var magazineCount = (int?)snapshotType.GetProperty("MagCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot) ?? 0;
                var reserveCount = (int?)snapshotType.GetProperty("ReserveCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot) ?? 0;
                var chamberRound = snapshotType.GetProperty("ChamberRound", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot);
                var magazineRounds = snapshotType.GetProperty("MagazineRounds", BindingFlags.Instance | BindingFlags.Public)?.GetValue(snapshot);
                if (magazineRounds is not IEnumerable)
                {
                    continue;
                }

                _pendingWeaponSnapshots.Add(new WeaponRuntimeSnapshotCapture
                {
                    ItemId = itemId,
                    ChamberLoaded = chamberLoaded,
                    MagazineCount = magazineCount,
                    ReserveCount = reserveCount,
                    ChamberRound = chamberRound,
                    MagazineRounds = magazineRounds
                });
            }
        }

        private static void ApplyWeaponRuntimeSnapshotAfterTravel(Transform playerRootTransform)
        {
            if (_pendingWeaponSnapshots.Count == 0 || playerRootTransform == null)
            {
                return;
            }

            var weaponController = playerRootTransform.GetComponent("PlayerWeaponController");
            if (weaponController == null)
            {
                _pendingWeaponSnapshots.Clear();
                return;
            }

            var applyRuntimeState = weaponController.GetType().GetMethod("ApplyRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            var applyRuntimeBallistics = weaponController.GetType().GetMethod("ApplyRuntimeBallistics", BindingFlags.Instance | BindingFlags.Public);
            if (applyRuntimeState == null || applyRuntimeBallistics == null)
            {
                _pendingWeaponSnapshots.Clear();
                return;
            }

            var ballisticsParameters = applyRuntimeBallistics.GetParameters();
            if (ballisticsParameters.Length != 3)
            {
                _pendingWeaponSnapshots.Clear();
                return;
            }

            var chamberParameterType = ballisticsParameters[1].ParameterType;
            var magazineParameterType = ballisticsParameters[2].ParameterType;
            foreach (var snapshot in _pendingWeaponSnapshots)
            {
                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ItemId) || snapshot.MagazineRounds == null)
                {
                    continue;
                }

                if (!magazineParameterType.IsInstanceOfType(snapshot.MagazineRounds))
                {
                    continue;
                }

                var appliedCounts = (bool)applyRuntimeState.Invoke(
                    weaponController,
                    new object[] { snapshot.ItemId, snapshot.MagazineCount, snapshot.ReserveCount, snapshot.ChamberLoaded });
                if (!appliedCounts)
                {
                    continue;
                }

                var chamberArgument = snapshot.ChamberRound;
                if (chamberArgument == null && snapshot.ChamberLoaded && snapshot.MagazineRounds is IEnumerable rounds)
                {
                    foreach (var round in rounds)
                    {
                        chamberArgument = round;
                        break;
                    }
                }

                if (chamberArgument == null && snapshot.ChamberLoaded)
                {
                    // Keep chamber-loaded state from ApplyRuntimeState when there is no concrete ballistic payload to apply.
                    continue;
                }

                if (chamberParameterType.IsGenericType
                    && chamberParameterType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && chamberArgument != null)
                {
                    chamberArgument = Activator.CreateInstance(chamberParameterType, chamberArgument);
                }

                applyRuntimeBallistics.Invoke(
                    weaponController,
                    new[] { (object)snapshot.ItemId, chamberArgument, snapshot.MagazineRounds });
            }

            _pendingWeaponSnapshots.Clear();
        }

        private static object GetRuntimeFromInventoryController(Component inventoryController)
        {
            if (inventoryController == null)
            {
                return null;
            }

            var runtimeProperty = inventoryController.GetType().GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            return runtimeProperty?.GetValue(inventoryController);
        }

        private static void CollectItemIdsFromRuntimeCollection(object runtime, string propertyName, ISet<string> sink)
        {
            if (runtime == null || sink == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = runtime.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return;
            }

            var enumerable = property.GetValue(runtime) as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return;
            }

            foreach (var entry in enumerable)
            {
                var itemId = entry as string;
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    sink.Add(itemId);
                }
            }
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

        private static void EnsureViewmodelRigAfterTravel(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return;
            }

            var cameraPivot = playerRootTransform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                return;
            }

            var animator = cameraPivot.GetComponentInChildren<Animator>(true);
            var armsController = Resources.Load<RuntimeAnimatorController>(FpsArmsControllerResourcePath);
            if (animator == null)
            {
                var armsPrefab = Resources.Load<GameObject>(FpsArmsPrefabResourcePath);
                if (armsPrefab != null)
                {
                    var instance = UnityEngine.Object.Instantiate(armsPrefab, cameraPivot);
                    instance.name = "PlayerArms";
                    instance.transform.localPosition = new Vector3(0f, -0.24f, 1.56f);
                    instance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    instance.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
                    animator = instance.GetComponentInChildren<Animator>(true);
                }
            }

            if (animator == null)
            {
                return;
            }

            if (animator.runtimeAnimatorController == null && armsController != null)
            {
                animator.runtimeAnimatorController = armsController;
            }

            var viewmodelAdapter = playerRootTransform.GetComponent("ViewmodelAnimationAdapter");
            SetComponentAnimatorField(viewmodelAdapter, animator);

            var fpsDriver = playerRootTransform.GetComponent("FpsViewmodelAnimatorDriver");
            SetComponentAnimatorField(fpsDriver, animator);
        }

        private static void SetComponentAnimatorField(Component component, Animator animator)
        {
            if (component == null || animator == null)
            {
                return;
            }

            var field = component.GetType().GetField("_animator", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            var current = field.GetValue(component) as Animator;
            if (current == animator)
            {
                return;
            }

            field.SetValue(component, animator);
        }

        private sealed class WeaponRuntimeSnapshotCapture
        {
            public string ItemId;
            public bool ChamberLoaded;
            public int MagazineCount;
            public int ReserveCount;
            public object ChamberRound;
            public object MagazineRounds;
        }
    }
}
