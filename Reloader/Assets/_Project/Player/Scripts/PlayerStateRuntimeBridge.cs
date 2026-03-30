using System;
using System.Collections.Generic;
using System.Reflection;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.Core.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStateRuntimeBridge : MonoBehaviour, ISaveRuntimeBridge, IPlayerRecoveryService
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string ArrestRecoveryReasonId = "arrest";
        private const string DeathRecoveryReasonId = "death";
        private const string PoliceRecoveryAnchorId = "entry.maintown.respawn.police";
        private const string HospitalRecoveryAnchorId = "entry.maintown.respawn.hospital";
        private const string HumanoidDamageReceiverTypeName = "Reloader.NPCs.Combat.HumanoidDamageReceiver, Reloader.NPCs";

        private static bool _sceneHookRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            if (_sceneHookRegistered)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            _sceneHookRegistered = false;
        }

        private PlayerStateModule _playerStateModule;
        private Transform _playerRootTransform;
        private object _inventoryRuntime;
        private bool _hasExplicitPlayerRootTransform;
        private string _currentScenePath = string.Empty;
        private string _currentAnchorId = string.Empty;
        private string _recoveryReasonId = string.Empty;
        private string _recoveryScenePath = string.Empty;
        private string _recoveryAnchorId = string.Empty;
        private IPlayerRecoveryTravelCoordinator _recoveryTravelCoordinator = WorldPlayerRecoveryTravelCoordinator.Instance;

        public string CurrentScenePath => _currentScenePath;
        public string CurrentAnchorId => _currentAnchorId;
        public string RecoveryReasonId => _recoveryReasonId;
        public string RecoveryScenePath => _recoveryScenePath;
        public string RecoveryAnchorId => _recoveryAnchorId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallRuntimeBridge()
        {
            EnsureSceneHookRegistered();
            EnsureAttachedToRuntimePlayerRoot();
        }

        private void Awake()
        {
            if (_playerRootTransform == null)
            {
                _playerRootTransform = transform;
            }
        }

        private void OnEnable()
        {
            SaveRuntimeBridgeRegistry.Register(this);
        }

        private void OnDisable()
        {
            SaveRuntimeBridgeRegistry.Unregister(this);
        }

        public void SetPlayerStateModuleForRuntime(PlayerStateModule playerStateModule)
        {
            _playerStateModule = playerStateModule;
        }

        public void SetPlayerRootTransformForRuntime(Transform playerRootTransform)
        {
            _playerRootTransform = playerRootTransform != null ? playerRootTransform : transform;
            _hasExplicitPlayerRootTransform = playerRootTransform != null;
        }

        public void SetInventoryRuntimeForRuntime(object inventoryRuntime)
        {
            _inventoryRuntime = inventoryRuntime;
        }

        public void SetCurrentAnchorState(string scenePath, string anchorId)
        {
            _currentScenePath = Normalize(scenePath);
            _currentAnchorId = Normalize(anchorId);
        }

        public void SetRecoveryState(string recoveryReasonId, string recoveryScenePath, string recoveryAnchorId)
        {
            _recoveryReasonId = Normalize(recoveryReasonId);
            _recoveryScenePath = Normalize(recoveryScenePath);
            _recoveryAnchorId = Normalize(recoveryAnchorId);
        }

        public void SetRecoveryTravelCoordinatorForRuntime(IPlayerRecoveryTravelCoordinator recoveryTravelCoordinator)
        {
            _recoveryTravelCoordinator = recoveryTravelCoordinator ?? WorldPlayerRecoveryTravelCoordinator.Instance;
        }

        public bool TryApplyArrestRecovery()
        {
            return TryApplyRecovery(ArrestRecoveryReasonId, PoliceRecoveryAnchorId);
        }

        public bool TryApplyDeathRecovery()
        {
            return TryApplyRecovery(DeathRecoveryReasonId, HospitalRecoveryAnchorId);
        }

        public void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            SetPlayerStateModuleForRuntime(ResolvePlayerStateModule(moduleRegistrations));
            CaptureToModule();
        }

        public void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            SetPlayerStateModuleForRuntime(ResolvePlayerStateModule(moduleRegistrations));
            RestoreFromModule();
        }

        public void CaptureToModule()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            var scenePath = _hasExplicitPlayerRootTransform ? ResolveLiveScenePath(_playerRootTransform) : string.Empty;
            var hasLiveScenePath = !string.IsNullOrWhiteSpace(scenePath);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                scenePath = Normalize(_currentScenePath);
            }

            var anchorId = hasLiveScenePath ? ResolveLastResolvedEntryPointId() : string.Empty;
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = Normalize(_currentAnchorId);
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                throw new InvalidOperationException("PlayerState CurrentAnchorId could not be resolved for save capture.");
            }

            _currentScenePath = scenePath;
            _currentAnchorId = anchorId;
            _playerStateModule.CurrentScenePath = scenePath;
            _playerStateModule.CurrentAnchorId = anchorId;
            _playerStateModule.PositionX = _playerRootTransform.position.x;
            _playerStateModule.PositionY = _playerRootTransform.position.y;
            _playerStateModule.PositionZ = _playerRootTransform.position.z;
            _playerStateModule.RotationX = _playerRootTransform.rotation.x;
            _playerStateModule.RotationY = _playerRootTransform.rotation.y;
            _playerStateModule.RotationZ = _playerRootTransform.rotation.z;
            _playerStateModule.RotationW = _playerRootTransform.rotation.w;
            _playerStateModule.SelectedBeltSlotIndex = ResolveSelectedBeltSlotIndex();
            _playerStateModule.RecoveryReasonId = Normalize(_recoveryReasonId);
            _playerStateModule.RecoveryScenePath = Normalize(_recoveryScenePath);
            _playerStateModule.RecoveryAnchorId = Normalize(_recoveryAnchorId);
            CaptureSharedHumanoidHealth();
        }

        public void RestoreFromModule()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            _playerRootTransform.SetPositionAndRotation(
                new Vector3(_playerStateModule.PositionX, _playerStateModule.PositionY, _playerStateModule.PositionZ),
                new Quaternion(_playerStateModule.RotationX, _playerStateModule.RotationY, _playerStateModule.RotationZ, _playerStateModule.RotationW));

            _currentScenePath = Normalize(_playerStateModule.CurrentScenePath);
            _currentAnchorId = Normalize(_playerStateModule.CurrentAnchorId);
            _recoveryReasonId = Normalize(_playerStateModule.RecoveryReasonId);
            _recoveryScenePath = Normalize(_playerStateModule.RecoveryScenePath);
            _recoveryAnchorId = Normalize(_playerStateModule.RecoveryAnchorId);

            ApplySelectedBeltSlot(_playerStateModule.SelectedBeltSlotIndex);
            RestoreSharedHumanoidHealth();
        }

        private static void EnsureSceneHookRegistered()
        {
            if (_sceneHookRegistered)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneHookRegistered = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAttachedToRuntimePlayerRoot();
        }

        private static PlayerStateRuntimeBridge EnsureAttachedToRuntimePlayerRoot()
        {
            var playerRootTransform = ResolveRuntimePlayerRootTransform();
            if (playerRootTransform == null)
            {
                return null;
            }

            var bridge = playerRootTransform.GetComponent<PlayerStateRuntimeBridge>();
            if (bridge == null)
            {
                bridge = playerRootTransform.gameObject.AddComponent<PlayerStateRuntimeBridge>();
            }

            bridge.SetPlayerRootTransformForRuntime(playerRootTransform);
            return bridge;
        }

        private bool ResolveDependencies()
        {
            if (_playerStateModule == null)
            {
                return false;
            }

            if (_playerRootTransform == null)
            {
                _playerRootTransform = transform;
            }

            if (_playerRootTransform == null)
            {
                _playerRootTransform = ResolveRuntimePlayerRootTransform();
            }

            if (_inventoryRuntime == null)
            {
                _inventoryRuntime = ResolveInventoryRuntimeFromPlayerRoot(_playerRootTransform);
            }

            return _playerRootTransform != null;
        }

        private int ResolveSelectedBeltSlotIndex()
        {
            var inventoryRuntime = _inventoryRuntime ?? ResolveInventoryRuntimeFromPlayerRoot(_playerRootTransform);
            if (inventoryRuntime == null)
            {
                return -1;
            }

            var property = inventoryRuntime.GetType().GetProperty("SelectedBeltIndex", BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(inventoryRuntime) is int selectedBeltIndex)
            {
                return selectedBeltIndex;
            }

            return -1;
        }

        private void ApplySelectedBeltSlot(int selectedBeltSlotIndex)
        {
            var inventoryRuntime = _inventoryRuntime ?? ResolveInventoryRuntimeFromPlayerRoot(_playerRootTransform);
            if (inventoryRuntime == null)
            {
                return;
            }

            if (selectedBeltSlotIndex < 0)
            {
                var clearMethod = inventoryRuntime.GetType().GetMethod("ClearSelectedBeltSlot", BindingFlags.Instance | BindingFlags.Public);
                clearMethod?.Invoke(inventoryRuntime, null);
                return;
            }

            var method = inventoryRuntime.GetType().GetMethod("SelectBeltSlot", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(inventoryRuntime, new object[] { selectedBeltSlotIndex });
        }

        private static PlayerStateModule ResolvePlayerStateModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i]?.Module is PlayerStateModule module)
                {
                    return module;
                }
            }

            return null;
        }

        private static Transform ResolveRuntimePlayerRootTransform()
        {
            var persistentPlayerRootType = Type.GetType("Reloader.World.Runtime.PersistentPlayerRoot, Reloader.World");
            if (persistentPlayerRootType == null)
            {
                return null;
            }

            var instance = persistentPlayerRootType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (instance == null)
            {
                return null;
            }

            return persistentPlayerRootType.GetProperty("PlayerRootTransform", BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance) as Transform;
        }

        private static object ResolveInventoryRuntimeFromPlayerRoot(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return null;
            }

            var components = playerRootTransform.GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || component.GetType().Name != "PlayerInventoryController")
                {
                    continue;
                }

                var runtimeProperty = component.GetType().GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
                var runtime = runtimeProperty?.GetValue(component);
                if (runtime != null)
                {
                    return runtime;
                }
            }

            return null;
        }

        private static string ResolveLiveScenePath(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return string.Empty;
            }

            var scene = playerRootTransform.gameObject.scene;
            if (!scene.IsValid())
            {
                return string.Empty;
            }

            return Normalize(scene.path);
        }

        private static string ResolveLastResolvedEntryPointId()
        {
            var coordinatorType = Type.GetType("Reloader.World.Travel.WorldTravelCoordinator, Reloader.World");
            if (coordinatorType == null)
            {
                return string.Empty;
            }

            return Normalize(coordinatorType.GetProperty("LastResolvedEntryPointId", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as string);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private bool TryApplyRecovery(string recoveryReasonId, string recoveryAnchorId)
        {
            if (_playerRootTransform == null)
            {
                _playerRootTransform = transform;
            }

            if (_playerRootTransform == null)
            {
                _playerRootTransform = ResolveRuntimePlayerRootTransform();
            }

            if (_inventoryRuntime == null)
            {
                _inventoryRuntime = ResolveInventoryRuntimeFromPlayerRoot(_playerRootTransform);
            }

            ClearCarriedInventory();
            SetRecoveryState(recoveryReasonId, MainTownScenePath, recoveryAnchorId);

            var currentScenePath = ResolveLiveScenePath(_playerRootTransform);
            if (string.IsNullOrWhiteSpace(currentScenePath))
            {
                currentScenePath = Normalize(_currentScenePath);
            }

            var recoveryApplied = false;
            if (string.Equals(currentScenePath, MainTownScenePath, StringComparison.Ordinal))
            {
                recoveryApplied = _recoveryTravelCoordinator.TryMoveRuntimePlayerToLoadedEntryPoint(MainTownScenePath, recoveryAnchorId);
            }

            if (!recoveryApplied)
            {
                var sceneName = WorldPlayerRecoveryTravelCoordinator.GetSceneNameFromPath(MainTownScenePath);
                recoveryApplied = _recoveryTravelCoordinator.TryTravelToSceneEntry(sceneName, recoveryAnchorId);
            }

            if (recoveryApplied)
            {
                ResetSharedHumanoidHealth();
                SetCurrentAnchorState(MainTownScenePath, recoveryAnchorId);
            }

            return recoveryApplied;
        }

        private void ClearCarriedInventory()
        {
            var inventoryRuntime = _inventoryRuntime ?? ResolveInventoryRuntimeFromPlayerRoot(_playerRootTransform);
            if (inventoryRuntime == null)
            {
                return;
            }

            var clearMethod = inventoryRuntime.GetType().GetMethod("ClearCarriedItems", BindingFlags.Instance | BindingFlags.Public);
            clearMethod?.Invoke(inventoryRuntime, null);
        }

        private void CaptureSharedHumanoidHealth()
        {
            var sharedReceiver = ResolveSharedHumanoidReceiver(_playerRootTransform);
            if (sharedReceiver == null)
            {
                _playerStateModule.CurrentHealth = 0f;
                _playerStateModule.MaxHealth = 0f;
                return;
            }

            _playerStateModule.CurrentHealth = ReadFloatProperty(sharedReceiver, "CurrentHealth");
            _playerStateModule.MaxHealth = ReadFloatProperty(sharedReceiver, "MaxHealth");
        }

        private void RestoreSharedHumanoidHealth()
        {
            if (_playerStateModule.MaxHealth <= 0f)
            {
                return;
            }

            var sharedReceiver = ResolveSharedHumanoidReceiver(_playerRootTransform);
            if (sharedReceiver == null)
            {
                return;
            }

            SetSharedHumanoidHealthState(sharedReceiver, _playerStateModule.CurrentHealth, _playerStateModule.MaxHealth);
        }

        private void ResetSharedHumanoidHealth()
        {
            var sharedReceiver = ResolveSharedHumanoidReceiver(_playerRootTransform);
            if (sharedReceiver == null)
            {
                return;
            }

            var resetMethod = sharedReceiver.GetType().GetMethod("ResetRuntime", BindingFlags.Instance | BindingFlags.Public);
            resetMethod?.Invoke(sharedReceiver, null);
        }

        private static object ResolveSharedHumanoidReceiver(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return null;
            }

            var sharedReceiverType = Type.GetType(HumanoidDamageReceiverTypeName, throwOnError: false);
            if (sharedReceiverType == null)
            {
                return null;
            }

            return playerRootTransform.GetComponent(sharedReceiverType);
        }

        private static float ReadFloatProperty(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(float))
            {
                return 0f;
            }

            var value = property.GetValue(instance);
            return value is float floatValue ? floatValue : 0f;
        }

        private static void SetSharedHumanoidHealthState(object sharedReceiver, float currentHealth, float maxHealth)
        {
            var method = sharedReceiver.GetType().GetMethod(
                "SetHealthStateForRuntime",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(float), typeof(float) },
                modifiers: null);
            method?.Invoke(sharedReceiver, new object[] { currentHealth, maxHealth });
        }
    }
}
