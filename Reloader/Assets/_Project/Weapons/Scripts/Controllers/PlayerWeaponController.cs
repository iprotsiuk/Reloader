using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Data;
using Reloader.Weapons.PackRuntime;
using Reloader.Weapons.Runtime;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using URandom = UnityEngine.Random;
using UObject = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private const float FeetToMeters = 0.3048f;
        private const float DefaultFov = 60f;

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
        [SerializeField] private bool _allowSceneWideDependencyLookup;

        private IPlayerInputSource _inputSource;
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
        private Component _adsStateRuntimeBridge;
        private Component _adsAttachmentManagerRuntimeBridge;
        private Component _weaponAimAlignerRuntimeBridge;
        private PlayerLookController _playerLookControllerRuntimeBridge;
        private float _scopedAdsPresentationEyeReliefOffset;
        private PropertyInfo _adsActiveOpticProperty;
        private PropertyInfo _adsBlendProperty;
        private PropertyInfo _adsCurrentSensitivityScaleProperty;
        private MethodInfo _adsSetHeldMethod;
        private MethodInfo _adsSetMagnificationMethod;
        private MethodInfo _adsApplyScopeAdjustmentInputMethod;
        private PropertyInfo _adsCurrentMagnificationProperty;
        private float _cachedScopeMagnification = 1f;
        private string _pendingEquipItemId;
        private WeaponDefinition _pendingEquipDefinition;
        private float _pendingEquipApplyTime;
        [SerializeField, Min(0f)] private float _holsterHideDelaySeconds = 0.2f;
        private float _scheduledArmsHideTime = -1f;
        private readonly List<Renderer> _packRenderers = new List<Renderer>();
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

        private void OnDisable()
        {
            DestroyEquippedWeaponView();
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

            var applied = ApplyEquippedAttachmentSlotToViewRuntime(slotType, state.GetEquippedAttachmentItemId(slotType));
            if (!applied)
            {
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

                return false;
            }

            ResolveInventoryEvents()?.RaiseInventoryChanged();
            return true;
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

            if (!IsReferenceOnPlayerHierarchy(_packAnimator != null ? _packAnimator.transform : null))
            {
                _packAnimator = ResolvePackAnimator();
                RefreshPackRenderers();
            }


            if (!IsReferenceOnPlayerHierarchy(_weaponViewParent))
            {
                _weaponViewParent = ResolveDefaultWeaponViewParent();
            }
        }

        private void UpdateEquipFromSelection()
        {
            var selectedItemId = _inventoryController.Runtime != null
                ? NormalizeWeaponItemId(_inventoryController.Runtime.SelectedBeltItemId)
                : null;
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

            var needsResync = _equippedWeaponView == null
                              || _adsAttachmentManagerRuntimeBridge == null
                              || _weaponAimAlignerRuntimeBridge == null;
            if (!needsResync)
            {
                var scopedAttachmentItemId = state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope);
                needsResync = !string.IsNullOrWhiteSpace(scopedAttachmentItemId) && !HasActiveScopedAdsAlignment;
            }

            if (needsResync)
            {
                ResyncEquippedViewFromRuntimeState(state, rebuildView: _equippedWeaponView == null);
            }
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
                ballisticSpec.BallisticCoefficientG1,
                transform);

            NotifyViewWeaponFired(_equippedItemId);
            var muzzleAudioOverride = ResolveMuzzleAudioOverride();
            ResolveWeaponEvents()?.RaiseWeaponFired(_equippedItemId, _muzzleTransform.position, firedDirection);
            ResolveCombatAudioEmitter()?.EmitWeaponFire(_equippedItemId, _muzzleTransform.position, muzzleAudioOverride);
        }

        private static bool IsFireInputBlocked()
        {
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

        private void TickReload()
        {
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

        private bool HasScopedAdsBridgeActive()
        {
            if (_adsStateRuntimeBridge == null || _adsAttachmentManagerRuntimeBridge == null)
            {
                return false;
            }

            var managerType = _adsAttachmentManagerRuntimeBridge.GetType();
            _adsActiveOpticProperty ??= managerType.GetProperty("ActiveOpticDefinition", BindingFlags.Instance | BindingFlags.Public);
            if (_adsActiveOpticProperty == null)
            {
                return false;
            }

            return _adsActiveOpticProperty.GetValue(_adsAttachmentManagerRuntimeBridge) != null;
        }

        public bool HasActiveScopedAdsAlignment => HasScopedAdsBridgeActive() && _weaponAimAlignerRuntimeBridge != null;
        public float ScopedAdsPresentationEyeReliefOffset => _scopedAdsPresentationEyeReliefOffset;

        public void SetScopedAdsPresentationEyeReliefOffset(float value)
        {
            _scopedAdsPresentationEyeReliefOffset = value;
            ApplyScopedAdsPresentationEyeReliefOffset();
        }

        private float ResolveCurrentAdsBlendT()
        {
            if (_adsStateRuntimeBridge != null)
            {
                var bridgeType = _adsStateRuntimeBridge.GetType();
                _adsBlendProperty ??= bridgeType.GetProperty("AdsT", BindingFlags.Instance | BindingFlags.Public);
                if (_adsBlendProperty != null && _adsBlendProperty.GetValue(_adsStateRuntimeBridge) is float adsBlend)
                {
                    return Mathf.Clamp01(adsBlend);
                }
            }

            return _inputSource != null && _inputSource.AimHeld ? 1f : 0f;
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

        private UObject ResolveActiveOpticDefinition()
        {
            if (_adsAttachmentManagerRuntimeBridge == null)
            {
                return null;
            }

            var managerType = _adsAttachmentManagerRuntimeBridge.GetType();
            _adsActiveOpticProperty ??= managerType.GetProperty("ActiveOpticDefinition", BindingFlags.Instance | BindingFlags.Public);
            return _adsActiveOpticProperty?.GetValue(_adsAttachmentManagerRuntimeBridge) as UObject;
        }

        private UObject ResolveEquippedScopeDefinitionFromState()
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

        private static bool TryReadOpticMagnification(UObject opticDefinition, out float minMagnification, out float maxMagnification)
        {
            minMagnification = 1f;
            maxMagnification = 1f;
            if (opticDefinition == null)
            {
                return false;
            }

            var opticType = opticDefinition.GetType();
            var minProperty = opticType.GetProperty("MagnificationMin", BindingFlags.Instance | BindingFlags.Public);
            var maxProperty = opticType.GetProperty("MagnificationMax", BindingFlags.Instance | BindingFlags.Public);
            if (minProperty == null || maxProperty == null)
            {
                return false;
            }

            if (minProperty.GetValue(opticDefinition) is not float minValue
                || maxProperty.GetValue(opticDefinition) is not float maxValue)
            {
                return false;
            }

            minMagnification = minValue;
            maxMagnification = maxValue;
            return true;
        }

        private void TickScopedAdsBridgeInput()
        {
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            _adsSetHeldMethod ??= _adsStateRuntimeBridge.GetType().GetMethod("SetAdsHeld", BindingFlags.Instance | BindingFlags.Public);
            _adsSetMagnificationMethod ??= _adsStateRuntimeBridge.GetType().GetMethod("SetMagnification", BindingFlags.Instance | BindingFlags.Public);
            _adsApplyScopeAdjustmentInputMethod ??= _adsStateRuntimeBridge.GetType().GetMethod("ApplyScopeAdjustmentInput", BindingFlags.Instance | BindingFlags.Public);
            _adsCurrentMagnificationProperty ??= _adsStateRuntimeBridge.GetType().GetProperty("CurrentMagnification", BindingFlags.Instance | BindingFlags.Public);

            _adsSetHeldMethod?.Invoke(_adsStateRuntimeBridge, new object[] { _inputSource != null && _inputSource.AimHeld });
            if (_adsSetMagnificationMethod == null || _adsCurrentMagnificationProperty == null)
            {
                return;
            }

            if (_adsCurrentMagnificationProperty.GetValue(_adsStateRuntimeBridge) is float currentMagnification)
            {
                _cachedScopeMagnification = currentMagnification;
            }

            TryApplyScopedAdjustmentInput();

            var scrollY = _inputSource != null ? _inputSource.ConsumeZoomInput() : 0f;
            if (Mathf.Abs(scrollY) <= 0.01f)
            {
                return;
            }

            var nextMagnification = _cachedScopeMagnification + scrollY;
            _adsSetMagnificationMethod.Invoke(_adsStateRuntimeBridge, new object[] { nextMagnification });
        }

        private void SyncScopedAdsLookSensitivityBridge()
        {
            _playerLookControllerRuntimeBridge ??= GetComponent<PlayerLookController>();
            if (_playerLookControllerRuntimeBridge == null || _adsStateRuntimeBridge == null)
            {
                return;
            }

            if (!UsesRenderTexturePipOptic(ResolveActiveOpticDefinition()))
            {
                ResetScopedAdsLookSensitivityBridge();
                return;
            }

            _adsCurrentSensitivityScaleProperty ??= _adsStateRuntimeBridge.GetType()
                .GetProperty("CurrentSensitivityScale", BindingFlags.Instance | BindingFlags.Public);
            if (_adsCurrentSensitivityScaleProperty?.GetValue(_adsStateRuntimeBridge) is not float sensitivityScale)
            {
                ResetScopedAdsLookSensitivityBridge();
                return;
            }

            var clampedScale = Mathf.Max(0.001f, sensitivityScale);
            _playerLookControllerRuntimeBridge.RuntimeAdsSensitivityMultiplier = new Vector2(clampedScale, clampedScale);
        }

        private void ResetScopedAdsLookSensitivityBridge()
        {
            _playerLookControllerRuntimeBridge ??= GetComponent<PlayerLookController>();
            if (_playerLookControllerRuntimeBridge == null)
            {
                return;
            }

            _playerLookControllerRuntimeBridge.RuntimeAdsSensitivityMultiplier = Vector2.one;
        }

        private static bool UsesRenderTexturePipOptic(UObject opticDefinition)
        {
            if (opticDefinition == null)
            {
                return false;
            }

            var property = opticDefinition.GetType().GetProperty("VisualModePolicy", BindingFlags.Instance | BindingFlags.Public);
            var value = property?.GetValue(opticDefinition);
            return string.Equals(value?.ToString(), "RenderTexturePiP", StringComparison.Ordinal);
        }

        private void TryApplyScopedAdjustmentInput()
        {
            if (_adsStateRuntimeBridge == null || _adsApplyScopeAdjustmentInputMethod == null)
            {
                return;
            }

            if (!TryReadScopedAdjustmentKeyInput(out var windageClicks, out var elevationClicks))
            {
                return;
            }

            _adsApplyScopeAdjustmentInputMethod.Invoke(_adsStateRuntimeBridge, new object[] { windageClicks, elevationClicks });
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
                    windageClicks += 1;
                }
                else
                {
                    elevationClicks += 1;
                }
            }

            if (equalsPressed)
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
            if (_equippedWeaponView == null)
            {
                return null;
            }

            var behaviors = _equippedWeaponView.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviors.Length; i++)
            {
                var behavior = behaviors[i];
                if (behavior == null || behavior.GetType().Name != "MuzzleAttachmentRuntime")
                {
                    continue;
                }

                var method = behavior.GetType().GetMethod("TryGetFireClipOverride", BindingFlags.Instance | BindingFlags.Public);
                if (method == null)
                {
                    continue;
                }

                var result = method.Invoke(behavior, null);
                if (result is AudioClip clip)
                {
                    return clip;
                }
            }

            return null;
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

            var muzzleRuntimeType = ResolveTypeByName("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            if (muzzleRuntimeType == null)
            {
                return;
            }

            var runtimeComponent = viewRoot.GetComponent(muzzleRuntimeType) ?? viewRoot.AddComponent(muzzleRuntimeType);
            Transform attachmentSlot = null;
            if (mounts != null)
            {
                mounts.TryGetAttachmentSlot(WeaponAttachmentSlotType.Muzzle, out attachmentSlot);
            }

            attachmentSlot ??= FindDescendantByName(viewRoot.transform, "MuzzleAttachmentSlot")
                ?? viewMuzzle;

            var muzzleSocketField = muzzleRuntimeType.GetField("_muzzleSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            muzzleSocketField?.SetValue(runtimeComponent, viewMuzzle);

            var attachmentSlotField = muzzleRuntimeType.GetField("_attachmentSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            attachmentSlotField?.SetValue(runtimeComponent, attachmentSlot);

            var defaultAttachmentField = muzzleRuntimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
            defaultAttachmentField?.SetValue(runtimeComponent, null);

            var unequipMethod = muzzleRuntimeType.GetMethod("Unequip", BindingFlags.Instance | BindingFlags.Public);
            unequipMethod?.Invoke(runtimeComponent, null);
        }

        private void EnsureDetachableMagazineRuntimeBridge(GameObject viewRoot, WeaponViewAttachmentMounts mounts)
        {
            if (viewRoot == null)
            {
                return;
            }

            var runtimeType = ResolveTypeByName("Reloader.Game.Weapons.DetachableMagazineRuntime");
            if (runtimeType == null)
            {
                return;
            }

            var runtimeComponent = viewRoot.GetComponent(runtimeType) ?? viewRoot.AddComponent(runtimeType);
            var magazineSocket = mounts != null
                ? mounts.MagazineSocket
                : FindDescendantByName(viewRoot.transform, "MagazineSocket")
                    ?? FindDescendantByName(viewRoot.transform, "SOCKET_Magazine")
                    ?? FindDescendantByName(viewRoot.transform, "Muzzle");
            var dropSocket = mounts != null && mounts.MagazineDropSocket != null
                ? mounts.MagazineDropSocket
                : FindDescendantByName(viewRoot.transform, "MagazineDropSocket") ?? magazineSocket;
            if (magazineSocket == null)
            {
                return;
            }

            var magazineSocketField = runtimeType.GetField("_magazineSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            magazineSocketField?.SetValue(runtimeComponent, magazineSocket);

            var dropSocketField = runtimeType.GetField("_magazineDropSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            dropSocketField?.SetValue(runtimeComponent, dropSocket);

            var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
            defaultAttachmentField?.SetValue(runtimeComponent, null);

            var setAttachmentMethod = runtimeType.GetMethod("SetAttachment", BindingFlags.Instance | BindingFlags.Public);
            setAttachmentMethod?.Invoke(runtimeComponent, new object[] { null });
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

        private UObject ResolveAttachmentDefinitionByMetadata(
            string attachmentItemId,
            Type expectedDefinitionType,
            string idPropertyName)
        {
            if (string.IsNullOrWhiteSpace(attachmentItemId) || expectedDefinitionType == null)
            {
                return null;
            }

            var metadataLookup = BuildAttachmentMetadataLookup();
            if (metadataLookup.TryGetValue(attachmentItemId, out var metadata)
                && metadata != null
                && metadata.AttachmentDefinition != null
                && expectedDefinitionType.IsInstanceOfType(metadata.AttachmentDefinition)
                && IsDefinitionPrefabReferenceUsable(metadata.AttachmentDefinition, expectedDefinitionType, idPropertyName))
            {
                return metadata.AttachmentDefinition;
            }

            return ResolveAttachmentDefinitionById(
                expectedDefinitionType,
                idPropertyName,
                attachmentItemId,
                GetPrefabPropertyNameForDefinition(expectedDefinitionType, idPropertyName));
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
            var parent = IsReferenceOnPlayerHierarchy(_weaponViewParent) ? _weaponViewParent : null;
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
            ApplyViewmodelLayer(_equippedWeaponView.transform);
            StripViewPhysicsComponents(_equippedWeaponView);
            StripViewRuntimeComponents(_equippedWeaponView);
            NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);

            var mounts = _equippedWeaponView.GetComponent<WeaponViewAttachmentMounts>();
            if (mounts == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{_equippedWeaponView.name}' is missing WeaponViewAttachmentMounts.", this);
            }

            var viewMuzzle = mounts?.MuzzleTransform
                ?? FindDescendantByName(_equippedWeaponView.transform, "Muzzle")
                ?? FindDescendantByName(_equippedWeaponView.transform, "SOCKET_Muzzle");
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

            ApplyEquippedAttachmentStateToViewRuntime(state);
        }

        private void ApplyEquippedAttachmentStateToViewRuntime(WeaponRuntimeState state)
        {
            if (state == null || _equippedWeaponView == null)
            {
                return;
            }

            ApplyEquippedAttachmentSlotToViewRuntime(WeaponAttachmentSlotType.Scope, state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope));
            ApplyEquippedAttachmentSlotToViewRuntime(WeaponAttachmentSlotType.Muzzle, state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Muzzle));
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

            var managerType = ResolveTypeByName("Reloader.Game.Weapons.AttachmentManager");
            var opticDefinitionType = ResolveTypeByName("Reloader.Game.Weapons.OpticDefinition");
            if (managerType == null || opticDefinitionType == null)
            {
                return false;
            }

            var manager = EnsureAttachmentManagerRuntimeBridge(_equippedWeaponView);
            if (manager == null)
            {
                return false;
            }

            var unequipMethod = managerType.GetMethod("UnequipOptic", BindingFlags.Instance | BindingFlags.Public);
            var equipMethod = managerType.GetMethod("EquipOptic", BindingFlags.Instance | BindingFlags.Public);
            var setPendingAdjustmentStateKeyMethod = managerType.GetMethod("SetPendingOpticAdjustmentStateKey", BindingFlags.Instance | BindingFlags.Public);
            if (unequipMethod == null || equipMethod == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                unequipMethod.Invoke(manager, null);
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            var definition = ResolveOpticDefinition(attachmentItemId);
            if (definition == null)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: Scope definition resolve failed for attachmentItemId='{attachmentItemId}'.",
                    this);
                return false;
            }

            var opticPrefabProperty = opticDefinitionType.GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            var opticPrefabObject = opticPrefabProperty?.GetValue(definition) as UObject;
            if (opticPrefabObject == null)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: Scope definition '{definition.name}' ({definition.GetType().FullName}) has null OpticPrefab for attachmentItemId='{attachmentItemId}'.",
                    this);
            }

            setPendingAdjustmentStateKeyMethod?.Invoke(manager, new object[] { attachmentItemId });
            var equipSucceeded = equipMethod.Invoke(manager, new object[] { definition }) is bool equipResult && equipResult;
            if (!equipSucceeded)
            {
                Debug.LogWarning(
                    $"PlayerWeaponController: EquipOptic returned failure for attachmentItemId='{attachmentItemId}', definition='{definition.name}' ({definition.GetType().FullName}), opticPrefab='{opticPrefabObject?.name ?? "<null>"}' ({opticPrefabObject?.GetType().FullName ?? "<null>"}).",
                    this);
            }

            EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
            NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
            return equipSucceeded;
        }

        private bool ApplyMuzzleAttachmentToViewRuntime(string attachmentItemId)
        {
            if (_equippedWeaponView == null)
            {
                return false;
            }

            var managerType = ResolveTypeByName("Reloader.Game.Weapons.AttachmentManager");
            if (managerType == null)
            {
                return false;
            }

            var manager = EnsureAttachmentManagerRuntimeBridge(_equippedWeaponView);
            if (manager == null)
            {
                return false;
            }

            var unequipMethod = managerType.GetMethod("UnequipMuzzle", BindingFlags.Instance | BindingFlags.Public);
            var equipMethod = managerType.GetMethod("EquipMuzzle", BindingFlags.Instance | BindingFlags.Public);
            if (unequipMethod == null || equipMethod == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                unequipMethod.Invoke(manager, null);
                EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
                NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
                return true;
            }

            var definition = ResolveMuzzleAttachmentDefinition(attachmentItemId);
            if (definition == null)
            {
                return false;
            }

            var equipSucceeded = equipMethod.Invoke(manager, new object[] { definition }) is bool equipResult && equipResult;
            EnsureScopedAdsRuntimeBridge(_equippedWeaponView, manager);
            NormalizeViewMaterialsForActiveRenderPipeline(_equippedWeaponView);
            return equipSucceeded;
        }

        private Component EnsureAttachmentManagerRuntimeBridge(GameObject viewRoot)
        {
            if (viewRoot == null)
            {
                return null;
            }

            var managerType = ResolveTypeByName("Reloader.Game.Weapons.AttachmentManager");
            if (managerType == null)
            {
                return null;
            }

            var manager = viewRoot.GetComponent(managerType);
            if (manager == null)
            {
                manager = viewRoot.AddComponent(managerType);
            }

            var mounts = viewRoot.GetComponent<WeaponViewAttachmentMounts>();
            if (mounts == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{viewRoot.name}' is missing WeaponViewAttachmentMounts.", this);
                return null;
            }

            var ironSightAnchor = mounts.IronSightAnchor;
            if (ironSightAnchor == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{viewRoot.name}' is missing an authored IronSightAnchor.", this);
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

            var configureMethod = managerType.GetMethod("ConfigureMounts", BindingFlags.Instance | BindingFlags.Public);
            if (configureMethod == null)
            {
                return null;
            }

            var muzzleRuntimeType = ResolveTypeByName("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var muzzleRuntime = muzzleRuntimeType != null ? viewRoot.GetComponent(muzzleRuntimeType) : null;
            configureMethod.Invoke(manager, new object[] { scopeSlot, ironSightAnchor, muzzleSlot, muzzleRuntime });
            return manager;
        }

        private void EnsureScopedAdsRuntimeBridge(GameObject viewRoot, Component attachmentManager)
        {
            if (viewRoot == null || attachmentManager == null)
            {
                _adsStateRuntimeBridge = null;
                _weaponAimAlignerRuntimeBridge = null;
                _scopedAdsPresentationEyeReliefOffset = 0f;
                _adsActiveOpticProperty = null;
                return;
            }

            var adsType = ResolveTypeByName("Reloader.Game.Weapons.AdsStateController");
            if (adsType == null)
            {
                _adsStateRuntimeBridge = null;
                _weaponAimAlignerRuntimeBridge = null;
                _scopedAdsPresentationEyeReliefOffset = 0f;
                _adsActiveOpticProperty = null;
                return;
            }

            _adsStateRuntimeBridge = gameObject.GetComponent(adsType) ?? gameObject.AddComponent(adsType);
            if (_adsStateRuntimeBridge == null)
            {
                return;
            }

            var worldCamera = ResolveAdsCamera();
            var viewmodelCamera = ResolveViewmodelCamera(worldCamera);

            var worldField = adsType.GetField("_worldCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            worldField?.SetValue(_adsStateRuntimeBridge, worldCamera);

            var viewmodelField = adsType.GetField("_viewmodelCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            viewmodelField?.SetValue(_adsStateRuntimeBridge, viewmodelCamera);

            var managerField = adsType.GetField("_attachmentManager", BindingFlags.Instance | BindingFlags.NonPublic);
            managerField?.SetValue(_adsStateRuntimeBridge, attachmentManager);
            _adsAttachmentManagerRuntimeBridge = attachmentManager;
            _adsActiveOpticProperty = null;

            EnsureWeaponAimAlignerRuntimeBridge(viewRoot, attachmentManager, adsType, worldCamera);
            EnsureRenderTextureScopeRuntimeBridge(adsType, worldCamera);
            EnsurePeripheralScopeEffectsRuntimeBridge(adsType);
            EnsureScopeAdjustmentTooltipRuntimeBridge(adsType);
            TryAssignScopedAdsWeaponDefinition(adsType);

            var legacyInputField = adsType.GetField("_useLegacyInput", BindingFlags.Instance | BindingFlags.NonPublic);
            legacyInputField?.SetValue(_adsStateRuntimeBridge, false);
        }

        private void EnsureWeaponAimAlignerRuntimeBridge(GameObject viewRoot, Component attachmentManager, Type adsType, Camera worldCamera)
        {
            _weaponAimAlignerRuntimeBridge = null;
            if (viewRoot == null || attachmentManager == null || adsType == null)
            {
                return;
            }

            var alignerType = ResolveTypeByName("Reloader.Game.Weapons.WeaponAimAligner");
            if (alignerType == null)
            {
                return;
            }

            var mounts = viewRoot.GetComponent<WeaponViewAttachmentMounts>();
            var adsPivot = mounts != null ? mounts.AdsPivot : null;
            if (adsPivot == null)
            {
                Debug.LogWarning($"PlayerWeaponController: View '{viewRoot.name}' is missing an authored AdsPivot required for scoped ADS alignment.", this);
                return;
            }

            var aligner = gameObject.GetComponent(alignerType) ?? gameObject.AddComponent(alignerType);
            if (aligner == null)
            {
                return;
            }

            var cameraTransform = worldCamera != null ? worldCamera.transform : null;
            var alignerAdsPivotField = alignerType.GetField("_adsPivot", BindingFlags.Instance | BindingFlags.NonPublic);
            var alignerCameraField = alignerType.GetField("_cameraTransform", BindingFlags.Instance | BindingFlags.NonPublic);
            var alignerAttachmentManagerField = alignerType.GetField("_attachmentManager", BindingFlags.Instance | BindingFlags.NonPublic);
            var alignerAdsStateField = alignerType.GetField("_adsStateController", BindingFlags.Instance | BindingFlags.NonPublic);

            var needsRebind =
                !ReferenceEquals(alignerAdsPivotField?.GetValue(aligner) as Transform, adsPivot)
                || !ReferenceEquals(alignerCameraField?.GetValue(aligner) as Transform, cameraTransform)
                || !ReferenceEquals(alignerAttachmentManagerField?.GetValue(aligner) as Component, attachmentManager)
                || !ReferenceEquals(alignerAdsStateField?.GetValue(aligner) as Component, _adsStateRuntimeBridge);

            if (needsRebind)
            {
                var bindRuntimeReferences = alignerType.GetMethod("BindRuntimeReferences", BindingFlags.Instance | BindingFlags.Public);
                if (bindRuntimeReferences != null)
                {
                    bindRuntimeReferences.Invoke(aligner, new object[]
                    {
                        adsPivot,
                        cameraTransform,
                        attachmentManager,
                        _adsStateRuntimeBridge
                    });
                }
                else
                {
                    alignerAdsPivotField?.SetValue(aligner, adsPivot);
                    alignerCameraField?.SetValue(aligner, cameraTransform);
                    alignerAttachmentManagerField?.SetValue(aligner, attachmentManager);
                    alignerAdsStateField?.SetValue(aligner, _adsStateRuntimeBridge);
                }
            }

            _weaponAimAlignerRuntimeBridge = aligner;
            ApplyScopedAdsPresentationEyeReliefOffset();
        }

        private void EnsureRenderTextureScopeRuntimeBridge(Type adsType, Camera worldCamera)
        {
            if (adsType == null || _adsStateRuntimeBridge == null)
            {
                return;
            }

            var renderScopeType = ResolveTypeByName("Reloader.Game.Weapons.RenderTextureScopeController");
            if (renderScopeType == null)
            {
                return;
            }

            var renderScopeController = gameObject.GetComponent(renderScopeType) ?? gameObject.AddComponent(renderScopeType);
            if (renderScopeController == null)
            {
                return;
            }

            var scopeCamera = EnsureScopeCamera(worldCamera);
            var scopeCameraField = renderScopeType.GetField("_scopeCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            scopeCameraField?.SetValue(renderScopeController, scopeCamera);

            var renderScopeField = adsType.GetField("_renderTextureScopeController", BindingFlags.Instance | BindingFlags.NonPublic);
            renderScopeField?.SetValue(_adsStateRuntimeBridge, renderScopeController);
        }

        private void EnsurePeripheralScopeEffectsRuntimeBridge(Type adsType)
        {
            if (adsType == null || _adsStateRuntimeBridge == null)
            {
                return;
            }

            var peripheralEffectsType = ResolveTypeByName("Reloader.Game.Weapons.PeripheralScopeEffects");
            if (peripheralEffectsType == null)
            {
                return;
            }

            var peripheralEffects = gameObject.GetComponent(peripheralEffectsType) ?? gameObject.AddComponent(peripheralEffectsType);
            if (peripheralEffects == null)
            {
                return;
            }

            var peripheralEffectsField = adsType.GetField("_peripheralScopeEffects", BindingFlags.Instance | BindingFlags.NonPublic);
            peripheralEffectsField?.SetValue(_adsStateRuntimeBridge, peripheralEffects);
        }

        private void EnsureScopeAdjustmentTooltipRuntimeBridge(Type adsType)
        {
            if (adsType == null || _adsStateRuntimeBridge == null)
            {
                return;
            }

            var tooltipType = ResolveTypeByName("Reloader.Game.Weapons.ScopeAdjustmentTooltipOverlay");
            if (tooltipType == null)
            {
                return;
            }

            var tooltipOverlay = gameObject.GetComponent(tooltipType) ?? gameObject.AddComponent(tooltipType);
            if (tooltipOverlay == null)
            {
                return;
            }

            var tooltipField = adsType.GetField("_scopeAdjustmentTooltipOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
            tooltipField?.SetValue(_adsStateRuntimeBridge, tooltipOverlay);
        }

        private void TryAssignScopedAdsWeaponDefinition(Type adsType)
        {
            if (adsType == null || _adsStateRuntimeBridge == null)
            {
                return;
            }

            var definitionField = adsType.GetField("_weaponDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
            var setDefinitionMethod = adsType.GetMethod("SetWeaponDefinition", BindingFlags.Instance | BindingFlags.Public);
            var targetDefinitionType = definitionField?.FieldType
                ?? (setDefinitionMethod?.GetParameters().Length == 1 ? setDefinitionMethod.GetParameters()[0].ParameterType : null);
            if (targetDefinitionType == null)
            {
                return;
            }

            UObject resolvedDefinition = null;
            if (_equippedDefinition != null && targetDefinitionType.IsInstanceOfType(_equippedDefinition))
            {
                resolvedDefinition = _equippedDefinition;
            }
            else
            {
                resolvedDefinition = ResolveGameWeaponDefinition(targetDefinitionType, _equippedItemId);
            }

            if (resolvedDefinition == null)
            {
                return;
            }

            if (setDefinitionMethod != null)
            {
                setDefinitionMethod.Invoke(_adsStateRuntimeBridge, new object[] { resolvedDefinition });
                return;
            }

            definitionField?.SetValue(_adsStateRuntimeBridge, resolvedDefinition);
        }

        private static UObject ResolveGameWeaponDefinition(Type definitionType, string weaponId)
        {
            if (definitionType == null || string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            var idProperty = definitionType.GetProperty("WeaponId", BindingFlags.Instance | BindingFlags.Public)
                ?? definitionType.GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public);
            if (idProperty == null)
            {
                return null;
            }

            var definitions = Resources.FindObjectsOfTypeAll(definitionType);
            if (definitions == null || definitions.Length == 0)
            {
                return null;
            }

            Array.Sort(definitions, CompareObjectsDeterministically);
            for (var i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] is not UObject candidate)
                {
                    continue;
                }

                if (idProperty.GetValue(candidate) is string candidateId
                    && string.Equals(candidateId, weaponId, StringComparison.Ordinal))
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

            var child = worldCamera.transform.Find("ViewmodelCamera");
            return child != null ? child.GetComponent<Camera>() : null;
        }

        private static Camera EnsureScopeCamera(Camera worldCamera)
        {
            if (worldCamera == null)
            {
                return null;
            }

            var scopeTransform = worldCamera.transform.Find("ScopeCamera");
            Camera scopeCamera;
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
            }

            if (scopeCamera == null)
            {
                return null;
            }

            scopeTransform.localPosition = Vector3.zero;
            scopeTransform.localRotation = Quaternion.identity;
            scopeTransform.localScale = Vector3.one;

            scopeCamera.enabled = false;
            scopeCamera.clearFlags = worldCamera.clearFlags;
            scopeCamera.backgroundColor = worldCamera.backgroundColor;
            scopeCamera.cullingMask = ExcludeViewmodelLayer(worldCamera.cullingMask);
            scopeCamera.nearClipPlane = worldCamera.nearClipPlane;
            scopeCamera.farClipPlane = worldCamera.farClipPlane;
            scopeCamera.allowHDR = worldCamera.allowHDR;
            scopeCamera.allowMSAA = worldCamera.allowMSAA;
            scopeCamera.orthographic = worldCamera.orthographic;
            scopeCamera.depthTextureMode = worldCamera.depthTextureMode;
            scopeCamera.fieldOfView = worldCamera.fieldOfView;
            scopeCamera.targetTexture = null;
            return scopeCamera;
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

        private static UObject ResolveAttachmentDefinitionById(
            Type definitionType,
            string idPropertyName,
            string itemId,
            string requiredPrefabPropertyName = null)
        {
            if (definitionType == null || string.IsNullOrWhiteSpace(idPropertyName) || string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            var definitions = Resources.FindObjectsOfTypeAll(definitionType);
            if (definitions == null || definitions.Length == 0)
            {
                return null;
            }

            var idProperty = definitionType.GetProperty(idPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (idProperty == null)
            {
                return null;
            }

            PropertyInfo requiredPrefabProperty = null;
            if (!string.IsNullOrWhiteSpace(requiredPrefabPropertyName))
            {
                requiredPrefabProperty = definitionType.GetProperty(requiredPrefabPropertyName, BindingFlags.Instance | BindingFlags.Public);
            }

            Array.Sort(definitions, CompareObjectsDeterministically);
            for (var i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] is not UObject candidate)
                {
                    continue;
                }

                if (idProperty.GetValue(candidate) is string candidateId
                    && string.Equals(candidateId, itemId, StringComparison.Ordinal))
                {
                    if (requiredPrefabProperty != null)
                    {
                        if (requiredPrefabProperty.GetValue(candidate) is not GameObject requiredPrefab
                            || requiredPrefab == null)
                        {
                            continue;
                        }
                    }

                    return candidate;
                }
            }

#if UNITY_EDITOR
            var fromAssetDatabase = ResolveAttachmentDefinitionFromAssetDatabase(
                definitionType,
                idPropertyName,
                itemId,
                requiredPrefabPropertyName);
            if (fromAssetDatabase != null)
            {
                return fromAssetDatabase;
            }
#endif

            return null;
        }

#if UNITY_EDITOR
        private static UObject ResolveAttachmentDefinitionFromAssetDatabase(
            Type definitionType,
            string idPropertyName,
            string itemId,
            string requiredPrefabPropertyName)
        {
            if (definitionType == null || string.IsNullOrWhiteSpace(idPropertyName) || string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            var idProperty = definitionType.GetProperty(idPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (idProperty == null)
            {
                return null;
            }

            PropertyInfo requiredPrefabProperty = null;
            if (!string.IsNullOrWhiteSpace(requiredPrefabPropertyName))
            {
                requiredPrefabProperty = definitionType.GetProperty(requiredPrefabPropertyName, BindingFlags.Instance | BindingFlags.Public);
            }

            var searchFilter = $"t:{definitionType.Name}";
            var guids = AssetDatabase.FindAssets(searchFilter);
            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var candidate = AssetDatabase.LoadAssetAtPath(assetPath, definitionType);
                if (candidate == null)
                {
                    continue;
                }

                if (idProperty.GetValue(candidate) is not string candidateId
                    || !string.Equals(candidateId, itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (requiredPrefabProperty != null)
                {
                    if (requiredPrefabProperty.GetValue(candidate) is not GameObject prefab
                        || prefab == null)
                    {
                        continue;
                    }
                }

                return candidate as UObject;
            }

            return null;
        }
#endif

        private static bool IsDefinitionPrefabReferenceUsable(
            UObject definition,
            Type expectedDefinitionType,
            string idPropertyName)
        {
            if (definition == null || expectedDefinitionType == null)
            {
                return false;
            }

            var prefabPropertyName = GetPrefabPropertyNameForDefinition(expectedDefinitionType, idPropertyName);
            if (string.IsNullOrWhiteSpace(prefabPropertyName))
            {
                return true;
            }

            var prefabProperty = expectedDefinitionType.GetProperty(prefabPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prefabProperty == null)
            {
                return false;
            }

            if (prefabProperty.GetValue(definition) is not GameObject prefab)
            {
                return false;
            }

            return prefab != null;
        }

        private static string GetPrefabPropertyNameForDefinition(Type expectedDefinitionType, string idPropertyName)
        {
            if (expectedDefinitionType == null)
            {
                return null;
            }

            if (string.Equals(idPropertyName, "OpticId", StringComparison.Ordinal))
            {
                return "OpticPrefab";
            }

            if (string.Equals(idPropertyName, "AttachmentId", StringComparison.Ordinal))
            {
                return "MuzzlePrefab";
            }

            return null;
        }

        private void DestroyEquippedWeaponView()
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            Destroy(_equippedWeaponView);
            _equippedWeaponView = null;
            if (_adsStateRuntimeBridge != null)
            {
                Destroy(_adsStateRuntimeBridge);
            }

            _adsStateRuntimeBridge = null;
            _adsAttachmentManagerRuntimeBridge = null;
            _weaponAimAlignerRuntimeBridge = null;
            _scopedAdsPresentationEyeReliefOffset = 0f;
            _adsActiveOpticProperty = null;
            _adsBlendProperty = null;
            _adsCurrentSensitivityScaleProperty = null;
            _adsSetHeldMethod = null;
            _adsSetMagnificationMethod = null;
            _adsApplyScopeAdjustmentInputMethod = null;
            _adsCurrentMagnificationProperty = null;
            ResetScopedAdsLookSensitivityBridge();
        }

        private void ApplyScopedAdsPresentationEyeReliefOffset()
        {
            if (_weaponAimAlignerRuntimeBridge == null)
            {
                return;
            }

            var alignerType = _weaponAimAlignerRuntimeBridge.GetType();
            var setter = alignerType.GetMethod("SetRuntimeEyeReliefBackOffset", BindingFlags.Instance | BindingFlags.Public);
            setter?.Invoke(_weaponAimAlignerRuntimeBridge, new object[] { _scopedAdsPresentationEyeReliefOffset });
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

        private UObject ResolveOpticDefinition(string attachmentItemId)
        {
            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return null;
            }

            var metadataLookup = BuildAttachmentMetadataLookup();
            if (metadataLookup.TryGetValue(attachmentItemId, out var metadata)
                && metadata?.AttachmentDefinition != null)
            {
                var metadataType = ResolveTypeByName("Reloader.Game.Weapons.OpticDefinition");
                if (metadataType != null && metadataType.IsInstanceOfType(metadata.AttachmentDefinition))
                {
                    return metadata.AttachmentDefinition;
                }
            }

            return ResolveAttachmentDefinitionById(
                ResolveTypeByName("Reloader.Game.Weapons.OpticDefinition"),
                "OpticId",
                attachmentItemId,
                "OpticPrefab");
        }

        private UObject ResolveMuzzleAttachmentDefinition(string attachmentItemId)
        {
            if (string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return null;
            }

            var metadataLookup = BuildAttachmentMetadataLookup();
            if (metadataLookup.TryGetValue(attachmentItemId, out var metadata)
                && metadata?.AttachmentDefinition != null)
            {
                var metadataType = ResolveTypeByName("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
                if (metadataType != null && metadataType.IsInstanceOfType(metadata.AttachmentDefinition))
                {
                    return metadata.AttachmentDefinition;
                }
            }

            return ResolveAttachmentDefinitionById(
                ResolveTypeByName("Reloader.Game.Weapons.MuzzleAttachmentDefinition"),
                "AttachmentId",
                attachmentItemId,
                "MuzzlePrefab");
        }

        private bool TryResolveWeaponDefinition(string itemId, out WeaponDefinition definition)
        {
            itemId = NormalizeWeaponItemId(itemId);
            definition = ResolveWeaponDefinition(itemId);
            return definition != null;
        }

        private Transform ResolveDefaultWeaponViewParent()
        {
            if (_packAnimator == null)
            {
                _packAnimator = ResolvePackAnimator();
                if (_packAnimator == null)
                {
                    return null;
                }
            }

            var ikHandGun = FindDescendantByName(_packAnimator.transform, "ik_hand_gun");
            return ikHandGun != null ? ikHandGun : _packAnimator.transform;
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

                if (behaviour is WeaponViewAttachmentMounts)
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
