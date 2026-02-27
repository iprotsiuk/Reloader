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
            PreparePersistentPlayerRootForTravel();
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

            var activeScenePlayerRoot = ResolveTravelPlayerRoot(scene);

            if (activeScenePlayerRoot != null)
            {
                ResetRuntimeUiStateAfterTravel();
                ApplyInventorySnapshotAfterTravel(activeScenePlayerRoot);
                ApplyWeaponRuntimeSnapshotAfterTravel(activeScenePlayerRoot);
                activeScenePlayerRoot.position = entryPointTransform.position;
                activeScenePlayerRoot.rotation = entryPointTransform.rotation;
                EnsureViewmodelRigAfterTravel(activeScenePlayerRoot);
                RestorePlayerControlsAfterTravel(activeScenePlayerRoot);
                HideOwnedWeaponPickupsInScene(scene, activeScenePlayerRoot);
            }

        }

        private static void PreparePersistentPlayerRootForTravel()
        {
            var persistentRoot = PersistentPlayerRoot.EnsureInstance();
            if (persistentRoot == null)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            persistentRoot.CaptureOrAdoptPlayerRootForScene(activeScene, preferSceneRoot: true);
        }

        private static Transform ResolveTravelPlayerRoot(Scene destinationScene)
        {
            if (destinationScene.IsValid() && destinationScene.isLoaded && PersistentPlayerRoot.Instance != null)
            {
                var adopted = PersistentPlayerRoot.Instance.CaptureOrAdoptPlayerRootForScene(destinationScene, preferSceneRoot: false);
                if (adopted != null)
                {
                    return adopted;
                }
            }

            return FindPlayerRootInScene(destinationScene);
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

            var playerArms = cameraPivot.Find("PlayerArms");
            var animator = playerArms != null ? playerArms.GetComponentInChildren<Animator>(true) : null;
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

            RebindPlayerRigRuntimeReferences(playerRootTransform);
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

        private static void RebindPlayerRigRuntimeReferences(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return;
            }

            var components = playerRootTransform.GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    continue;
                }

                var type = component.GetType();
                if (type.Name == "PlayerInputReader")
                {
                    var resolveActions = type.GetMethod("ResolveActions", BindingFlags.Instance | BindingFlags.NonPublic);
                    resolveActions?.Invoke(component, null);
                }

                var resolveReferences = type.GetMethod("ResolveReferences", BindingFlags.Instance | BindingFlags.NonPublic);
                resolveReferences?.Invoke(component, null);
            }
        }

        private static void RestorePlayerControlsAfterTravel(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return;
            }

            var playerInputReader = playerRootTransform.GetComponent("PlayerInputReader");
            var characterController = playerRootTransform.GetComponent<CharacterController>();
            var cameraPivot = playerRootTransform.Find("CameraPivot");
            var inventoryController = playerRootTransform.GetComponent("PlayerInventoryController");

            var components = playerRootTransform.GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component is Behaviour behaviour && !behaviour.enabled)
                {
                    behaviour.enabled = true;
                }

                var type = component.GetType();
                var resolveReferences = type.GetMethod("ResolveReferences", BindingFlags.Instance | BindingFlags.NonPublic);
                resolveReferences?.Invoke(component, null);

                if (type.Name == "PlayerInputReader")
                {
                    var resolveActions = type.GetMethod("ResolveActions", BindingFlags.Instance | BindingFlags.NonPublic);
                    resolveActions?.Invoke(component, null);

                    var playerMapField = type.GetField("_playerMap", BindingFlags.Instance | BindingFlags.NonPublic);
                    var playerMap = playerMapField?.GetValue(component);
                    playerMap?.GetType().GetMethod("Enable", BindingFlags.Instance | BindingFlags.Public)?.Invoke(playerMap, null);
                }
                else if (type.Name == "PlayerMover")
                {
                    type.GetMethod("SetInputSource", BindingFlags.Instance | BindingFlags.Public)
                        ?.Invoke(component, new[] { playerInputReader });
                    type.GetMethod("SetCharacterController", BindingFlags.Instance | BindingFlags.Public)
                        ?.Invoke(component, new object[] { characterController });
                }
                else if (type.Name == "PlayerLookController")
                {
                    type.GetMethod("SetInputSource", BindingFlags.Instance | BindingFlags.Public)
                        ?.Invoke(component, new[] { playerInputReader });
                    type.GetMethod("SetPitchTransform", BindingFlags.Instance | BindingFlags.Public)
                        ?.Invoke(component, new object[] { cameraPivot });
                }
                else if (type.Name == "PlayerInventoryController")
                {
                    type.GetField("_inputSource", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, null);
                    type.GetField("_inputSourceBehaviour", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, playerInputReader);
                    resolveReferences?.Invoke(component, null);
                }
                else if (type.Name == "PlayerWeaponController")
                {
                    type.GetField("_inputSource", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, null);
                    type.GetField("_attemptedSceneInputResolution", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, false);
                    type.GetField("_inputSourceBehaviour", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, playerInputReader);
                    if (inventoryController != null)
                    {
                        type.GetField("_inventoryController", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.SetValue(component, inventoryController);
                    }

                    resolveReferences?.Invoke(component, null);
                }
                else if (type.Name == "PlayerCursorLockController")
                {
                    type.GetField("_isTradeMenuOpen", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, false);
                    type.GetField("_isWorkbenchMenuOpen", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, false);
                    type.GetField("_isTabInventoryOpen", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, false);
                    type.GetMethod("ApplyCursorState", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);
                    type.GetMethod("LockCursor", BindingFlags.Instance | BindingFlags.Public)?.Invoke(component, null);
                }
            }
        }

        private static void HideOwnedWeaponPickupsInScene(Scene scene, Transform playerRootTransform)
        {
            if (!scene.IsValid() || !scene.isLoaded || playerRootTransform == null)
            {
                return;
            }

            var inventoryController = playerRootTransform.GetComponent("PlayerInventoryController");
            var runtime = GetRuntimeFromInventoryController(inventoryController);
            if (runtime == null)
            {
                return;
            }

            var ownedItemIds = new HashSet<string>(StringComparer.Ordinal);
            CollectItemIdsFromRuntimeCollection(runtime, "BeltSlotItemIds", ownedItemIds);
            CollectItemIdsFromRuntimeCollection(runtime, "BackpackItemIds", ownedItemIds);
            if (ownedItemIds.Count == 0)
            {
                return;
            }

            var pickupComponents = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < pickupComponents.Length; i++)
            {
                var pickup = pickupComponents[i];
                if (pickup == null || pickup.gameObject.scene != scene)
                {
                    continue;
                }

                var pickupType = pickup.GetType();
                if (pickupType.Name != "WeaponPickupTarget")
                {
                    continue;
                }

                var itemIdProperty = pickupType.GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public);
                var itemId = itemIdProperty?.GetValue(pickup) as string;
                if (string.IsNullOrWhiteSpace(itemId) || !ownedItemIds.Contains(itemId))
                {
                    continue;
                }

                pickupType.GetMethod("OnPickedUp", BindingFlags.Instance | BindingFlags.Public)?.Invoke(pickup, null);
            }
        }

        private static void ResetRuntimeUiStateAfterTravel()
        {
            var runtimeKernelType = ResolveRuntimeKernelBootstrapperType();
            if (runtimeKernelType == null)
            {
                return;
            }

            var shopEvents = runtimeKernelType.GetProperty("ShopEvents", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (shopEvents != null)
            {
                shopEvents.GetType().GetMethod("RaiseShopTradeClosed", BindingFlags.Public | BindingFlags.Instance)?.Invoke(shopEvents, null);
            }

            var uiStateEvents = runtimeKernelType.GetProperty("UiStateEvents", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (uiStateEvents != null)
            {
                var uiType = uiStateEvents.GetType();
                uiType.GetMethod("RaiseWorkbenchMenuVisibilityChanged", BindingFlags.Public | BindingFlags.Instance)
                    ?.Invoke(uiStateEvents, new object[] { false });
                uiType.GetMethod("RaiseTabInventoryVisibilityChanged", BindingFlags.Public | BindingFlags.Instance)
                    ?.Invoke(uiStateEvents, new object[] { false });
            }
        }

        private static Type ResolveRuntimeKernelBootstrapperType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType("Reloader.Core.Runtime.RuntimeKernelBootstrapper", throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
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
