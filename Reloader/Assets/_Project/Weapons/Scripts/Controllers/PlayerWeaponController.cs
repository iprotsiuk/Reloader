using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Weapons.Controllers
{
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
        private const string DefaultAmmoDisplayName = "Factory .308 147gr FMJ";
        private const string DefaultAmmoItemId = "ammo-factory-308-147-fmj";

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private WeaponRegistry _weaponRegistry;
        [SerializeField] private WeaponProjectile _projectilePrefab;
        [SerializeField] private Transform _muzzleTransform;
        [SerializeField] private float _reloadDurationSeconds = 0.35f;

        private IPlayerInputSource _inputSource;
        private readonly Dictionary<string, WeaponRuntimeState> _statesByItemId = new Dictionary<string, WeaponRuntimeState>();
        private readonly Dictionary<string, float> _reloadCompleteTimeByItemId = new Dictionary<string, float>();
        private string _equippedItemId;
        private WeaponDefinition _equippedDefinition;
        private bool _isAiming;
        private bool _loggedMissingProjectilePrefab;
        private bool _loggedMissingCoreDependencies;
        private bool _attemptedSceneInputResolution;
        public string EquippedItemId => _equippedItemId;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!DependencyResolutionGuard.HasRequiredReferences(
                    ref _loggedMissingCoreDependencies,
                    this,
                    "PlayerWeaponController requires PlayerInventoryController and WeaponRegistry references.",
                    _inventoryController,
                    _weaponRegistry))
            {
                return;
            }

            UpdateEquipFromSelection();
            SyncEquippedReserveFromInventory();
            if (_inputSource == null)
            {
                DependencyResolutionGuard.ResolveOnce(
                    ref _inputSource,
                    ref _attemptedSceneInputResolution,
                    () => DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)));

                return;
            }

            TickAimState();
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

            state.SetAmmoLoadoutForTests(chamberRound, magazineRounds);
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
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
                _attemptedSceneInputResolution = true;
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
        }

        private void UpdateEquipFromSelection()
        {
            var selectedItemId = _inventoryController.Runtime != null ? _inventoryController.Runtime.SelectedBeltItemId : null;
            if (string.IsNullOrWhiteSpace(selectedItemId))
            {
                SetEquippedWeapon(null, null);
                return;
            }

            if (_weaponRegistry.TryGetWeaponDefinition(selectedItemId, out var definition))
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
                return;
            }

            var previousItemId = _equippedItemId;
            if (!string.IsNullOrWhiteSpace(previousItemId))
            {
                GameEvents.RaiseWeaponUnequipStarted(previousItemId);
                CancelReload(previousItemId, WeaponReloadCancelReason.Unequip);
                if (_isAiming)
                {
                    _isAiming = false;
                    GameEvents.RaiseWeaponAimChanged(previousItemId, false);
                }
            }

            _equippedItemId = itemId;
            _equippedDefinition = definition;
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                return;
            }

            GameEvents.RaiseWeaponEquipStarted(_equippedItemId);
            var state = GetOrCreateState(_equippedItemId, _equippedDefinition, seedFromDefinition: true);
            if (state == null)
            {
                return;
            }

            state.IsEquipped = true;
            GameEvents.RaiseWeaponEquipped(_equippedItemId);
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

            if (!state.TryFire(Time.time, out var fireData))
            {
                return;
            }

            var ballisticSpec = ResolveBallisticSpec(fireData);
            var projectile = SpawnProjectile();
            projectile?.Initialize(
                _equippedItemId,
                ApplyDispersion(_muzzleTransform.forward, ballisticSpec.DispersionMoa),
                ballisticSpec.MuzzleVelocityFps * FeetToMeters,
                _equippedDefinition.ProjectileGravityMultiplier,
                _equippedDefinition.BaseDamage,
                _equippedDefinition.MaxRangeMeters / Mathf.Max(ballisticSpec.MuzzleVelocityFps * FeetToMeters, 0.01f),
                ballisticSpec.BallisticCoefficientG1,
                transform);

            GameEvents.RaiseWeaponFired(_equippedItemId, _muzzleTransform.position, _muzzleTransform.forward);
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

            if (state.IsReloading)
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

            state.IsReloading = true;
            _reloadCompleteTimeByItemId[_equippedItemId] = Time.time + Mathf.Max(0.01f, _reloadDurationSeconds);
            GameEvents.RaiseWeaponReloadStarted(_equippedItemId);
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

            if (!TryGetEquippedState(out var state) || state == null || !state.IsReloading)
            {
                return;
            }

            if (_reloadCompleteTimeByItemId.TryGetValue(_equippedItemId, out var completeAt) && Time.time < completeAt)
            {
                return;
            }

            state.IsReloading = false;
            _reloadCompleteTimeByItemId.Remove(_equippedItemId);

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
                    GameEvents.RaiseInventoryChanged();
                }
            }

            state.SetReserveCount(_inventoryController.Runtime.GetItemQuantity(ammoItemId));
            GameEvents.RaiseWeaponReloaded(_equippedItemId, state.MagazineCount, state.ReserveCount);
        }

        private void TickAimState()
        {
            if (string.IsNullOrWhiteSpace(_equippedItemId))
            {
                return;
            }

            var isAimingNow = _inputSource.AimHeld;
            if (_isAiming == isAimingNow)
            {
                return;
            }

            _isAiming = isAimingNow;
            GameEvents.RaiseWeaponAimChanged(_equippedItemId, _isAiming);
        }

        private void CancelReload(string itemId, WeaponReloadCancelReason reason)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            if (!_statesByItemId.TryGetValue(itemId, out var state) || state == null || !state.IsReloading)
            {
                return;
            }

            state.IsReloading = false;
            _reloadCompleteTimeByItemId.Remove(itemId);
            GameEvents.RaiseWeaponReloadCancelled(itemId, reason);
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
                return CartridgeBallisticSpecBuilder.Build(fireData.FiredRound.Value, Random.value);
            }

            var fallbackRound = BuildDefinitionRound(_equippedDefinition);
            return CartridgeBallisticSpecBuilder.Build(fallbackRound, Random.value);
        }

        private static AmmoBallisticSnapshot BuildDefinitionRound(WeaponDefinition definition)
        {
            return new AmmoBallisticSnapshot(
                AmmoSourceType.Factory,
                2780f,
                55f,
                147f,
                0.398f,
                4.5f,
                DefaultAmmoDisplayName,
                System.Guid.NewGuid().ToString("N"),
                DefaultAmmoItemId);
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

            return DefaultAmmoItemId;
        }

        private static Vector3 ApplyDispersion(Vector3 direction, float dispersionMoa)
        {
            var safeDirection = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
            if (dispersionMoa <= 0f)
            {
                return safeDirection;
            }

            var maxAngle = dispersionMoa / 60f;
            var yaw = Random.Range(-maxAngle, maxAngle);
            var pitch = Random.Range(-maxAngle, maxAngle);
            return Quaternion.Euler(pitch, yaw, 0f) * safeDirection;
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
