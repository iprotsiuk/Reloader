using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using Reloader.Inventory;
using Reloader.Player;
using GameAdsStateController = Reloader.Game.Weapons.AdsStateController;
using GameAdsVisualMode = Reloader.Game.Weapons.AdsVisualMode;
using GameAttachmentManager = Reloader.Game.Weapons.AttachmentManager;
using GameDetachableMagazineRuntime = Reloader.Game.Weapons.DetachableMagazineRuntime;
using GameMuzzleAttachmentDefinition = Reloader.Game.Weapons.MuzzleAttachmentDefinition;
using GameMuzzleAttachmentRuntime = Reloader.Game.Weapons.MuzzleAttachmentRuntime;
using GameOpticDefinition = Reloader.Game.Weapons.OpticDefinition;
using GamePeripheralScopeEffects = Reloader.Game.Weapons.PeripheralScopeEffects;
using GameRenderTextureScopeController = Reloader.Game.Weapons.RenderTextureScopeController;
using GameScopeAdjustmentTooltipOverlay = Reloader.Game.Weapons.ScopeAdjustmentTooltipOverlay;
using GameWeaponDefinition = Reloader.Game.Weapons.WeaponDefinition;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Cinematics;
using Reloader.Weapons.Data;
using Reloader.Weapons.PackRuntime;
using Reloader.Weapons.Runtime;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using URandom = UnityEngine.Random;
using UObject = UnityEngine.Object;
namespace Reloader.Weapons.Controllers
{
    [System.Serializable]
    public struct WeaponViewPrefabBinding
    {
        [SerializeField] private string _itemId;
        [SerializeField] private GameObject _viewPrefab;

        public string ItemId => _itemId;
        public GameObject ViewPrefab => _viewPrefab;
    }

    public readonly struct WeaponRuntimeSnapshot
    {
        public WeaponRuntimeSnapshot(
            string itemId,
            bool chamberLoaded,
            int magCapacity,
            int magCount,
            int reserveCount,
            AmmoBallisticSnapshot? chamberRound,
            IReadOnlyList<AmmoBallisticSnapshot> magazineRounds,
            IReadOnlyDictionary<WeaponAttachmentSlotType, string> equippedAttachmentItemIdsBySlot)
        {
            ItemId = itemId;
            ChamberLoaded = chamberLoaded;
            MagCapacity = magCapacity;
            MagCount = magCount;
            ReserveCount = reserveCount;
            ChamberRound = chamberRound;
            MagazineRounds = magazineRounds;
            EquippedAttachmentItemIdsBySlot = equippedAttachmentItemIdsBySlot;
        }

        public string ItemId { get; }
        public bool ChamberLoaded { get; }
        public int MagCapacity { get; }
        public int MagCount { get; }
        public int ReserveCount { get; }
        public AmmoBallisticSnapshot? ChamberRound { get; }
        public IReadOnlyList<AmmoBallisticSnapshot> MagazineRounds { get; }
        public IReadOnlyDictionary<WeaponAttachmentSlotType, string> EquippedAttachmentItemIdsBySlot { get; }
    }

    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private readonly struct RendererVisibilityState
        {
            public RendererVisibilityState(Renderer renderer, bool wasEnabled)
            {
                Renderer = renderer;
                WasEnabled = wasEnabled;
            }

            public Renderer Renderer { get; }
            public bool WasEnabled { get; }
        }

        private readonly struct CameraEnabledState
        {
            public CameraEnabledState(Camera camera, bool wasEnabled)
            {
                Camera = camera;
                WasEnabled = wasEnabled;
            }

            public Camera Camera { get; }
            public bool WasEnabled { get; }
        }

        private const float FeetToMeters = 0.3048f;
        private const float DefaultFov = 60f;
        private const float ScopedPresentationEnterAdsBlendT = 0.999f;
        private const float ScopedPresentationExitAdsBlendT = 0.95f;
        private const string CameraPivotName = "CameraPivot";
        private const string PlayerArmsRootName = "PlayerArms";
        private const string WeaponPresentationRootName = "WeaponPresentationRoot";

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private WeaponRegistry _weaponRegistry;
        [SerializeField] private WeaponProjectile _projectilePrefab;
        [SerializeField] private Transform _muzzleTransform;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private Camera _adsCamera;
        [SerializeField] private Animator _packAnimator;
        [SerializeField] private WeaponCombatAudioEmitter _combatAudioEmitter;
        [SerializeField] private PackWeaponPresentationConfig _packPresentationConfig = new PackWeaponPresentationConfig();
        [SerializeField] private Transform _weaponViewParent;
        [SerializeField] private WeaponViewPrefabBinding[] _weaponViewPrefabs = System.Array.Empty<WeaponViewPrefabBinding>();
        [SerializeField] private WeaponAttachmentItemMetadata[] _attachmentItemMetadata = System.Array.Empty<WeaponAttachmentItemMetadata>();
        [SerializeField] private MonoBehaviour _shotCameraRuntimeBehaviour;
        [SerializeField] private ShotCameraSettings _shotCameraSettings = new ShotCameraSettings(true, 100f, 0.1f, 0.25f);
        [SerializeField] private bool _allowSceneWideDependencyLookup;

        private IPlayerInputSource _inputSource;
        private IShotCameraRuntime _shotCameraRuntime;
        private readonly Dictionary<string, WeaponRuntimeState> _statesByItemId = new Dictionary<string, WeaponRuntimeState>();
        private readonly Dictionary<string, PackWeaponRuntimeDriver> _packDriversByItemId = new Dictionary<string, PackWeaponRuntimeDriver>();
        private IWeaponEvents _weaponEvents;
        private IInventoryEvents _inventoryEvents;
        private bool _useRuntimeKernelWeaponEvents = true;
        private bool _useRuntimeKernelInventoryEvents = true;
        private string _equippedItemId;
        private WeaponDefinition _equippedDefinition;
        private bool _isAiming;
        private bool _loggedMissingProjectilePrefab;
        private bool _loggedMissingCoreDependencies;
        private bool _attemptedSceneInputResolution;
        private float _baseCameraFieldOfView = DefaultFov;
        private bool _baseCameraFieldOfViewCaptured;
        private Camera _cachedAdsCamera;
        private bool _pendingUnequipFovBaselineRestore;
        private Transform _defaultMuzzleTransform;
        private GameObject _equippedWeaponView;
        private bool _activationResyncReady;
        private GameAdsStateController _adsStateRuntimeBridge;
        private GameAttachmentManager _adsAttachmentManagerRuntimeBridge;
        private GameRenderTextureScopeController _renderTextureScopeRuntimeBridge;
        private GamePeripheralScopeEffects _peripheralScopeEffectsRuntimeBridge;
        private GameScopeAdjustmentTooltipOverlay _scopeAdjustmentTooltipRuntimeBridge;
        private PlayerLookController _playerLookControllerRuntimeBridge;
        private FpsViewmodelAnimatorDriver _viewmodelAnimatorDriver;
        private float _cachedScopeMagnification = 1f;
        private string _pendingEquipItemId;
        private WeaponDefinition _pendingEquipDefinition;
        private float _pendingEquipApplyTime;
        [SerializeField, Min(0f)] private float _holsterHideDelaySeconds = 0.2f;
        private float _scheduledArmsHideTime = -1f;
        private readonly List<Renderer> _packRenderers = new List<Renderer>();
        private readonly List<RendererVisibilityState> _shotCameraSuppressedRenderers = new List<RendererVisibilityState>();
        private readonly List<CameraEnabledState> _shotCameraSuppressedCameras = new List<CameraEnabledState>();
        private readonly HashSet<int> _shotCameraSuppressedRendererIds = new HashSet<int>();
        private readonly HashSet<int> _shotCameraSuppressedCameraIds = new HashSet<int>();
        private bool _isShotCameraPresentationSuppressed;
        private bool _isStableMagnifiedScopedAds;
        private string _appliedScopeAttachmentItemId = string.Empty;
        private string _appliedMuzzleAttachmentItemId = string.Empty;
        private static readonly Bounds ViewmodelSkinnedBounds = new Bounds(Vector3.zero, new Vector3(8f, 8f, 8f));
        private static readonly Dictionary<int, Material> MaterialUpgradeCacheBySourceId = new Dictionary<int, Material>();
        private static MethodInfo s_createActiveProjectilePathObserverMethod;
        private static bool s_attemptedDevTraceObserverResolution;
        public string EquippedItemId => _equippedItemId;
        public Transform EquippedWeaponViewTransform => _equippedWeaponView != null ? _equippedWeaponView.transform : null;
        public bool IsAiming => _isAiming;
        public bool IsAimInputHeld => _inputSource != null && _inputSource.AimHeld;
        public float CurrentAdsBlendT => ResolveCurrentAdsBlendT();

        private void Awake()
        {
            ResolveReferences();
            _defaultMuzzleTransform = _muzzleTransform;
            RefreshPackRenderers();
            SetArmsVisible(false);
        }

        private void OnEnable()
        {
            if (!_activationResyncReady)
            {
                return;
            }

            ResyncAfterActivation();
        }

        private void OnDisable()
        {
            RestoreShotCameraPresentation();
            _isShotCameraPresentationSuppressed = false;
            ResetStableMagnifiedScopedAdsState();
            ResetScopedViewmodelStabilization();
            DestroyEquippedWeaponView();
        }

        private void Start()
        {
            _activationResyncReady = true;
            ResyncAfterActivation();
        }

        private void Update()
        {
            ResolveReferences();
            if (!DependencyResolutionGuard.HasRequiredReferences(
                    ref _loggedMissingCoreDependencies,
                    this,
                    "PlayerWeaponController requires PlayerInventoryController and WeaponRegistry references.",
                    _inventoryController,
                    _weaponRegistry))
            {
                return;
            }

            ProcessPendingEquip();
            ProcessScheduledArmsHide();
            UpdateEquipFromSelection();
            EnsureEquippedViewMatchesRuntimeState();
            SyncEquippedReserveFromInventory();
            UpdateStableMagnifiedScopedAdsState();
            SyncScopedViewmodelStabilization();
            if (_inputSource == null)
            {
                if (_allowSceneWideDependencyLookup)
                {
                    DependencyResolutionGuard.ResolveOnce(
                        ref _inputSource,
                        ref _attemptedSceneInputResolution,
                        () => DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID)));
                }

                return;
            }

            TickPackPresentation();
            TickReloadCancellation();
            TickReloadCompletion();
            TickFire();
            TickReload();
        }

        public bool TryGetRuntimeState(string itemId, out WeaponRuntimeState state)
        {
            return _statesByItemId.TryGetValue(NormalizeWeaponItemId(itemId), out state);
        }

        public bool HasMagnifiedOpticEquipped()
        {
            if (string.IsNullOrWhiteSpace(_equippedItemId))
            {
                return false;
            }

            if (!TryGetActiveOpticMagnification(out var minMagnification, out var maxMagnification))
            {
                return false;
            }

            return maxMagnification > 1.01f || minMagnification > 1.01f;
        }

        public IReadOnlyList<WeaponRuntimeSnapshot> GetRuntimeStateSnapshots()
        {
            var snapshots = new List<WeaponRuntimeSnapshot>(_statesByItemId.Count);
            foreach (var entry in _statesByItemId)
            {
                var state = entry.Value;
                if (state == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                snapshots.Add(new WeaponRuntimeSnapshot(
                    entry.Key,
                    state.ChamberLoaded,
                    state.MagazineCapacity,
                    state.MagazineCount,
                    state.ReserveCount,
                    state.ChamberRound,
                    state.GetMagazineRoundsSnapshot(),
                    state.GetEquippedAttachmentItemIdsSnapshot()));
            }

            return snapshots;
        }

        public bool ApplyRuntimeState(string itemId, int magazineCount, int reserveCount, bool chamberLoaded)
        {
            var normalizedItemId = NormalizeWeaponItemId(itemId);
            if (string.IsNullOrWhiteSpace(normalizedItemId))
            {
                return false;
            }

            var state = TryGetRuntimeState(normalizedItemId, out var existing)
                ? existing
                : GetOrCreateState(normalizedItemId, ResolveWeaponDefinition(normalizedItemId), seedFromDefinition: false);
            if (state == null)
            {
                return false;
            }

            state.SetAmmoCounts(magazineCount, reserveCount, chamberLoaded);
            if (string.Equals(_equippedItemId, normalizedItemId, StringComparison.Ordinal))
            {
                ResyncEquippedViewFromRuntimeState(state, rebuildView: _equippedWeaponView == null);
            }

            return true;
        }

        public bool ApplyRuntimeBallistics(string itemId, AmmoBallisticSnapshot? chamberRound, IReadOnlyList<AmmoBallisticSnapshot> magazineRounds)
        {
            var normalizedItemId = NormalizeWeaponItemId(itemId);
            if (string.IsNullOrWhiteSpace(normalizedItemId))
            {
                return false;
            }

            var state = TryGetRuntimeState(normalizedItemId, out var existing)
                ? existing
                : GetOrCreateState(normalizedItemId, ResolveWeaponDefinition(normalizedItemId), seedFromDefinition: false);
            if (state == null)
            {
                return false;
            }

            var normalizedMagazineCount = state.MagazineCount;
            var normalizedReserveCount = state.ReserveCount;
            var normalizedChamberLoaded = state.ChamberLoaded;
            state.SetAmmoLoadoutForTests(chamberRound, magazineRounds);
            state.SetAmmoCounts(normalizedMagazineCount, normalizedReserveCount, normalizedChamberLoaded);
            return true;
        }

        public bool ApplyRuntimeAttachments(string itemId, IReadOnlyDictionary<WeaponAttachmentSlotType, string> equippedAttachmentItemIdsBySlot)
        {
            var normalizedItemId = NormalizeWeaponItemId(itemId);
            if (string.IsNullOrWhiteSpace(normalizedItemId))
            {
                return false;
            }

            var state = TryGetRuntimeState(normalizedItemId, out var existing)
                ? existing
                : GetOrCreateState(normalizedItemId, ResolveWeaponDefinition(normalizedItemId), seedFromDefinition: false);
            if (state == null)
            {
                return false;
            }

            ClearAttachmentSlots(state);
            if (equippedAttachmentItemIdsBySlot != null)
            {
                foreach (var entry in equippedAttachmentItemIdsBySlot)
                {
                    state.SetEquippedAttachmentItemId(entry.Key, entry.Value);
                }
            }

            if (string.Equals(_equippedItemId, normalizedItemId, StringComparison.Ordinal))
            {
                if (_equippedWeaponView == null)
                {
                    ResyncEquippedViewFromRuntimeState(state, rebuildView: true);
                }
                else
                {
                    ApplyEquippedAttachmentStateToViewRuntime(state);
                }
            }

            return true;
        }

        public void Configure(IWeaponEvents weaponEvents = null, IInventoryEvents inventoryEvents = null)
        {
            _useRuntimeKernelWeaponEvents = weaponEvents == null;
            _weaponEvents = weaponEvents;
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
        }

        public bool TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType slotType, string attachmentItemId)
        {
            if (string.IsNullOrWhiteSpace(_equippedItemId)
                || _equippedDefinition == null
                || _inventoryController == null
                || _inventoryController.Runtime == null)
            {
                return false;
            }

            if (!TryGetRuntimeState(_equippedItemId, out var state) || state == null)
            {
                return false;
            }

            var previousAttachmentItemId = state.GetEquippedAttachmentItemId(slotType);
            var swapped = WeaponAttachmentSwapService.TrySwap(
                _inventoryController.Runtime,
                _equippedDefinition,
                state,
                BuildAttachmentSlotLookup(BuildAttachmentMetadataLookup()),
                slotType,
                attachmentItemId);
            if (!swapped)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(attachmentItemId) && _equippedWeaponView == null)
            {
                ResolveInventoryEvents()?.RaiseInventoryChanged();
                return true;
            }

            if (_equippedWeaponView == null)
            {
                ResolveInventoryEvents()?.RaiseInventoryChanged();
                return true;
            }

            var equippedAttachmentItemId = state.GetEquippedAttachmentItemId(slotType);
            if (!CanApplyAttachmentToViewRuntime(slotType, equippedAttachmentItemId))
            {
                RollBackAttachmentSwap(slotType, previousAttachmentItemId, state);
                return false;
            }

            var applied = ApplyEquippedAttachmentSlotToViewRuntime(slotType, equippedAttachmentItemId);
            if (!applied)
            {
                RollBackAttachmentSwap(slotType, previousAttachmentItemId, state);
                return false;
            }

            ResolveInventoryEvents()?.RaiseInventoryChanged();
            return true;
        }

        private void RollBackAttachmentSwap(
            WeaponAttachmentSlotType slotType,
            string previousAttachmentItemId,
            WeaponRuntimeState state)
        {
            if (_inventoryController?.Runtime == null || _equippedDefinition == null || state == null)
            {
                return;
            }

            var reverted = WeaponAttachmentSwapService.TrySwap(
                _inventoryController.Runtime,
                _equippedDefinition,
                state,
                BuildAttachmentSlotLookup(BuildAttachmentMetadataLookup()),
                slotType,
                previousAttachmentItemId);
            if (reverted)
            {
                ApplyEquippedAttachmentSlotToViewRuntime(slotType, previousAttachmentItemId);
            }
        }

        private bool CanApplyAttachmentToViewRuntime(WeaponAttachmentSlotType slotType, string attachmentItemId)
        {
            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return true;
            }

            return slotType switch
            {
                WeaponAttachmentSlotType.Scope => ResolveOpticDefinition(attachmentItemId) != null,
                WeaponAttachmentSlotType.Muzzle => ResolveMuzzleAttachmentDefinition(attachmentItemId) != null,
                _ => false
            };
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                if (_allowSceneWideDependencyLookup)
                {
                    _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID));
                    _attemptedSceneInputResolution = true;
                }
            }

            if (_inventoryController == null)
            {
                _inventoryController = GetComponent<PlayerInventoryController>();
            }

            if (_weaponRegistry == null)
            {
                _weaponRegistry = FindFirstObjectByType<WeaponRegistry>();
            }

            if (_muzzleTransform == null)
            {
                _muzzleTransform = transform;
            }

            _defaultMuzzleTransform ??= _muzzleTransform;

            if (_cameraDefaults == null)
            {
                _cameraDefaults = GetComponent<PlayerCameraDefaults>();
            }

            _playerLookControllerRuntimeBridge ??= GetComponent<PlayerLookController>();
            _viewmodelAnimatorDriver ??= GetComponent<FpsViewmodelAnimatorDriver>();

            if (!IsReferenceOnPlayerHierarchy(_packAnimator != null ? _packAnimator.transform : null))
            {
                _packAnimator = ResolvePackAnimator();
                RefreshPackRenderers();
            }

            var viewmodelRoot = ResolveViewmodelRoot();
            if (viewmodelRoot != null)
            {
                ApplyViewmodelLayer(viewmodelRoot);
            }

            if (!IsWeaponViewParentUsable(_weaponViewParent))
            {
                _weaponViewParent = ResolveDefaultWeaponViewParent(viewmodelRoot);
            }

            if (_shotCameraRuntimeBehaviour == null && _shotCameraSettings.Enabled)
            {
                _shotCameraRuntimeBehaviour = GetComponent<ShotCameraRuntime>();
                if (_shotCameraRuntimeBehaviour == null)
                {
                    _shotCameraRuntimeBehaviour = gameObject.AddComponent<ShotCameraRuntime>();
                }
            }

            if (_shotCameraRuntimeBehaviour is ShotCameraRuntime shotCameraRuntime)
            {
                shotCameraRuntime.Configure(_inputSource as IShotCameraInputSource, _shotCameraSettings);
            }

            _shotCameraRuntime = _shotCameraRuntimeBehaviour as IShotCameraRuntime;
        }

        private void ResyncAfterActivation()
        {
            ResolveReferences();
            if (_inventoryController == null || _weaponRegistry == null)
            {
                return;
            }

            UpdateEquipFromSelection();
            EnsureEquippedViewMatchesRuntimeState();
        }

        private void UpdateEquipFromSelection()
        {
            var inventoryRuntime = _inventoryController != null ? _inventoryController.Runtime : null;
            if (inventoryRuntime == null)
            {
                // Keep the current view/runtime bridge alive while inventory runtime is unavailable.
                return;
            }

            var selectedItemId = NormalizeWeaponItemId(inventoryRuntime.SelectedBeltItemId);
            if (HasPendingEquip())
            {
                if (!string.IsNullOrWhiteSpace(selectedItemId) && TryResolveWeaponDefinition(selectedItemId, out var pendingDefinition))
                {
                    if (_pendingEquipItemId != selectedItemId || _pendingEquipDefinition != pendingDefinition)
                    {
                        StartPendingEquip(selectedItemId, pendingDefinition);
                    }

                    return;
                }

                ClearPendingEquip();
                if (string.IsNullOrWhiteSpace(_equippedItemId))
                {
                    ScheduleArmsHide();
                    SetArmsVisible(true);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(selectedItemId))
            {
                SetEquippedWeapon(null, null);
                return;
            }

            if (TryResolveWeaponDefinition(selectedItemId, out var definition))
            {
                SetEquippedWeapon(selectedItemId, definition);
                return;
            }

            SetEquippedWeapon(null, null);
        }

        private void SetEquippedWeapon(string itemId, WeaponDefinition definition)
        {
            itemId = NormalizeWeaponItemId(itemId);
            if (_equippedItemId == itemId)
            {
                if (!string.IsNullOrWhiteSpace(_equippedItemId) && _equippedDefinition != null)
                {
                    if (TryGetRuntimeState(_equippedItemId, out var existingState))
                    {
                        ResyncEquippedViewFromRuntimeState(existingState, rebuildView: _equippedWeaponView == null);
                    }
                }

                return;
            }

            // For weapon-to-weapon swaps, force a short holster phase first so animator
            // evaluates transitions instead of seeing holster+unholster in same frame.
            if (!string.IsNullOrWhiteSpace(_equippedItemId) && !string.IsNullOrWhiteSpace(itemId))
            {
                StartPendingEquip(itemId, definition);
                SetEquippedWeapon(null, null);
                return;
            }

            var previousItemId = _equippedItemId;
            if (!string.IsNullOrWhiteSpace(previousItemId))
            {
                ResolveWeaponEvents()?.RaiseWeaponUnequipStarted(previousItemId);
                CancelReload(previousItemId, WeaponReloadCancelReason.Unequip);
                if (_packDriversByItemId.TryGetValue(previousItemId, out var previousDriver) && previousDriver != null)
                {
                    _pendingUnequipFovBaselineRestore =
                        previousDriver.State.IsAiming || Mathf.Abs(previousDriver.State.AimFovVelocity) > 0.01f;
                    previousDriver.SetEquipped(false);
                }
            }

            DestroyEquippedWeaponView();
            if (_defaultMuzzleTransform != null)
            {
                _muzzleTransform = _defaultMuzzleTransform;
            }

            _equippedItemId = itemId;
            _equippedDefinition = definition;
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                if (HasPendingEquip())
                {
                    CancelScheduledArmsHide();
                    SetArmsVisible(true);
                }
                else
                {
                    ScheduleArmsHide();
                    SetArmsVisible(true);
                }

                return;
            }

            CancelScheduledArmsHide();
            SetArmsVisible(true);
            ResolveWeaponEvents()?.RaiseWeaponEquipStarted(_equippedItemId);
            var state = GetOrCreateState(_equippedItemId, _equippedDefinition, seedFromDefinition: true);
            if (state == null)
            {
                return;
            }

            state.IsEquipped = true;
            ResyncEquippedViewFromRuntimeState(state, rebuildView: true);
            GetOrCreatePackDriver(_equippedItemId).SetEquipped(true);
            ResolveWeaponEvents()?.RaiseWeaponEquipped(_equippedItemId);
        }

        private void ProcessPendingEquip()
        {
            if (!HasPendingEquip() || Time.time < _pendingEquipApplyTime)
            {
                return;
            }

            var itemId = _pendingEquipItemId;
            var definition = _pendingEquipDefinition;
            ClearPendingEquip();
            SetEquippedWeapon(itemId, definition);
        }

        private bool HasPendingEquip()
        {
            return !string.IsNullOrWhiteSpace(_pendingEquipItemId);
        }

        private void StartPendingEquip(string itemId, WeaponDefinition definition)
        {
            _pendingEquipItemId = NormalizeWeaponItemId(itemId);
            _pendingEquipDefinition = definition;
            _pendingEquipApplyTime = Time.time + 0.08f;
        }

        private void EnsureEquippedViewMatchesRuntimeState()
        {
            if (string.IsNullOrWhiteSpace(_equippedItemId)
                || _equippedDefinition == null
                || !TryGetRuntimeState(_equippedItemId, out var state)
                || state == null)
            {
                return;
            }

            if (_equippedWeaponView == null)
            {
                ResyncEquippedViewFromRuntimeState(state, rebuildView: true);
                return;
            }

            var expectedScopeAttachmentItemId = NormalizeAttachmentItemId(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope));
            var mountedScopeAttachmentItemId = ResolveMountedScopeAttachmentItemId();
            if (!string.Equals(expectedScopeAttachmentItemId, mountedScopeAttachmentItemId, StringComparison.Ordinal)
                || ShouldRepairScopedRuntimePresentation(expectedScopeAttachmentItemId, mountedScopeAttachmentItemId))
            {
                _appliedScopeAttachmentItemId = string.Empty;
            }

            ApplyEquippedAttachmentStateToViewRuntime(state);
        }

        private bool ShouldRepairScopedRuntimePresentation(string expectedScopeAttachmentItemId, string mountedScopeAttachmentItemId)
        {
            if (string.IsNullOrEmpty(expectedScopeAttachmentItemId)
                || !string.Equals(expectedScopeAttachmentItemId, mountedScopeAttachmentItemId, StringComparison.Ordinal))
            {
                return false;
            }

            var activeOpticDefinition = ResolveActiveOpticDefinition();
            if (_adsStateRuntimeBridge == null
                || _adsAttachmentManagerRuntimeBridge == null
                || _scopeAdjustmentTooltipRuntimeBridge == null
                || activeOpticDefinition == null
                || _adsAttachmentManagerRuntimeBridge.GetActiveSightAnchor() == null)
            {
                return true;
            }

            if (!string.Equals(GetOpticDefinitionId(activeOpticDefinition), expectedScopeAttachmentItemId, StringComparison.Ordinal))
            {
                return true;
            }

            return UsesRenderTexturePipOptic(activeOpticDefinition)
                && (_renderTextureScopeRuntimeBridge == null || _peripheralScopeEffectsRuntimeBridge == null);
        }

        private void ClearPendingEquip()
        {
            _pendingEquipItemId = null;
            _pendingEquipDefinition = null;
            _pendingEquipApplyTime = 0f;
        }

        private void ProcessScheduledArmsHide()
        {
            if (_scheduledArmsHideTime < 0f || Time.time < _scheduledArmsHideTime)
            {
                return;
            }

            _scheduledArmsHideTime = -1f;
            if (string.IsNullOrWhiteSpace(_equippedItemId) && !HasPendingEquip())
            {
                SetArmsVisible(false);
            }
        }

        private void ScheduleArmsHide()
        {
            _scheduledArmsHideTime = Time.time + _holsterHideDelaySeconds;
        }

        private void CancelScheduledArmsHide()
        {
            _scheduledArmsHideTime = -1f;
        }

        private void RefreshPackRenderers()
        {
            _packRenderers.Clear();
            if (_packAnimator == null)
            {
                return;
            }

            _packAnimator.GetComponentsInChildren(true, _packRenderers);
            ConfigureViewmodelRenderers();
        }

        private void SetArmsVisible(bool visible)
        {
            if (_packAnimator == null)
            {
                return;
            }

            if (_packRenderers.Count == 0)
            {
                RefreshPackRenderers();
            }

            for (var i = 0; i < _packRenderers.Count; i++)
            {
                var renderer = _packRenderers[i];
                if (renderer == null || renderer.enabled == visible)
                {
                    continue;
                }

                renderer.enabled = visible;
            }
        }

        public void SetShotCameraPresentationSuppressed(bool suppressed)
        {
            if (_isShotCameraPresentationSuppressed == suppressed)
            {
                return;
            }

            _isShotCameraPresentationSuppressed = suppressed;
            if (suppressed)
            {
                CaptureAndDisableShotCameraPresentation();
                return;
            }

            RestoreShotCameraPresentation();
        }

        public void SetShotCameraPresentationActive(bool active)
        {
            SetShotCameraPresentationSuppressed(active);
        }

        private void ConfigureViewmodelRenderers()
        {
            for (var i = 0; i < _packRenderers.Count; i++)
            {
                var renderer = _packRenderers[i];
                if (renderer is not SkinnedMeshRenderer skinned)
                {
                    continue;
                }

                if (!skinned.updateWhenOffscreen)
                {
                    skinned.updateWhenOffscreen = true;
                }

                if (skinned.localBounds != ViewmodelSkinnedBounds)
                {
                    skinned.localBounds = ViewmodelSkinnedBounds;
                }
            }
        }

        private void CaptureAndDisableShotCameraPresentation()
        {
            _shotCameraSuppressedRenderers.Clear();
            _shotCameraSuppressedCameras.Clear();
            _shotCameraSuppressedRendererIds.Clear();
            _shotCameraSuppressedCameraIds.Clear();

            CaptureAndDisableRenderers(_packAnimator != null ? _packAnimator.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>());
            CaptureAndDisableRenderers(_equippedWeaponView != null ? _equippedWeaponView.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>());

            var worldCamera = ResolveAdsCamera();
            CaptureAndDisableCamera(ResolveViewmodelCamera(worldCamera));
            CaptureAndDisableCamera(worldCamera != null ? worldCamera.transform.Find("ScopeCamera")?.GetComponent<Camera>() : null);
        }

        private void RestoreShotCameraPresentation()
        {
            for (var i = _shotCameraSuppressedRenderers.Count - 1; i >= 0; i--)
            {
                var state = _shotCameraSuppressedRenderers[i];
                if (state.Renderer == null)
                {
                    continue;
                }

                state.Renderer.enabled = state.WasEnabled;
            }

            for (var i = _shotCameraSuppressedCameras.Count - 1; i >= 0; i--)
            {
                var state = _shotCameraSuppressedCameras[i];
                if (state.Camera == null)
                {
                    continue;
                }

                state.Camera.enabled = state.WasEnabled;
            }

            _shotCameraSuppressedRenderers.Clear();
            _shotCameraSuppressedCameras.Clear();
            _shotCameraSuppressedRendererIds.Clear();
            _shotCameraSuppressedCameraIds.Clear();
        }

        private void CaptureAndDisableRenderers(Renderer[] renderers)
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !_shotCameraSuppressedRendererIds.Add(renderer.GetInstanceID()))
                {
                    continue;
                }

                _shotCameraSuppressedRenderers.Add(new RendererVisibilityState(renderer, renderer.enabled));
                renderer.enabled = false;
            }
        }

        private void CaptureAndDisableCamera(Camera camera)
        {
            if (camera == null || !_shotCameraSuppressedCameraIds.Add(camera.GetInstanceID()))
            {
                return;
            }

            _shotCameraSuppressedCameras.Add(new CameraEnabledState(camera, camera.enabled));
            camera.enabled = false;
        }

        private void TickFire()
        {
            if (IsFireInputBlocked())
            {
                _inputSource?.ConsumeFirePressed();
                return;
            }

            if (_inputSource.SprintHeld)
            {
                return;
            }

            if (!_inputSource.ConsumeFirePressed())
            {
                return;
            }

            if (!TryGetEquippedState(out var state))
            {
                return;
            }

            var packDriver = GetOrCreatePackDriver(_equippedItemId);
            if (packDriver == null || !packDriver.CanFire(Time.time))
            {
                return;
            }

            if (!state.TryFire(Time.time, out var fireData))
            {
                return;
            }

            packDriver.NotifyFire(Time.time, state.FireIntervalSeconds);

            var ballisticSpec = ResolveBallisticSpec(fireData);
            var hasQualifiedShotCameraPrediction = TryBuildShotCameraPrediction(out var predictedImpactPoint, out var predictedDistanceMeters);
            var projectile = SpawnProjectile();
            projectile?.Configure(_useRuntimeKernelWeaponEvents ? null : _weaponEvents);
            projectile?.SetPathObserver(TryCreateActiveTracePathObserver());
            var firedDirection = ApplyDispersion(_muzzleTransform.forward, ballisticSpec.DispersionMoa, URandom.value, URandom.value);
            projectile?.Initialize(
                _equippedItemId,
                firedDirection,
                ballisticSpec.MuzzleVelocityFps * FeetToMeters,
                _equippedDefinition.ProjectileGravityMultiplier,
                _equippedDefinition.BaseDamage,
                ballisticCoefficientG1: ballisticSpec.BallisticCoefficientG1,
                projectileMassGrains: ballisticSpec.ProjectileMassGrains,
                shooterRoot: transform);
            if (hasQualifiedShotCameraPrediction)
            {
                TryRegisterQualifiedShotCamera(projectile, predictedImpactPoint, predictedDistanceMeters);
            }

            NotifyViewWeaponFired(_equippedItemId);
            var muzzleAudioOverride = ResolveMuzzleAudioOverride();
            ResolveWeaponEvents()?.RaiseWeaponFired(_equippedItemId, _muzzleTransform.position, firedDirection);
            ResolveCombatAudioEmitter()?.EmitWeaponFire(_equippedItemId, _muzzleTransform.position, muzzleAudioOverride);
        }

        private static bool IsFireInputBlocked()
        {
            if (ShotCameraRuntime.IsAnyShotCameraActive)
            {
                return true;
            }

            if (PlayerCursorLockController.IsGameplayInputBlocked)
            {
                return true;
            }

            return RuntimeKernelBootstrapper.UiStateEvents?.IsAnyMenuOpen ?? false;
        }

        private WeaponProjectile SpawnProjectile()
        {
            if (_muzzleTransform == null)
            {
                return null;
            }

            if (_projectilePrefab != null)
            {
                return Instantiate(_projectilePrefab, _muzzleTransform.position, _muzzleTransform.rotation);
            }

            if (!_loggedMissingProjectilePrefab)
            {
                Debug.LogWarning("PlayerWeaponController has no projectile prefab assigned. Spawning runtime fallback projectile.", this);
                _loggedMissingProjectilePrefab = true;
            }

            var fallbackGo = new GameObject("RuntimeWeaponProjectile");
            fallbackGo.transform.SetPositionAndRotation(_muzzleTransform.position, _muzzleTransform.rotation);
            return fallbackGo.AddComponent<WeaponProjectile>();
        }

        private static WeaponProjectile.IPathObserver TryCreateActiveTracePathObserver()
        {
            if (s_createActiveProjectilePathObserverMethod == null && !s_attemptedDevTraceObserverResolution)
            {
                s_attemptedDevTraceObserverResolution = true;
                var devTraceRuntimeType = Type.GetType("Reloader.DevTools.Runtime.DevTraceRuntime, Reloader.DevTools");
                s_createActiveProjectilePathObserverMethod = devTraceRuntimeType?.GetMethod(
                    "TryCreateActiveProjectilePathObserver",
                    BindingFlags.Public | BindingFlags.Static);
            }

            return s_createActiveProjectilePathObserverMethod?.Invoke(null, null) as WeaponProjectile.IPathObserver;
        }

        private void TryRegisterQualifiedShotCamera(WeaponProjectile projectile, Vector3 predictedImpactPoint, float predictedDistanceMeters)
        {
            if (projectile == null
                || !TryBuildShotCameraRequest(projectile, predictedImpactPoint, predictedDistanceMeters, out var request))
            {
                return;
            }

            if (_shotCameraRuntime != null && _shotCameraRuntime.TryRegisterShot(request))
            {
                return;
            }

            if (_shotCameraRuntimeBehaviour == null)
            {
                return;
            }

            var compatibilityMethod = _shotCameraRuntimeBehaviour.GetType().GetMethod(
                "RegisterShotCameraRequest",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(WeaponProjectile) },
                modifiers: null);
            compatibilityMethod?.Invoke(_shotCameraRuntimeBehaviour, new object[] { projectile });
        }

        private bool TryBuildShotCameraPrediction(out Vector3 predictedImpactPoint, out float predictedDistanceMeters)
        {
            predictedImpactPoint = default;
            predictedDistanceMeters = 0f;
            if (!_shotCameraSettings.Enabled
                || _inputSource == null
                || !_inputSource.AimHeld)
            {
                return false;
            }

            if (!TryPredictShotCameraImpactPoint(out predictedImpactPoint, out predictedDistanceMeters))
            {
                return false;
            }

            return predictedDistanceMeters > _shotCameraSettings.MinimumPredictedDistanceMeters;
        }

        private bool TryBuildShotCameraRequest(
            WeaponProjectile projectile,
            Vector3 predictedImpactPoint,
            float predictedDistanceMeters,
            out ShotCameraRequest request)
        {
            request = default;
            if (projectile == null
                || !_shotCameraSettings.Enabled
                || _inputSource == null
                || !_inputSource.AimHeld
                || predictedDistanceMeters <= _shotCameraSettings.MinimumPredictedDistanceMeters)
            {
                return false;
            }

            request = new ShotCameraRequest(
                projectile,
                projectile.transform.position,
                predictedImpactPoint,
                predictedDistanceMeters,
                _shotCameraSettings);
            return true;
        }

        private bool TryPredictShotCameraImpactPoint(out Vector3 predictedImpactPoint, out float predictedDistanceMeters)
        {
            predictedImpactPoint = default;
            predictedDistanceMeters = 0f;

            var camera = ResolveAdsCamera();
            if (camera == null)
            {
                return false;
            }

            Physics.SyncTransforms();
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var hits = Physics.RaycastAll(ray, float.PositiveInfinity, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (candidate.collider == null
                    || candidate.collider.isTrigger
                    || candidate.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                predictedImpactPoint = candidate.point;
                var origin = _muzzleTransform != null ? _muzzleTransform.position : transform.position;
                predictedDistanceMeters = Vector3.Distance(origin, predictedImpactPoint);
                return true;
            }

            return false;
        }

        private void TickReload()
        {
            if (ShotCameraRuntime.IsAnyShotCameraActive)
            {
                _inputSource?.ConsumeReloadPressed();
                return;
            }

            if (!_inputSource.ConsumeReloadPressed())
            {
                return;
            }

            if (!TryGetEquippedState(out var state))
            {
                return;
            }

            if (_inventoryController?.Runtime == null)
            {
                return;
            }

            var packDriver = GetOrCreatePackDriver(_equippedItemId);
            if (packDriver == null || packDriver.State.IsReloading)
            {
                return;
            }

            var ammoItemId = ResolveAmmoItemId(state);
            var availableInInventory = _inventoryController.Runtime.GetItemQuantity(ammoItemId);
            state.SetReserveCount(availableInInventory);
            if (state.MagazineCount >= state.MagazineCapacity || state.ReserveCount <= 0)
            {
                return;
            }

            var presentationConfig = packDriver.PresentationConfig ?? ResolvePackPresentationConfig(_equippedItemId, _equippedDefinition);
            if (!packDriver.TryStartReload(Time.time, presentationConfig.ReloadDurationSeconds))
            {
                return;
            }

            state.IsReloading = true;
            ResolveWeaponEvents()?.RaiseWeaponReloadStarted(_equippedItemId);
            ResolveCombatAudioEmitter()?.EmitReloadStarted(_equippedItemId, _muzzleTransform.position);
            NotifyViewReloadStarted(_equippedItemId);
        }

        private void TickReloadCancellation()
        {
            if (ShotCameraRuntime.IsAnyShotCameraActive)
            {
                return;
            }

            if (!_inputSource.SprintHeld)
            {
                return;
            }

            CancelReload(_equippedItemId, WeaponReloadCancelReason.Sprint);
        }

        private void TickReloadCompletion()
        {
            if (_inventoryController?.Runtime == null)
            {
                return;
            }

            if (!TryGetEquippedState(out var state) || state == null)
            {
                return;
            }

            var packDriver = GetOrCreatePackDriver(_equippedItemId);
            if (packDriver == null || !packDriver.State.IsReloading || !packDriver.TryCompleteReload(Time.time))
            {
                return;
            }

            state.IsReloading = false;

            var ammoItemId = ResolveAmmoItemId(state);
            var availableInInventory = _inventoryController.Runtime.GetItemQuantity(ammoItemId);
            state.SetReserveCount(availableInInventory);
            if (!state.TryReload())
            {
                return;
            }

            var consumed = Mathf.Max(0, availableInInventory - state.ReserveCount);
            if (consumed > 0)
            {
                var removed = _inventoryController.Runtime.TryRemoveStackItem(ammoItemId, consumed);
                if (removed)
                {
                    ResolveInventoryEvents()?.RaiseInventoryChanged();
                }
            }

            state.SetReserveCount(_inventoryController.Runtime.GetItemQuantity(ammoItemId));
            ResolveWeaponEvents()?.RaiseWeaponReloaded(_equippedItemId, state.MagazineCount, state.ReserveCount);
            ResolveCombatAudioEmitter()?.EmitReloadCompleted(_equippedItemId, _muzzleTransform.position);
            NotifyViewReloadCompleted(_equippedItemId);
        }

        private void TickPackPresentation()
        {
            var hasFieldOfView = TryGetCurrentFieldOfView(out var currentFieldOfView);

            if (string.IsNullOrWhiteSpace(_equippedItemId))
            {
                ResetScopedAdsLookSensitivityBridge();
                if (hasFieldOfView)
                {
                    var baselineFieldOfView = Mathf.Clamp(_baseCameraFieldOfView, 1f, 179f);
                    if (_pendingUnequipFovBaselineRestore)
                    {
                        if (Mathf.Abs(currentFieldOfView - baselineFieldOfView) > 0.01f)
                        {
                            TrySetCurrentFieldOfView(baselineFieldOfView);
                        }

                        _pendingUnequipFovBaselineRestore = false;
                    }
                    else
                    {
                        // Preserve external FOV changes (e.g. settings menu) while unarmed.
                        _baseCameraFieldOfView = baselineFieldOfView = Mathf.Clamp(currentFieldOfView, 1f, 179f);
                    }
                }

                return;
            }

            var packDriver = GetOrCreatePackDriver(_equippedItemId);
            if (packDriver == null)
            {
                ResetScopedAdsLookSensitivityBridge();
                return;
            }

            if (HasScopedAdsBridgeActive())
            {
                // Keep pack animator/runtime aim state in sync, but let AdsStateController own camera FOV.
                packDriver.TickAimFov(_inputSource.AimHeld, _baseCameraFieldOfView, _baseCameraFieldOfView, Time.deltaTime);
                TickScopedAdsBridgeInput();
                SyncScopedAdsLookSensitivityBridge();
                return;
            }

            ResetScopedAdsLookSensitivityBridge();

            if (hasFieldOfView && !packDriver.State.IsAiming && Mathf.Abs(packDriver.State.AimFovVelocity) < 0.01f)
            {
                _baseCameraFieldOfView = Mathf.Clamp(currentFieldOfView, 1f, 179f);
            }

            var sourceFieldOfView = hasFieldOfView ? currentFieldOfView : _baseCameraFieldOfView;
            var nextFieldOfView = packDriver.TickAimFov(_inputSource.AimHeld, sourceFieldOfView, _baseCameraFieldOfView, Time.deltaTime);
            if (hasFieldOfView)
            {
                TrySetCurrentFieldOfView(nextFieldOfView);
            }
        }

        private void SyncScopedViewmodelStabilization()
        {
            var shouldStabilize = _isStableMagnifiedScopedAds;

            if (_viewmodelAnimatorDriver != null)
            {
                _viewmodelAnimatorDriver.LockViewmodelRootPose = shouldStabilize;
            }
        }

        private void ResetScopedViewmodelStabilization()
        {
            if (_viewmodelAnimatorDriver != null)
            {
                _viewmodelAnimatorDriver.LockViewmodelRootPose = false;
            }
        }

        private static bool ShouldStabilizeScopedViewmodelPresentation(
            bool isCurrentlyStable,
            bool hasScopedAdsAlignment,
            bool hasMagnifiedOpticEquipped,
            float adsBlendT,
            bool hasEquippedView)
        {
            if (!hasScopedAdsAlignment || !hasMagnifiedOpticEquipped || !hasEquippedView)
            {
                return false;
            }

            var threshold = isCurrentlyStable
                ? ScopedPresentationExitAdsBlendT
                : ScopedPresentationEnterAdsBlendT;

            return adsBlendT >= threshold;
        }

        private bool HasScopedAdsBridgeActive()
        {
            return _adsStateRuntimeBridge != null
                && _adsAttachmentManagerRuntimeBridge != null
                && _adsAttachmentManagerRuntimeBridge.ActiveOpticDefinition != null;
        }

        public bool HasActiveScopedAdsAlignment => HasScopedAdsBridgeActive();
        public bool HasStableScopedAdsAlignment => _isStableMagnifiedScopedAds;

        private string ResolveMountedScopeAttachmentItemId()
        {
            var activeOptic = ResolveActiveOpticDefinition();
            return NormalizeAttachmentItemId(GetOpticDefinitionId(activeOptic));
        }

        private float ResolveCurrentAdsBlendT()
        {
            return _adsStateRuntimeBridge != null
                ? Mathf.Clamp01(_adsStateRuntimeBridge.AdsT)
                : 0f;
        }

        private void UpdateStableMagnifiedScopedAdsState()
        {
            _isStableMagnifiedScopedAds = ShouldStabilizeScopedViewmodelPresentation(
                _isStableMagnifiedScopedAds,
                HasScopedAdsBridgeActive(),
                HasMagnifiedOpticEquipped(),
                ResolveCurrentAdsBlendT(),
                _equippedWeaponView != null);
        }

        private void ResetStableMagnifiedScopedAdsState()
        {
            _isStableMagnifiedScopedAds = false;
        }

        public bool TryGetActiveOpticMagnification(out float minMagnification, out float maxMagnification)
        {
            minMagnification = 1f;
            maxMagnification = 1f;

            var activeOptic = ResolveActiveOpticDefinition();
            if (activeOptic == null)
            {
                activeOptic = ResolveEquippedScopeDefinitionFromState();
            }

            if (activeOptic == null)
            {
                return false;
            }

            return TryReadOpticMagnification(activeOptic, out minMagnification, out maxMagnification);
        }

        private GameOpticDefinition ResolveActiveOpticDefinition()
        {
            return _adsAttachmentManagerRuntimeBridge != null
                ? _adsAttachmentManagerRuntimeBridge.ActiveOpticDefinition
                : null;
        }

        private GameOpticDefinition ResolveEquippedScopeDefinitionFromState()
        {
            if (string.IsNullOrWhiteSpace(_equippedItemId)
                || !TryGetRuntimeState(_equippedItemId, out var runtimeState)
                || runtimeState == null)
            {
                return null;
            }

            var attachmentItemId = runtimeState.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope);
            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return null;
            }

            return ResolveOpticDefinition(attachmentItemId);
        }

        private static bool TryReadOpticMagnification(GameOpticDefinition opticDefinition, out float minMagnification, out float maxMagnification)
        {
            minMagnification = 1f;
            maxMagnification = 1f;
            if (opticDefinition == null)
            {
                return false;
            }

            minMagnification = opticDefinition.MagnificationMin;
            maxMagnification = opticDefinition.MagnificationMax;
            return true;
        }

        private void TickScopedAdsBridgeInput()
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            _adsStateRuntimeBridge.SetAdsHeld(_inputSource != null && _inputSource.AimHeld);
            _cachedScopeMagnification = _adsStateRuntimeBridge.CurrentMagnification;

            TryApplyScopedAdjustmentInput();

            var scrollY = _inputSource != null ? _inputSource.ConsumeZoomInput() : 0f;
            if (Mathf.Abs(scrollY) <= 0.01f)
            {
                return;
            }

            var nextMagnification = _cachedScopeMagnification + scrollY;
            _adsStateRuntimeBridge.SetMagnification(nextMagnification);
        }

        private void SyncScopedAdsLookSensitivityBridge()
        {
            _playerLookControllerRuntimeBridge ??= GetComponent<PlayerLookController>();
            if (_playerLookControllerRuntimeBridge == null || _adsStateRuntimeBridge == null)
            {
                return;
            }

            if (!HasStableScopedAdsAlignment)
            {
                ResetScopedAdsLookSensitivityBridge();
                return;
            }

            var activeOptic = ResolveActiveOpticDefinition() ?? ResolveEquippedScopeDefinitionFromState();
            if (!UsesRenderTexturePipOptic(activeOptic))
            {
                ResetScopedAdsLookSensitivityBridge();
                return;
            }

            var clampedScale = Mathf.Max(0.001f, _adsStateRuntimeBridge.CurrentPipPrecisionScale);
            _playerLookControllerRuntimeBridge.RuntimeAdsSensitivityMultiplier = new Vector2(clampedScale, clampedScale);
            _playerLookControllerRuntimeBridge.AllowFovSensitivityScaling = false;
        }

        private void ResetScopedAdsLookSensitivityBridge()
        {
            _playerLookControllerRuntimeBridge ??= GetComponent<PlayerLookController>();
            if (_playerLookControllerRuntimeBridge == null)
            {
                return;
            }

            _playerLookControllerRuntimeBridge.RuntimeAdsSensitivityMultiplier = Vector2.one;
            _playerLookControllerRuntimeBridge.AllowFovSensitivityScaling = true;
        }

        private static bool UsesRenderTexturePipOptic(GameOpticDefinition opticDefinition)
        {
            return opticDefinition != null && opticDefinition.VisualModePolicy == GameAdsVisualMode.RenderTexturePiP;
        }

        private void TryApplyScopedAdjustmentInput()
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            if (!TryReadScopedAdjustmentKeyInput(out var windageClicks, out var elevationClicks))
            {
                return;
            }

            _adsStateRuntimeBridge.ApplyScopeAdjustmentInput(windageClicks, elevationClicks);
        }

        private static bool TryReadScopedAdjustmentKeyInput(out int windageClicks, out int elevationClicks)
        {
            windageClicks = 0;
            elevationClicks = 0;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                var minusPressed = keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame;
                var equalsPressed = keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame;
                if (!minusPressed && !equalsPressed)
                {
                    return false;
                }

                return ResolveScopedAdjustmentClicks(
                    shiftHeld,
                    minusPressed,
                    equalsPressed,
                    out windageClicks,
                    out elevationClicks);
            }

            try
            {
                var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var minusPressed = Input.GetKeyDown(KeyCode.Minus);
                var equalsPressed = Input.GetKeyDown(KeyCode.Equals);
                if (!minusPressed && !equalsPressed)
                {
                    return false;
                }

                return ResolveScopedAdjustmentClicks(
                    shiftHeld,
                    minusPressed,
                    equalsPressed,
                    out windageClicks,
                    out elevationClicks);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool ResolveScopedAdjustmentClicks(
            bool shiftHeld,
            bool minusPressed,
            bool equalsPressed,
            out int windageClicks,
            out int elevationClicks)
        {
            windageClicks = 0;
            elevationClicks = 0;

            if (minusPressed)
            {
                if (shiftHeld)
                {
                    windageClicks -= 1;
                }
                else
                {
                    elevationClicks -= 1;
                }
            }

            if (equalsPressed)
            {
                if (shiftHeld)
                {
                    windageClicks += 1;
                }
                else
                {
                    elevationClicks += 1;
                }
            }

            return windageClicks != 0 || elevationClicks != 0;
        }

        // Forwarded from PackAnimationEventRelay attached to the animator GameObject.
        public void OnAnimationEndedHolster()
        {
        }

        public void OnAmmunitionFill()
        {
            NotifyViewMagazineInserted(_equippedItemId);
        }

        public void OnAnimationEndedReload()
        {
            NotifyViewMagazineInserted(_equippedItemId);
        }

        public void OnAmmunitionFillForwarded()
        {
            OnAmmunitionFill();
        }

        public void OnAnimationEndedReloadForwarded()
        {
            OnAnimationEndedReload();
        }

        private void CancelReload(string itemId, WeaponReloadCancelReason reason)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            if (!_statesByItemId.TryGetValue(itemId, out var state) || state == null)
            {
                return;
            }

            if (!_packDriversByItemId.TryGetValue(itemId, out var packDriver) || packDriver == null || !packDriver.CancelReload())
            {
                return;
            }

            state.IsReloading = false;
            NotifyViewMagazineInserted(itemId);
            ResolveWeaponEvents()?.RaiseWeaponReloadCancelled(itemId, reason);
        }

        private WeaponCombatAudioEmitter ResolveCombatAudioEmitter()
        {
            if (_combatAudioEmitter != null)
            {
                return _combatAudioEmitter;
            }

            _combatAudioEmitter = GetComponentInChildren<WeaponCombatAudioEmitter>(true);
            if (_combatAudioEmitter == null)
            {
                _combatAudioEmitter = gameObject.GetComponent<WeaponCombatAudioEmitter>();
            }

            if (_combatAudioEmitter == null)
            {
                _combatAudioEmitter = gameObject.AddComponent<WeaponCombatAudioEmitter>();
            }

            _combatAudioEmitter.EnsureCatalog(CombatAudioCatalogResolver.Resolve(null));
            return _combatAudioEmitter;
        }

        private void NotifyViewWeaponFired(string itemId)
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            _equippedWeaponView.SendMessage("HandleWeaponFired", itemId, SendMessageOptions.DontRequireReceiver);
        }

        private void NotifyViewReloadStarted(string itemId)
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            _equippedWeaponView.SendMessage("HandleReloadStarted", itemId, SendMessageOptions.DontRequireReceiver);
        }

        private void NotifyViewMagazineInserted(string itemId)
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            _equippedWeaponView.SendMessage("HandleMagazineInserted", itemId, SendMessageOptions.DontRequireReceiver);
        }

        private void NotifyViewReloadCompleted(string itemId)
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            _equippedWeaponView.SendMessage("HandleReloadCompleted", itemId, SendMessageOptions.DontRequireReceiver);
        }

        private AudioClip ResolveMuzzleAudioOverride()
        {
            var muzzleRuntime = _equippedWeaponView != null
                ? _equippedWeaponView.GetComponentInChildren<GameMuzzleAttachmentRuntime>(true)
                : null;
            return muzzleRuntime != null ? muzzleRuntime.TryGetFireClipOverride() : null;
        }

        private void EnsureMuzzleRuntimeBridge(
            GameObject viewRoot,
            WeaponViewAttachmentMounts mounts,
            Transform viewMuzzle)
        {
            if (viewRoot == null || viewMuzzle == null)
            {
                return;
            }

            var runtimeComponent = viewRoot.GetComponent<GameMuzzleAttachmentRuntime>() ?? viewRoot.AddComponent<GameMuzzleAttachmentRuntime>();
            Transform attachmentSlot = null;
            if (mounts != null)
            {
                mounts.TryGetAttachmentSlot(WeaponAttachmentSlotType.Muzzle, out attachmentSlot);
            }

            runtimeComponent.ConfigureRuntimeReferences(viewMuzzle, attachmentSlot);
            runtimeComponent.Unequip();
        }

        private void EnsureDetachableMagazineRuntimeBridge(GameObject viewRoot, WeaponViewAttachmentMounts mounts)
        {
            if (viewRoot == null)
            {
                return;
            }

            var runtimeComponent = viewRoot.GetComponent<GameDetachableMagazineRuntime>() ?? viewRoot.AddComponent<GameDetachableMagazineRuntime>();
            var magazineSocket = mounts != null ? mounts.MagazineSocket : null;
            var dropSocket = mounts != null && mounts.MagazineDropSocket != null
                ? mounts.MagazineDropSocket
                : magazineSocket;
            if (magazineSocket == null)
            {
                return;
            }
            runtimeComponent.ConfigureRuntimeReferences(magazineSocket, dropSocket);
            runtimeComponent.SetAttachment(null);
        }

        private static Type ResolveTypeByName(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            var direct = Type.GetType(fullTypeName);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolved = assemblies[i].GetType(fullTypeName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private Dictionary<string, WeaponAttachmentItemMetadata> BuildAttachmentMetadataLookup()
        {
            var lookup = new Dictionary<string, WeaponAttachmentItemMetadata>(StringComparer.Ordinal);
            if (_attachmentItemMetadata == null || _attachmentItemMetadata.Length == 0)
            {
                return lookup;
            }

            for (var i = 0; i < _attachmentItemMetadata.Length; i++)
            {
                var metadata = _attachmentItemMetadata[i];
                if (metadata == null || string.IsNullOrWhiteSpace(metadata.AttachmentItemId))
                {
                    continue;
                }

                lookup[metadata.AttachmentItemId] = metadata;
            }

            return lookup;
        }

        private static Dictionary<string, WeaponAttachmentSlotType> BuildAttachmentSlotLookup(
            IReadOnlyDictionary<string, WeaponAttachmentItemMetadata> metadataLookup)
        {
            var lookup = new Dictionary<string, WeaponAttachmentSlotType>(StringComparer.Ordinal);
            if (metadataLookup == null || metadataLookup.Count == 0)
            {
                return lookup;
            }

            foreach (var entry in metadataLookup)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                {
                    continue;
                }

                lookup[entry.Key] = entry.Value.SlotType;
            }

            return lookup;
        }

        private IWeaponEvents ResolveWeaponEvents()
        {
            return _useRuntimeKernelWeaponEvents ? RuntimeKernelBootstrapper.WeaponEvents : _weaponEvents;
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            return _useRuntimeKernelInventoryEvents ? RuntimeKernelBootstrapper.InventoryEvents : _inventoryEvents;
        }

        private bool TryGetEquippedState(out WeaponRuntimeState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                return false;
            }

            state = GetOrCreateState(_equippedItemId, _equippedDefinition, seedFromDefinition: true);
            return state != null;
        }

        private WeaponRuntimeState GetOrCreateState(string itemId, WeaponDefinition definition, bool seedFromDefinition)
        {
            itemId = NormalizeWeaponItemId(itemId);
            if (definition == null || string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            if (_statesByItemId.TryGetValue(itemId, out var existing))
            {
                return existing;
            }

            var state = new WeaponRuntimeState(
                itemId,
                definition.MagazineCapacity,
                definition.FireIntervalSeconds,
                seedFromDefinition ? definition.StartingMagazineCount : 0,
                0,
                seedFromDefinition && definition.StartingChamberLoaded);

            _statesByItemId[itemId] = state;
            if (seedFromDefinition)
            {
                SeedStateAmmoFromDefinition(state, definition);
            }

            return state;
        }

        private void SeedStateAmmoFromDefinition(WeaponRuntimeState state, WeaponDefinition definition)
        {
            if (state == null || definition == null)
            {
                return;
            }

            var chamberRound = definition.StartingChamberLoaded ? BuildDefinitionRound(definition) : (AmmoBallisticSnapshot?)null;
            var magazineRounds = new AmmoBallisticSnapshot[definition.StartingMagazineCount];
            for (var i = 0; i < magazineRounds.Length; i++)
            {
                magazineRounds[i] = BuildDefinitionRound(definition);
            }

            state.SetAmmoLoadoutForTests(chamberRound, magazineRounds);
            SyncEquippedReserveFromInventory();
        }

        private CartridgeBallisticSpec ResolveBallisticSpec(WeaponFireData fireData)
        {
            if (fireData.FiredRound.HasValue)
            {
                return CartridgeBallisticSpecBuilder.Build(fireData.FiredRound.Value, URandom.value);
            }

            var fallbackRound = BuildDefinitionRound(_equippedDefinition);
            return CartridgeBallisticSpecBuilder.Build(fallbackRound, URandom.value);
        }

        private static AmmoBallisticSnapshot BuildDefinitionRound(WeaponDefinition definition)
        {
            var ammoItemId = definition != null && !string.IsNullOrWhiteSpace(definition.AmmoItemId)
                ? definition.AmmoItemId
                : WeaponAmmoDefaults.DefaultAmmoItemId;

            return WeaponAmmoDefaults.BuildFactoryRound(ammoItemId);
        }

        private void SyncEquippedReserveFromInventory()
        {
            if (_inventoryController?.Runtime == null || !TryGetEquippedState(out var state) || state == null)
            {
                return;
            }

            var ammoItemId = ResolveAmmoItemId(state);
            var inventoryQuantity = _inventoryController.Runtime.GetItemQuantity(ammoItemId);
            state.SetReserveCount(inventoryQuantity);
        }

        private static string ResolveAmmoItemId(WeaponRuntimeState state)
        {
            if (state?.ChamberRound.HasValue == true && !string.IsNullOrWhiteSpace(state.ChamberRound.Value.AmmoItemId))
            {
                return state.ChamberRound.Value.AmmoItemId;
            }

            if (state != null)
            {
                var rounds = state.GetMagazineRoundsSnapshot();
                if (rounds.Count > 0 && !string.IsNullOrWhiteSpace(rounds[0].AmmoItemId))
                {
                    return rounds[0].AmmoItemId;
                }
            }

            return WeaponAmmoDefaults.DefaultAmmoItemId;
        }

        private static Vector3 ApplyDispersion(Vector3 direction, float dispersionMoa, float random01A, float random01B)
        {
            var safeDirection = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
            if (dispersionMoa <= 0f)
            {
                return safeDirection;
            }

            var tangent = Vector3.Cross(safeDirection, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.Cross(safeDirection, Vector3.right);
            }

            tangent.Normalize();
            var bitangent = Vector3.Cross(tangent, safeDirection).normalized;
            var maxAngleRadians = Mathf.Deg2Rad * (dispersionMoa / 60f);
            var minCosine = Mathf.Cos(maxAngleRadians);
            var cosineTheta = Mathf.Lerp(minCosine, 1f, Mathf.Clamp01(random01A));
            var sineTheta = Mathf.Sqrt(Mathf.Max(0f, 1f - (cosineTheta * cosineTheta)));
            var phi = Mathf.PI * 2f * Mathf.Clamp01(random01B);

            var offsetOnTangentPlane = (Mathf.Cos(phi) * tangent) + (Mathf.Sin(phi) * bitangent);
            return ((safeDirection * cosineTheta) + (offsetOnTangentPlane * sineTheta)).normalized;
        }

        private PackWeaponRuntimeDriver GetOrCreatePackDriver(string itemId)
        {
            itemId = NormalizeWeaponItemId(itemId);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            var definition = itemId == _equippedItemId ? _equippedDefinition : ResolveWeaponDefinition(itemId);
            var presentationConfig = ResolvePackPresentationConfig(itemId, definition);

            if (_packDriversByItemId.TryGetValue(itemId, out var cachedDriver))
            {
                cachedDriver.SetAnimator(_packAnimator);
                cachedDriver.SetPresentationConfig(presentationConfig);
                return cachedDriver;
            }

            var runtimeState = new PackWeaponRuntimeState(itemId);
            var driver = new PackWeaponRuntimeDriver(runtimeState, presentationConfig, _packAnimator);
            driver.AimStateChanged += isAiming =>
            {
                _isAiming = !string.IsNullOrWhiteSpace(_equippedItemId) && _equippedItemId == itemId && isAiming;
                ResolveWeaponEvents()?.RaiseWeaponAimChanged(itemId, isAiming);
            };
            _packDriversByItemId[itemId] = driver;
            return driver;
        }

        private void SpawnEquippedWeaponView(string itemId)
        {
            itemId = NormalizeWeaponItemId(itemId);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            var viewPrefab = ResolveWeaponViewPrefab(itemId);
            if (viewPrefab == null)
            {
                return;
            }

            // Re-resolve every spawn to avoid stale references after scene/prefab swaps.
            var parent = IsWeaponViewParentUsable(_weaponViewParent) ? _weaponViewParent : null;
            if (parent == null)
            {
                parent = ResolveDefaultWeaponViewParent();
            }

            if (parent == null)
            {
                return;
            }

            _weaponViewParent = parent;
            _equippedWeaponView = InstantiateWeaponView(viewPrefab, parent);
            if (_equippedWeaponView == null)
            {
                Debug.LogWarning($"Failed to spawn weapon view for '{itemId}'. Source '{viewPrefab.name}' is not a GameObject instance.", this);
                return;
            }

            _equippedWeaponView.name = $"EquippedView_{itemId}";
            _equippedWeaponView.transform.localPosition = Vector3.zero;
            _equippedWeaponView.transform.localRotation = Quaternion.identity;
            _equippedWeaponView.transform.localScale = Vector3.one;
            ResetAppliedAttachmentViewState();
            ApplyViewmodelLayer(_equippedWeaponView.transform);
            StripViewPhysicsComponents(_equippedWeaponView);
            StripViewRuntimeComponents(_equippedWeaponView);
            NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);

            var mounts = _equippedWeaponView.GetComponent<WeaponViewAttachmentMounts>();
            if (mounts == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{_equippedWeaponView.name}' is missing WeaponViewAttachmentMounts.", this);
            }

            var viewMuzzle = mounts != null ? mounts.MuzzleTransform : null;
            if (viewMuzzle != null)
            {
                _muzzleTransform = viewMuzzle;
            }

            EnsureMuzzleRuntimeBridge(_equippedWeaponView, mounts, viewMuzzle);
            EnsureDetachableMagazineRuntimeBridge(_equippedWeaponView, mounts);
            var manager = EnsureAttachmentManagerRuntimeBridge(_equippedWeaponView);
            EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
        }

        private void ResyncEquippedViewFromRuntimeState(WeaponRuntimeState state, bool rebuildView)
        {
            if (state == null
                || _equippedDefinition == null
                || !string.Equals(_equippedItemId, state.ItemId, StringComparison.Ordinal))
            {
                return;
            }

            if (rebuildView && _equippedWeaponView != null)
            {
                DestroyEquippedWeaponView();
                if (_defaultMuzzleTransform != null)
                {
                    _muzzleTransform = _defaultMuzzleTransform;
                }
            }

            if (_equippedWeaponView == null)
            {
                SpawnEquippedWeaponView(_equippedItemId);
            }
            else
            {
                EnsureEquippedWeaponViewParent();
            }

            ApplyEquippedAttachmentStateToViewRuntime(state);
        }

        private void EnsureEquippedWeaponViewParent()
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            if (!IsWeaponViewParentUsable(_weaponViewParent))
            {
                _weaponViewParent = ResolveDefaultWeaponViewParent();
            }

            if (_weaponViewParent == null || _equippedWeaponView.transform.parent == _weaponViewParent)
            {
                return;
            }

            _equippedWeaponView.transform.SetParent(_weaponViewParent, false);
            _equippedWeaponView.transform.localPosition = Vector3.zero;
            _equippedWeaponView.transform.localRotation = Quaternion.identity;
            _equippedWeaponView.transform.localScale = Vector3.one;
        }

        private void ApplyEquippedAttachmentStateToViewRuntime(WeaponRuntimeState state)
        {
            if (state == null || _equippedWeaponView == null)
            {
                return;
            }

            var scopeAttachmentItemId = NormalizeAttachmentItemId(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope));
            if (!string.Equals(scopeAttachmentItemId, _appliedScopeAttachmentItemId, StringComparison.Ordinal))
            {
                if (ApplyEquippedAttachmentSlotToViewRuntime(WeaponAttachmentSlotType.Scope, scopeAttachmentItemId))
                {
                    _appliedScopeAttachmentItemId = scopeAttachmentItemId;
                }
            }

            var muzzleAttachmentItemId = NormalizeAttachmentItemId(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Muzzle));
            if (!string.Equals(muzzleAttachmentItemId, _appliedMuzzleAttachmentItemId, StringComparison.Ordinal))
            {
                if (ApplyEquippedAttachmentSlotToViewRuntime(WeaponAttachmentSlotType.Muzzle, muzzleAttachmentItemId))
                {
                    _appliedMuzzleAttachmentItemId = muzzleAttachmentItemId;
                }
            }
        }

        private static string NormalizeAttachmentItemId(string attachmentItemId)
        {
            return string.IsNullOrWhiteSpace(attachmentItemId) ? string.Empty : attachmentItemId;
        }

        private void ResetAppliedAttachmentViewState()
        {
            _appliedScopeAttachmentItemId = string.Empty;
            _appliedMuzzleAttachmentItemId = string.Empty;
        }

        private bool ApplyEquippedAttachmentSlotToViewRuntime(WeaponAttachmentSlotType slotType, string attachmentItemId)
        {
            if (_equippedWeaponView == null)
            {
                return false;
            }

            var normalizedItemId = string.IsNullOrWhiteSpace(attachmentItemId) ? string.Empty : attachmentItemId;
            switch (slotType)
            {
                case WeaponAttachmentSlotType.Scope:
                    return ApplyScopeAttachmentToViewRuntime(normalizedItemId);
                case WeaponAttachmentSlotType.Muzzle:
                    return ApplyMuzzleAttachmentToViewRuntime(normalizedItemId);
                default:
                    return false;
            }
        }

        private bool ApplyScopeAttachmentToViewRuntime(string attachmentItemId)
        {
            if (_equippedWeaponView == null)
            {
                return false;
            }

            var manager = EnsureAttachmentManagerRuntimeBridge(_equippedWeaponView);
            if (manager == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                manager.UnequipOptic();
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            if (!HasScopedAttachmentRuntimeAuthoring(_equippedWeaponView))
            {
                return false;
            }

            var definition = ResolveOpticDefinition(attachmentItemId);
            if (definition == null)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: Scope definition resolve failed for attachmentItemId='{attachmentItemId}'.",
                    this);
                manager.UnequipOptic();
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            var opticPrefabObject = definition.OpticPrefab;
            if (opticPrefabObject == null)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: Scope definition '{definition.name}' ({typeof(GameOpticDefinition).FullName}) has null OpticPrefab for attachmentItemId='{attachmentItemId}'.",
                    this);
            }

            if (manager.ActiveOpticDefinition == definition
                && manager.GetActiveSightAnchor() != null)
            {
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            manager.SetPendingOpticAdjustmentStateKey(attachmentItemId);
            var equipSucceeded = manager.EquipOptic(definition);
            if (!equipSucceeded)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: EquipOptic returned failure for attachmentItemId='{attachmentItemId}', definition='{definition.name}' ({typeof(GameOpticDefinition).FullName}), opticPrefab='{opticPrefabObject?.name ?? "<null>"}' ({opticPrefabObject?.GetType().FullName ?? "<null>"}).",
                    this);
            }

            EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
            NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
            return equipSucceeded;
        }

        private static string GetOpticDefinitionId(GameOpticDefinition opticDefinition)
        {
            return opticDefinition != null ? opticDefinition.OpticId : string.Empty;
        }

        private bool ApplyMuzzleAttachmentToViewRuntime(string attachmentItemId)
        {
            if (_equippedWeaponView == null)
            {
                return false;
            }

            var manager = EnsureAttachmentManagerRuntimeBridge(_equippedWeaponView);
            if (manager == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                manager.UnequipMuzzle();
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            var definition = ResolveMuzzleAttachmentDefinition(attachmentItemId);
            if (definition == null)
            {
                manager.UnequipMuzzle();
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            var equipSucceeded = manager.EquipMuzzle(definition);
            EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
            NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
            return equipSucceeded;
        }

        private GameAttachmentManager EnsureAttachmentManagerRuntimeBridge(GameObject viewRoot)
        {
            if (viewRoot == null)
            {
                return null;
            }

            var manager = viewRoot.GetComponent<GameAttachmentManager>() ?? viewRoot.AddComponent<GameAttachmentManager>();

            var mounts = viewRoot.GetComponent<WeaponViewAttachmentMounts>();
            if (mounts == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{viewRoot.name}' is missing WeaponViewAttachmentMounts.", this);
                return null;
            }

            Transform scopeSlot = null;
            Transform muzzleSlot = null;
            mounts.TryGetAttachmentSlot(WeaponAttachmentSlotType.Scope, out scopeSlot);
            mounts.TryGetAttachmentSlot(WeaponAttachmentSlotType.Muzzle, out muzzleSlot);

            if (scopeSlot == null && muzzleSlot == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{viewRoot.name}' is missing explicit attachment slots.", this);
                return null;
            }

            var ironSightAnchor = mounts.IronSightAnchor;
            var muzzleRuntime = viewRoot.GetComponent<GameMuzzleAttachmentRuntime>();
            manager.ConfigureMounts(scopeSlot, ironSightAnchor, muzzleSlot, muzzleRuntime);
            return manager;
        }

        private bool HasScopedAttachmentRuntimeAuthoring(GameObject viewRoot)
        {
            if (viewRoot == null)
            {
                return false;
            }

            var mounts = viewRoot.GetComponent<WeaponViewAttachmentMounts>();
            if (mounts == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{viewRoot.name}' is missing WeaponViewAttachmentMounts.", this);
                return false;
            }

            mounts.TryGetAttachmentSlot(WeaponAttachmentSlotType.Scope, out var scopeSlot);
            if (scopeSlot == null)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: View '{viewRoot.name}' is missing an authored scope attachment slot required for scoped attachment runtime.",
                    this);
                return false;
            }

            if (mounts.IronSightAnchor == null)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: View '{viewRoot.name}' is missing an authored IronSightAnchor required for scoped attachment runtime.",
                    this);
                return false;
            }

            return true;
        }

        private void EnsureScopedAdsRuntimeBridge(GameObject viewRoot, GameAttachmentManager attachmentManager)
        {
            if (viewRoot == null || attachmentManager == null)
            {
                _adsStateRuntimeBridge = null;
                _renderTextureScopeRuntimeBridge = null;
                _peripheralScopeEffectsRuntimeBridge = null;
                _scopeAdjustmentTooltipRuntimeBridge = null;
                return;
            }

            _adsStateRuntimeBridge = gameObject.GetComponent<GameAdsStateController>() ?? gameObject.AddComponent<GameAdsStateController>();
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            var worldCamera = ResolveAdsCamera();
            var viewmodelCamera = ResolveViewmodelCamera(worldCamera);
            _adsAttachmentManagerRuntimeBridge = attachmentManager;

            EnsureRenderTextureScopeRuntimeBridge(worldCamera);
            EnsurePeripheralScopeEffectsRuntimeBridge();
            EnsureScopeAdjustmentTooltipRuntimeBridge();
            _adsStateRuntimeBridge.BindRuntimeReferences(
                worldCamera,
                viewmodelCamera,
                attachmentManager,
                _renderTextureScopeRuntimeBridge,
                _peripheralScopeEffectsRuntimeBridge,
                _scopeAdjustmentTooltipRuntimeBridge);
            TryAssignScopedAdsWeaponDefinition();
            _adsStateRuntimeBridge.RefreshVisualMode();
            _adsStateRuntimeBridge.SetUseLegacyInput(false);
        }

        private void EnsureRenderTextureScopeRuntimeBridge(Camera worldCamera)
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            _renderTextureScopeRuntimeBridge = gameObject.GetComponent<GameRenderTextureScopeController>()
                ?? gameObject.AddComponent<GameRenderTextureScopeController>();
            if (_renderTextureScopeRuntimeBridge == null)
            {
                return;
            }

            var scopeCamera = EnsureScopeCamera(worldCamera);
            _renderTextureScopeRuntimeBridge.SetScopeCamera(scopeCamera);
        }

        private void EnsurePeripheralScopeEffectsRuntimeBridge()
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            _peripheralScopeEffectsRuntimeBridge = gameObject.GetComponent<GamePeripheralScopeEffects>()
                ?? gameObject.AddComponent<GamePeripheralScopeEffects>();
        }

        private void EnsureScopeAdjustmentTooltipRuntimeBridge()
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            _scopeAdjustmentTooltipRuntimeBridge = gameObject.GetComponent<GameScopeAdjustmentTooltipOverlay>()
                ?? gameObject.AddComponent<GameScopeAdjustmentTooltipOverlay>();
        }

        private void TryAssignScopedAdsWeaponDefinition()
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            var resolvedDefinition = ResolveGameWeaponDefinition(_equippedItemId);
            if (resolvedDefinition != null)
            {
                _adsStateRuntimeBridge.SetWeaponDefinition(resolvedDefinition);
            }
        }

        private static GameWeaponDefinition ResolveGameWeaponDefinition(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            var definitions = Resources.FindObjectsOfTypeAll<GameWeaponDefinition>();
            if (definitions == null || definitions.Length == 0)
            {
                return null;
            }

            Array.Sort(definitions, CompareObjectsDeterministically);
            for (var i = 0; i < definitions.Length; i++)
            {
                var candidate = definitions[i];
                if (candidate != null && string.Equals(candidate.WeaponId, weaponId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Camera ResolveViewmodelCamera(Camera worldCamera)
        {
            if (worldCamera == null)
            {
                return null;
            }

            var worldCameraTransform = worldCamera.transform;
            var sharedBasis = worldCameraTransform.parent != null ? worldCameraTransform.parent : worldCameraTransform;

            var sharedBasisViewmodelCamera = sharedBasis.Find("ViewmodelCamera")?.GetComponent<Camera>();
            if (sharedBasisViewmodelCamera != null)
            {
                return sharedBasisViewmodelCamera;
            }

            if (sharedBasis != worldCameraTransform)
            {
                var legacyChild = worldCameraTransform.Find("ViewmodelCamera")?.GetComponent<Camera>();
                if (legacyChild != null)
                {
                    return legacyChild;
                }
            }

            return null;
        }

        private static Camera EnsureScopeCamera(Camera worldCamera)
        {
            if (worldCamera == null)
            {
                return null;
            }

            var scopeTransform = worldCamera.transform.Find("ScopeCamera");
            Camera scopeCamera;
            var createdScopeCamera = false;
            if (scopeTransform != null)
            {
                scopeCamera = scopeTransform.GetComponent<Camera>();
            }
            else
            {
                var scopeCameraGo = new GameObject("ScopeCamera");
                scopeTransform = scopeCameraGo.transform;
                scopeTransform.SetParent(worldCamera.transform, false);
                scopeCamera = scopeCameraGo.AddComponent<Camera>();
                createdScopeCamera = true;
            }

            if (scopeCamera == null)
            {
                return null;
            }

            scopeTransform.localPosition = Vector3.zero;
            scopeTransform.localRotation = Quaternion.identity;
            scopeTransform.localScale = Vector3.one;

            scopeCamera.clearFlags = worldCamera.clearFlags;
            scopeCamera.backgroundColor = worldCamera.backgroundColor;
            scopeCamera.cullingMask = ExcludeViewmodelLayer(worldCamera.cullingMask);
            scopeCamera.nearClipPlane = worldCamera.nearClipPlane;
            scopeCamera.farClipPlane = worldCamera.farClipPlane;
            scopeCamera.allowHDR = worldCamera.allowHDR;
            scopeCamera.allowMSAA = worldCamera.allowMSAA;
            scopeCamera.orthographic = worldCamera.orthographic;
            scopeCamera.depthTextureMode = worldCamera.depthTextureMode;
            if (createdScopeCamera)
            {
                scopeCamera.enabled = false;
                scopeCamera.fieldOfView = worldCamera.fieldOfView;
                scopeCamera.targetTexture = null;
            }
            EnsureScopeCameraUniversalRenderPipelineData(scopeCamera);
            return scopeCamera;
        }

        private static void EnsureScopeCameraUniversalRenderPipelineData(Camera scopeCamera)
        {
            if (scopeCamera == null)
            {
                return;
            }

            var additionalCameraDataType = ResolveTypeByName("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (additionalCameraDataType == null || !typeof(Component).IsAssignableFrom(additionalCameraDataType))
            {
                return;
            }

            var additionalCameraData = scopeCamera.GetComponent(additionalCameraDataType)
                ?? scopeCamera.gameObject.AddComponent(additionalCameraDataType);
            var renderTypeProperty = additionalCameraDataType.GetProperty("renderType", BindingFlags.Instance | BindingFlags.Public);
            if (renderTypeProperty?.CanWrite == true)
            {
                renderTypeProperty.SetValue(additionalCameraData, Enum.Parse(renderTypeProperty.PropertyType, "Base"));
            }

            var cameraStackProperty = additionalCameraDataType.GetProperty("cameraStack", BindingFlags.Instance | BindingFlags.Public);
            var clearMethod = cameraStackProperty?.GetValue(additionalCameraData)?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
            clearMethod?.Invoke(cameraStackProperty.GetValue(additionalCameraData), null);
        }

        private static int ExcludeViewmodelLayer(int cullingMask)
        {
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            if (viewmodelLayer < 0)
            {
                return cullingMask;
            }

            return cullingMask & ~(1 << viewmodelLayer);
        }

        private static void ApplyViewmodelLayer(Transform root)
        {
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            if (root == null || viewmodelLayer < 0)
            {
                return;
            }

            SetLayerRecursively(root, viewmodelLayer);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void DestroyEquippedWeaponView()
        {
            ResetStableMagnifiedScopedAdsState();
            ResetScopedViewmodelStabilization();
            ResetAppliedAttachmentViewState();

            if (_equippedWeaponView == null)
            {
                DestroyScopedAdsRuntimeBridgeComponents();
                return;
            }

            if (transform.gameObject.activeInHierarchy && _equippedWeaponView.transform.parent != null)
            {
                _equippedWeaponView.transform.SetParent(null, false);
            }

            Destroy(_equippedWeaponView);
            _equippedWeaponView = null;
            DestroyScopedAdsRuntimeBridgeComponents();
        }

        private void DestroyScopedAdsRuntimeBridgeComponents()
        {
            ResetStableMagnifiedScopedAdsState();
            ResetScopedViewmodelStabilization();
            DestroyScopedBridgeComponent(_adsStateRuntimeBridge);
            DestroyScopedBridgeComponent(_renderTextureScopeRuntimeBridge);
            DestroyScopedBridgeComponent(_scopeAdjustmentTooltipRuntimeBridge);

            if (_peripheralScopeEffectsRuntimeBridge != null)
            {
                _peripheralScopeEffectsRuntimeBridge.SetState(false, 0f);
            }

            _adsStateRuntimeBridge = null;
            _adsAttachmentManagerRuntimeBridge = null;
            _renderTextureScopeRuntimeBridge = null;
            _peripheralScopeEffectsRuntimeBridge = null;
            _scopeAdjustmentTooltipRuntimeBridge = null;
            ResetScopedAdsLookSensitivityBridge();
        }

        private static void DestroyScopedBridgeComponent(Component component)
        {
            if (component != null)
            {
                Destroy(component);
            }
        }

        private static void ClearAttachmentSlots(WeaponRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            var slotValues = (WeaponAttachmentSlotType[])Enum.GetValues(typeof(WeaponAttachmentSlotType));
            for (var i = 0; i < slotValues.Length; i++)
            {
                state.SetEquippedAttachmentItemId(slotValues[i], string.Empty);
            }
        }

        private static GameObject InstantiateWeaponView(GameObject source, Transform parent)
        {
            if (source == null)
            {
                return null;
            }

            UObject instance;
            try
            {
                instance = Instantiate((UObject)source, parent);
            }
            catch (System.Exception)
            {
                return null;
            }

            if (instance is GameObject gameObjectInstance)
            {
                return gameObjectInstance;
            }

            if (instance is Component componentInstance)
            {
                return componentInstance.gameObject;
            }

            if (instance != null)
            {
                Destroy(instance);
            }

            return null;
        }

        private GameObject ResolveWeaponViewPrefab(string itemId)
        {
            itemId = NormalizeWeaponItemId(itemId);
            for (var i = 0; i < _weaponViewPrefabs.Length; i++)
            {
                var binding = _weaponViewPrefabs[i];
                if (!string.IsNullOrWhiteSpace(binding.ItemId)
                    && NormalizeWeaponItemId(binding.ItemId) == itemId
                    && binding.ViewPrefab != null)
                {
                    return binding.ViewPrefab;
                }
            }

            Debug.LogWarning(
                $"PlayerWeaponController: No explicit weapon view prefab binding exists for '{itemId}'. View spawn rejected.",
                this);
            return null;
        }

        private GameOpticDefinition ResolveOpticDefinition(string attachmentItemId)
        {
            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return null;
            }

            var metadataLookup = BuildAttachmentMetadataLookup();
            if (metadataLookup.TryGetValue(attachmentItemId, out var metadata)
                && metadata != null)
            {
                if (metadata.SlotType != WeaponAttachmentSlotType.Scope)
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Attachment metadata for '{attachmentItemId}' is bound to slot '{metadata.SlotType}' instead of Scope.",
                        this);
                    return null;
                }

                if (metadata.AttachmentDefinition is not GameOpticDefinition opticDefinition)
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Attachment metadata for '{attachmentItemId}' is missing a typed {typeof(GameOpticDefinition).Name} definition.",
                        this);
                    return null;
                }

                if (!string.Equals(opticDefinition.OpticId, attachmentItemId, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Optic definition '{opticDefinition.name}' has OpticId '{opticDefinition.OpticId}' but metadata expects '{attachmentItemId}'.",
                        opticDefinition);
                    return null;
                }

                if (opticDefinition.OpticPrefab == null)
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Optic definition '{opticDefinition.name}' for '{attachmentItemId}' is missing OpticPrefab.",
                        opticDefinition);
                    return null;
                }

                return opticDefinition;
            }

            return null;
        }

        private GameMuzzleAttachmentDefinition ResolveMuzzleAttachmentDefinition(string attachmentItemId)
        {
            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return null;
            }

            var metadataLookup = BuildAttachmentMetadataLookup();
            if (metadataLookup.TryGetValue(attachmentItemId, out var metadata)
                && metadata != null)
            {
                if (metadata.SlotType != WeaponAttachmentSlotType.Muzzle)
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Attachment metadata for '{attachmentItemId}' is bound to slot '{metadata.SlotType}' instead of Muzzle.",
                        this);
                    return null;
                }

                if (metadata.AttachmentDefinition is not GameMuzzleAttachmentDefinition muzzleDefinition)
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Attachment metadata for '{attachmentItemId}' is missing a typed {typeof(GameMuzzleAttachmentDefinition).Name} definition.",
                        this);
                    return null;
                }

                if (!string.Equals(muzzleDefinition.AttachmentId, attachmentItemId, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Muzzle definition '{muzzleDefinition.name}' has AttachmentId '{muzzleDefinition.AttachmentId}' but metadata expects '{attachmentItemId}'.",
                        muzzleDefinition);
                    return null;
                }

                if (muzzleDefinition.MuzzlePrefab == null)
                {
                    Debug.LogWarning(
                        $"PlayerWeaponController: Muzzle definition '{muzzleDefinition.name}' for '{attachmentItemId}' is missing MuzzlePrefab.",
                        muzzleDefinition);
                    return null;
                }

                return muzzleDefinition;
            }

            return null;
        }

        private bool TryResolveWeaponDefinition(string itemId, out WeaponDefinition definition)
        {
            itemId = NormalizeWeaponItemId(itemId);
            definition = ResolveWeaponDefinition(itemId);
            return definition != null;
        }

        private Transform ResolveDefaultWeaponViewParent()
        {
            return ResolveDefaultWeaponViewParent(null);
        }

        private Transform ResolveDefaultWeaponViewParent(Transform resolvedViewmodelRoot)
        {
            var cameraPivot = ResolveCameraPivot(resolvedViewmodelRoot);
            if (cameraPivot == null)
            {
                return null;
            }

            var weaponPresentationRoot = cameraPivot.Find(WeaponPresentationRootName);
            if (weaponPresentationRoot == null)
            {
                var rootGo = new GameObject(WeaponPresentationRootName);
                weaponPresentationRoot = rootGo.transform;
                weaponPresentationRoot.SetParent(cameraPivot, false);
                weaponPresentationRoot.localPosition = Vector3.zero;
                weaponPresentationRoot.localRotation = Quaternion.identity;
                weaponPresentationRoot.localScale = Vector3.one;
            }

            ApplyViewmodelLayer(weaponPresentationRoot);
            return weaponPresentationRoot;
        }

        private Transform ResolveCameraPivot(Transform resolvedViewmodelRoot)
        {
            var explicitPath = transform.Find(CameraPivotName);
            if (explicitPath != null)
            {
                return explicitPath;
            }

            var viewmodelRoot = resolvedViewmodelRoot != null && IsReferenceOnPlayerHierarchy(resolvedViewmodelRoot)
                ? resolvedViewmodelRoot
                : ResolveViewmodelRoot();

            return viewmodelRoot != null && string.Equals(viewmodelRoot.parent?.name, CameraPivotName, StringComparison.Ordinal)
                ? viewmodelRoot.parent
                : null;
        }

        private Transform ResolveViewmodelRoot()
        {
            var explicitPath = transform.Find("CameraPivot/PlayerArms");
            if (explicitPath != null)
            {
                return explicitPath;
            }

            if (_packAnimator == null)
            {
                _packAnimator = ResolvePackAnimator();
            }

            var current = _packAnimator != null ? _packAnimator.transform : null;
            while (current != null)
            {
                if (string.Equals(current.name, PlayerArmsRootName, StringComparison.Ordinal))
                {
                    return current;
                }

                current = current.parent;
            }

            return FindDescendantByName(transform, PlayerArmsRootName);
        }

        private Animator ResolvePackAnimator()
        {
            var explicitPath = transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual");
            if (explicitPath != null)
            {
                var explicitAnimator = explicitPath.GetComponent<Animator>() ?? explicitPath.GetComponentInChildren<Animator>(true);
                if (explicitAnimator != null)
                {
                    return explicitAnimator;
                }
            }

            var byName = FindDescendantByName(transform, "PlayerArmsVisual");
            if (byName != null)
            {
                var namedAnimator = byName.GetComponent<Animator>() ?? byName.GetComponentInChildren<Animator>(true);
                if (namedAnimator != null)
                {
                    return namedAnimator;
                }
            }

            return GetComponentInChildren<Animator>(true);
        }

        private bool IsReferenceOnPlayerHierarchy(Transform candidate)
        {
            return candidate != null && (candidate == transform || candidate.IsChildOf(transform));
        }

        private bool IsWeaponViewParentUsable(Transform candidate)
        {
            return IsReferenceOnPlayerHierarchy(candidate)
                && string.Equals(candidate.name, WeaponPresentationRootName, StringComparison.Ordinal)
                && string.Equals(candidate.parent?.name, CameraPivotName, StringComparison.Ordinal);
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendantByName(root.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void StripViewPhysicsComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Destroy(colliders[i]);
                }
            }

            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                {
                    Destroy(rigidbodies[i]);
                }
            }
        }

        private static void StripViewRuntimeComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var animators = root.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null)
                {
                    Destroy(animators[i]);
                }
            }

            var animations = root.GetComponentsInChildren<Animation>(true);
            for (var i = 0; i < animations.Length; i++)
            {
                if (animations[i] != null)
                {
                    Destroy(animations[i]);
                }
            }

            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is WeaponViewAttachmentMounts
                    || behaviour is WeaponViewPoseTuningHelper
                    || behaviour is WeaponViewHandAnchors
                    || behaviour is GameMuzzleAttachmentRuntime
                    || behaviour is GameDetachableMagazineRuntime
                    || behaviour is GameAttachmentManager)
                {
                    continue;
                }

                // Weapon view instances should be pure visual meshes/sockets.
                Destroy(behaviour);
            }
        }

        private static void NormalizeViewMaterialsForActiveRenderPipeline(GameObject viewRoot)
        {
            if (viewRoot == null)
            {
                return;
            }

            var fallbackShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (fallbackShader == null)
            {
                return;
            }

            var renderers = viewRoot.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var sourceMaterials = renderer.sharedMaterials;
                var replaced = false;
                for (var m = 0; m < sourceMaterials.Length; m++)
                {
                    var source = sourceMaterials[m];
                    if (source == null)
                    {
                        continue;
                    }

                    var shader = source.shader;
                    var shaderName = shader != null ? shader.name : string.Empty;
                    var requiresUpgrade =
                        shader == null
                        || !shader.isSupported
                        || string.Equals(shaderName, "Hidden/InternalErrorShader", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(shaderName, "Standard", StringComparison.OrdinalIgnoreCase)
                        || shaderName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase);

                    if (!requiresUpgrade)
                    {
                        continue;
                    }

                    var sourceId = source.GetInstanceID();
                    if (!MaterialUpgradeCacheBySourceId.TryGetValue(sourceId, out var replacement) || replacement == null)
                    {
                        replacement = new Material(fallbackShader);

                        if (source.HasProperty("_BaseMap") && replacement.HasProperty("_BaseMap"))
                        {
                            replacement.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                        }
                        else if (source.HasProperty("_MainTex"))
                        {
                            var tex = source.GetTexture("_MainTex");
                            if (replacement.HasProperty("_BaseMap"))
                            {
                                replacement.SetTexture("_BaseMap", tex);
                            }
                            else if (replacement.HasProperty("_MainTex"))
                            {
                                replacement.SetTexture("_MainTex", tex);
                            }
                        }

                        if (source.HasProperty("_BaseColor") && replacement.HasProperty("_BaseColor"))
                        {
                            replacement.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                        }
                        else if (source.HasProperty("_Color"))
                        {
                            var color = source.GetColor("_Color");
                            if (replacement.HasProperty("_BaseColor"))
                            {
                                replacement.SetColor("_BaseColor", color);
                            }
                            else if (replacement.HasProperty("_Color"))
                            {
                                replacement.SetColor("_Color", color);
                            }
                        }

                        var sourceTransparent =
                            source.renderQueue >= 3000
                            || (source.HasProperty("_Mode") && source.GetFloat("_Mode") >= 2.5f);
                        if (sourceTransparent)
                        {
                            if (replacement.HasProperty("_Surface"))
                            {
                                replacement.SetFloat("_Surface", 1f);
                            }

                            if (replacement.HasProperty("_Blend"))
                            {
                                replacement.SetFloat("_Blend", 0f);
                            }

                            replacement.renderQueue = 3000;
                        }

                        MaterialUpgradeCacheBySourceId[sourceId] = replacement;
                    }

                    sourceMaterials[m] = replacement;
                    replaced = true;
                }

                if (replaced)
                {
                    renderer.sharedMaterials = sourceMaterials;
                }
            }
        }

        private static int CompareObjectsDeterministically(UObject left, UObject right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var nameComparison = string.Compare(left.name, right.name, StringComparison.Ordinal);
            return nameComparison != 0
                ? nameComparison
                : left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private PackWeaponPresentationConfig ResolvePackPresentationConfig(string itemId, WeaponDefinition definition = null)
        {
            itemId = NormalizeWeaponItemId(itemId);
            var fallbackConfig = _packPresentationConfig ?? new PackWeaponPresentationConfig();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return fallbackConfig;
            }

            var resolvedDefinition = definition ?? ResolveWeaponDefinition(itemId);
            return resolvedDefinition != null
                ? resolvedDefinition.ResolvePackPresentationConfig(fallbackConfig)
                : fallbackConfig;
        }

        private Camera ResolveAdsCamera()
        {
            var activeCamera = _adsCamera != null ? _adsCamera : Camera.main;
            if (activeCamera == null && (_cameraDefaults == null || !_cameraDefaults.TryGetEffectiveFieldOfView(out _)))
            {
                return null;
            }

            if (!_baseCameraFieldOfViewCaptured || _cachedAdsCamera != activeCamera)
            {
                _cachedAdsCamera = activeCamera;
                if (_cameraDefaults != null && _cameraDefaults.TryGetEffectiveFieldOfView(out var defaultsFov))
                {
                    _baseCameraFieldOfView = defaultsFov > 1f ? defaultsFov : DefaultFov;
                }
                else
                {
                    _baseCameraFieldOfView = activeCamera != null && activeCamera.fieldOfView > 1f ? activeCamera.fieldOfView : DefaultFov;
                }
                _baseCameraFieldOfViewCaptured = true;
            }

            return activeCamera;
        }

        private bool TryGetCurrentFieldOfView(out float fieldOfView)
        {
            ResolveAdsCamera();
            if (_cameraDefaults != null && _cameraDefaults.TryGetEffectiveFieldOfView(out fieldOfView))
            {
                return true;
            }

            var camera = _cachedAdsCamera ?? _adsCamera ?? Camera.main;
            if (camera != null)
            {
                fieldOfView = camera.fieldOfView;
                return true;
            }

            fieldOfView = default;
            return false;
        }

        private bool TrySetCurrentFieldOfView(float fieldOfView)
        {
            if (_cameraDefaults != null && _cameraDefaults.TrySetEffectiveFieldOfView(fieldOfView))
            {
                return true;
            }

            var camera = _cachedAdsCamera ?? _adsCamera ?? Camera.main;
            if (camera == null)
            {
                return false;
            }

            camera.fieldOfView = fieldOfView;
            return true;
        }

        private WeaponDefinition ResolveWeaponDefinition(string itemId)
        {
            itemId = NormalizeWeaponItemId(itemId);
            if (string.IsNullOrWhiteSpace(itemId) || _weaponRegistry == null)
            {
                return null;
            }

            return _weaponRegistry.TryGetWeaponDefinition(itemId, out var definition) ? definition : null;
        }

        private static string NormalizeWeaponItemId(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId;
        }

    }
}
