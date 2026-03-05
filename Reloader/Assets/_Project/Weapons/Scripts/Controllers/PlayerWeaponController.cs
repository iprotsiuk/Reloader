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
            IReadOnlyList<AmmoBallisticSnapshot> magazineRounds)
        {
            ItemId = itemId;
            ChamberLoaded = chamberLoaded;
            MagCapacity = magCapacity;
            MagCount = magCount;
            ReserveCount = reserveCount;
            ChamberRound = chamberRound;
            MagazineRounds = magazineRounds;
        }

        public string ItemId { get; }
        public bool ChamberLoaded { get; }
        public int MagCapacity { get; }
        public int MagCount { get; }
        public int ReserveCount { get; }
        public AmmoBallisticSnapshot? ChamberRound { get; }
        public IReadOnlyList<AmmoBallisticSnapshot> MagazineRounds { get; }
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
        [SerializeField] private bool _useInventoryDefinitionViewFallback;

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
        private string _pendingEquipItemId;
        private WeaponDefinition _pendingEquipDefinition;
        private float _pendingEquipApplyTime;
        [SerializeField, Min(0f)] private float _holsterHideDelaySeconds = 0.2f;
        private float _scheduledArmsHideTime = -1f;
        private readonly List<Renderer> _packRenderers = new List<Renderer>();
        private static readonly Bounds ViewmodelSkinnedBounds = new Bounds(Vector3.zero, new Vector3(8f, 8f, 8f));
        public string EquippedItemId => _equippedItemId;

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
            return _statesByItemId.TryGetValue(itemId ?? string.Empty, out state);
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
                    state.GetMagazineRoundsSnapshot()));
            }

            return snapshots;
        }

        public bool ApplyRuntimeState(string itemId, int magazineCount, int reserveCount, bool chamberLoaded)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var state = TryGetRuntimeState(itemId, out var existing)
                ? existing
                : GetOrCreateState(itemId, ResolveWeaponDefinition(itemId), seedFromDefinition: false);
            if (state == null)
            {
                return false;
            }

            state.SetAmmoCounts(magazineCount, reserveCount, chamberLoaded);
            return true;
        }

        public bool ApplyRuntimeBallistics(string itemId, AmmoBallisticSnapshot? chamberRound, IReadOnlyList<AmmoBallisticSnapshot> magazineRounds)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var state = TryGetRuntimeState(itemId, out var existing)
                ? existing
                : GetOrCreateState(itemId, ResolveWeaponDefinition(itemId), seedFromDefinition: false);
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

            return WeaponAttachmentSwapService.TrySwap(
                _inventoryController.Runtime,
                _equippedDefinition,
                state,
                BuildAttachmentSlotLookup(),
                slotType,
                attachmentItemId);
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
            var selectedItemId = _inventoryController.Runtime != null ? _inventoryController.Runtime.SelectedBeltItemId : null;
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
            if (_equippedItemId == itemId)
            {
                if (!string.IsNullOrWhiteSpace(_equippedItemId)
                    && _equippedDefinition != null
                    && _equippedWeaponView == null)
                {
                    SpawnEquippedWeaponView(_equippedItemId);
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
            SpawnEquippedWeaponView(_equippedItemId);
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
            _pendingEquipItemId = itemId;
            _pendingEquipDefinition = definition;
            _pendingEquipApplyTime = Time.time + 0.08f;
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
            var firedDirection = ApplyDispersion(_muzzleTransform.forward, ballisticSpec.DispersionMoa, URandom.value, URandom.value);
            projectile?.Initialize(
                _equippedItemId,
                firedDirection,
                ballisticSpec.MuzzleVelocityFps * FeetToMeters,
                _equippedDefinition.ProjectileGravityMultiplier,
                _equippedDefinition.BaseDamage,
                _equippedDefinition.MaxRangeMeters / Mathf.Max(ballisticSpec.MuzzleVelocityFps * FeetToMeters, 0.01f),
                ballisticSpec.BallisticCoefficientG1,
                transform);

            NotifyViewWeaponFired(_equippedItemId);
            var muzzleAudioOverride = ResolveMuzzleAudioOverride();
            ResolveWeaponEvents()?.RaiseWeaponFired(_equippedItemId, _muzzleTransform.position, firedDirection);
            ResolveCombatAudioEmitter()?.EmitWeaponFire(_equippedItemId, _muzzleTransform.position, muzzleAudioOverride);
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
                return;
            }

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

        private void EnsureMuzzleRuntimeBridge(GameObject viewRoot, Transform viewMuzzle, UObject preferredAttachmentDefinition)
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
            var attachmentSlot = FindDescendantByName(viewRoot.transform, "MuzzleAttachmentSlot") ?? viewMuzzle;

            var muzzleSocketField = muzzleRuntimeType.GetField("_muzzleSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            muzzleSocketField?.SetValue(runtimeComponent, viewMuzzle);

            var attachmentSlotField = muzzleRuntimeType.GetField("_attachmentSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            attachmentSlotField?.SetValue(runtimeComponent, attachmentSlot);

            var attachmentDefinitionType = ResolveTypeByName("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            if (attachmentDefinitionType == null)
            {
                return;
            }

            var resolvedAttachment = preferredAttachmentDefinition;
            if (resolvedAttachment == null || !attachmentDefinitionType.IsInstanceOfType(resolvedAttachment))
            {
                resolvedAttachment = ResolveDeterministicMuzzleDefinitionFallback(attachmentDefinitionType);
            }

            if (resolvedAttachment == null)
            {
                resolvedAttachment = ResolveRuntimeDefaultAttachment(runtimeComponent, muzzleRuntimeType, attachmentDefinitionType);
            }

            if (resolvedAttachment == null)
            {
                return;
            }

            var defaultAttachmentField = muzzleRuntimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
            defaultAttachmentField?.SetValue(runtimeComponent, resolvedAttachment);

            var equipMethod = muzzleRuntimeType.GetMethod("Equip", BindingFlags.Instance | BindingFlags.Public);
            equipMethod?.Invoke(runtimeComponent, new object[] { resolvedAttachment });
        }

        private void EnsureDetachableMagazineRuntimeBridge(GameObject viewRoot, UObject preferredAttachmentDefinition)
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
            var magazineSocket = FindDescendantByName(viewRoot.transform, "MagazineSocket")
                ?? FindDescendantByName(viewRoot.transform, "SOCKET_Magazine")
                ?? FindDescendantByName(viewRoot.transform, "Muzzle");
            var dropSocket = FindDescendantByName(viewRoot.transform, "MagazineDropSocket") ?? magazineSocket;

            var magazineSocketField = runtimeType.GetField("_magazineSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            magazineSocketField?.SetValue(runtimeComponent, magazineSocket);

            var dropSocketField = runtimeType.GetField("_magazineDropSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            dropSocketField?.SetValue(runtimeComponent, dropSocket);

            var definitionType = ResolveTypeByName("Reloader.Game.Weapons.MagazineAttachmentDefinition");
            if (definitionType == null)
            {
                return;
            }

            var resolvedAttachment = preferredAttachmentDefinition;
            if (resolvedAttachment == null || !definitionType.IsInstanceOfType(resolvedAttachment))
            {
                resolvedAttachment = ResolveDeterministicMagazineDefinitionFallback(definitionType);
            }

            if (resolvedAttachment == null)
            {
                resolvedAttachment = ResolveRuntimeDefaultAttachment(runtimeComponent, runtimeType, definitionType);
            }

            if (resolvedAttachment == null)
            {
                return;
            }

            var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
            defaultAttachmentField?.SetValue(runtimeComponent, resolvedAttachment);

            var setAttachmentMethod = runtimeType.GetMethod("SetAttachment", BindingFlags.Instance | BindingFlags.Public);
            setAttachmentMethod?.Invoke(runtimeComponent, new object[] { resolvedAttachment });
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

        private static UObject ResolveRuntimeDefaultAttachment(Component runtimeComponent, Type runtimeType, Type expectedAttachmentType)
        {
            if (runtimeComponent == null || runtimeType == null || expectedAttachmentType == null)
            {
                return null;
            }

            var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
            if (defaultAttachmentField == null || !(defaultAttachmentField.GetValue(runtimeComponent) is UObject current))
            {
                return null;
            }

            return expectedAttachmentType.IsInstanceOfType(current) ? current : null;
        }

        private Dictionary<string, WeaponAttachmentSlotType> BuildAttachmentSlotLookup()
        {
            var lookup = new Dictionary<string, WeaponAttachmentSlotType>(StringComparer.Ordinal);
            if (_attachmentItemMetadata == null || _attachmentItemMetadata.Length == 0)
            {
                return lookup;
            }

            for (var i = 0; i < _attachmentItemMetadata.Length; i++)
            {
                var metadata = _attachmentItemMetadata[i];
                if (metadata == null)
                {
                    continue;
                }

                var attachmentItemId = metadata.AttachmentItemId;
                if (string.IsNullOrWhiteSpace(attachmentItemId))
                {
                    continue;
                }

                lookup[attachmentItemId] = metadata.SlotType;
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
            var preferredMuzzleAttachmentDefinition = ResolveViewMuzzleAttachmentDefinition(_equippedWeaponView);
            var preferredMagazineAttachmentDefinition = ResolveViewMagazineAttachmentDefinition(_equippedWeaponView);
            StripViewPhysicsComponents(_equippedWeaponView);
            StripViewRuntimeComponents(_equippedWeaponView);

            var viewMuzzle = FindDescendantByName(_equippedWeaponView.transform, "Muzzle")
                ?? FindDescendantByName(_equippedWeaponView.transform, "SOCKET_Muzzle");
            if (viewMuzzle != null)
            {
                _muzzleTransform = viewMuzzle;
            }

            EnsureMuzzleRuntimeBridge(_equippedWeaponView, viewMuzzle, preferredMuzzleAttachmentDefinition);
            EnsureDetachableMagazineRuntimeBridge(_equippedWeaponView, preferredMagazineAttachmentDefinition);
        }

        private void DestroyEquippedWeaponView()
        {
            if (_equippedWeaponView == null)
            {
                return;
            }

            Destroy(_equippedWeaponView);
            _equippedWeaponView = null;
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
            for (var i = 0; i < _weaponViewPrefabs.Length; i++)
            {
                var binding = _weaponViewPrefabs[i];
                if (!string.IsNullOrWhiteSpace(binding.ItemId) && binding.ItemId == itemId && binding.ViewPrefab != null)
                {
                    return binding.ViewPrefab;
                }
            }

            if (_equippedDefinition != null
                && _equippedDefinition.ItemId == itemId
                && _equippedDefinition.IconSourcePrefab != null)
            {
                return _equippedDefinition.IconSourcePrefab;
            }

            if (!_useInventoryDefinitionViewFallback)
            {
                return null;
            }

            var itemDefinitions = _inventoryController != null
                ? _inventoryController.GetItemDefinitionRegistrySnapshot()
                : null;
            if (itemDefinitions != null)
            {
                for (var i = 0; i < itemDefinitions.Count; i++)
                {
                    var definition = itemDefinitions[i];
                    if (definition == null
                        || string.IsNullOrWhiteSpace(definition.DefinitionId)
                        || definition.IconSourcePrefab == null)
                    {
                        continue;
                    }

                    if (definition.DefinitionId == itemId)
                    {
                        return definition.IconSourcePrefab;
                    }
                }
            }

            return null;
        }

        private bool TryResolveWeaponDefinition(string itemId, out WeaponDefinition definition)
        {
            definition = ResolveWeaponDefinition(itemId);
            if (definition != null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var registries = FindObjectsByType<WeaponRegistry>(FindObjectsSortMode.InstanceID);
            for (var i = 0; i < registries.Length; i++)
            {
                var candidate = registries[i];
                if (candidate == null || !candidate.TryGetWeaponDefinition(itemId, out definition))
                {
                    continue;
                }

                _weaponRegistry = candidate;
                return true;
            }

            definition = null;
            return false;
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

                // Weapon view instances should be pure visual meshes/sockets.
                Destroy(behaviour);
            }
        }

        private static UObject ResolveViewMuzzleAttachmentDefinition(GameObject viewRoot)
        {
            if (viewRoot == null)
            {
                return null;
            }

            var behaviours = viewRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "MuzzleAttachmentRuntime")
                {
                    continue;
                }

                var behaviourType = behaviour.GetType();
                var activeAttachmentProperty = behaviourType.GetProperty("ActiveAttachment", BindingFlags.Instance | BindingFlags.Public);
                if (activeAttachmentProperty != null && activeAttachmentProperty.GetValue(behaviour) is UObject activeAttachment)
                {
                    return activeAttachment;
                }

                var defaultAttachmentField = behaviourType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultAttachmentField != null && defaultAttachmentField.GetValue(behaviour) is UObject defaultAttachment)
                {
                    return defaultAttachment;
                }
            }

            return null;
        }

        private static UObject ResolveDeterministicMuzzleDefinitionFallback(Type attachmentDefinitionType)
        {
            if (attachmentDefinitionType == null)
            {
                return null;
            }

            var definitions = Resources.FindObjectsOfTypeAll(attachmentDefinitionType);
            if (definitions == null || definitions.Length == 0)
            {
                return null;
            }

            Array.Sort(definitions, CompareObjectsDeterministically);
            for (var i = 0; i < definitions.Length; i++)
            {
                var candidate = definitions[i];
                if (candidate == null || !IsSafeMuzzleDefinition(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
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

        private static bool IsSafeMuzzleDefinition(UObject definition)
        {
            if (definition == null)
            {
                return false;
            }

            var muzzlePrefabField = definition.GetType().GetField("_muzzlePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            if (muzzlePrefabField == null || !(muzzlePrefabField.GetValue(definition) is GameObject muzzlePrefab) || muzzlePrefab == null)
            {
                return false;
            }

            return !HasMissingScriptsInPrefab(muzzlePrefab);
        }

        private static UObject ResolveViewMagazineAttachmentDefinition(GameObject viewRoot)
        {
            if (viewRoot == null)
            {
                return null;
            }

            var behaviours = viewRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "DetachableMagazineRuntime")
                {
                    continue;
                }

                var defaultAttachmentField = behaviour.GetType().GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultAttachmentField != null && defaultAttachmentField.GetValue(behaviour) is UObject defaultAttachment)
                {
                    return defaultAttachment;
                }
            }

            return null;
        }

        private static UObject ResolveDeterministicMagazineDefinitionFallback(Type definitionType)
        {
            if (definitionType == null)
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
                var candidate = definitions[i];
                if (candidate == null || !IsSafeMagazineDefinition(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static bool IsSafeMagazineDefinition(UObject definition)
        {
            if (definition == null)
            {
                return false;
            }

            var visualPrefabField = definition.GetType().GetField("_magazineVisualPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            if (visualPrefabField == null || !(visualPrefabField.GetValue(definition) is GameObject visualPrefab) || visualPrefab == null)
            {
                return false;
            }

            return !HasMissingScriptsInPrefab(visualPrefab);
        }

        private static bool HasMissingScriptsInPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return true;
            }

            var components = prefab.GetComponentsInChildren<Component>(true);
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private PackWeaponPresentationConfig ResolvePackPresentationConfig(string itemId, WeaponDefinition definition = null)
        {
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
            if (string.IsNullOrWhiteSpace(itemId) || _weaponRegistry == null)
            {
                return null;
            }

            return _weaponRegistry.TryGetWeaponDefinition(itemId, out var definition) ? definition : null;
        }

    }
}
