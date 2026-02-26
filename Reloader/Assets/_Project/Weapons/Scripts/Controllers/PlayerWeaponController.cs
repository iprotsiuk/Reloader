using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
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
        private const string Ebr7cReticleId = "ebr-7c";
        private const float DefaultFov = 60f;
        private const float DefaultScopeZoomStep = 0.5f;

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private WeaponRegistry _weaponRegistry;
        [SerializeField] private WeaponProjectile _projectilePrefab;
        [SerializeField] private Transform _muzzleTransform;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private float _reloadDurationSeconds = 0.35f;
        [SerializeField] private Camera _adsCamera;
        [SerializeField] private float _scopeZoomSmoothTime = 0.08f;
        [SerializeField] private float _scopeZoomStep = DefaultScopeZoomStep;
        [SerializeField] private float _reticleScreenScale = 0.6f;

        private IPlayerInputSource _inputSource;
        private readonly Dictionary<string, WeaponRuntimeState> _statesByItemId = new Dictionary<string, WeaponRuntimeState>();
        private readonly Dictionary<string, WeaponScopeRuntimeState> _scopeStatesByItemId = new Dictionary<string, WeaponScopeRuntimeState>();
        private readonly Dictionary<string, float> _reloadCompleteTimeByItemId = new Dictionary<string, float>();
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
        private Texture2D _reticlePixelTexture;
        private float _cameraFovVelocity;
        private float _baseCameraFieldOfView = DefaultFov;
        private bool _baseCameraFieldOfViewCaptured;
        private Camera _cachedAdsCamera;
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
            TickScopeInput();
            TickScopeCameraZoom();
            TickReloadCancellation();
            TickReloadCompletion();
            TickFire();
            TickReload();
        }

        private void OnDestroy()
        {
            if (_reticlePixelTexture != null)
            {
                Destroy(_reticlePixelTexture);
                _reticlePixelTexture = null;
            }
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

        public void Configure(IWeaponEvents weaponEvents = null, IInventoryEvents inventoryEvents = null)
        {
            _useRuntimeKernelWeaponEvents = weaponEvents == null;
            _weaponEvents = weaponEvents;
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
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

            if (_cameraDefaults == null)
            {
                _cameraDefaults = GetComponent<PlayerCameraDefaults>();
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
                ResolveWeaponEvents()?.RaiseWeaponUnequipStarted(previousItemId);
                CancelReload(previousItemId, WeaponReloadCancelReason.Unequip);
                if (_isAiming)
                {
                    _isAiming = false;
                    ResolveWeaponEvents()?.RaiseWeaponAimChanged(previousItemId, false);
                }
            }

            _equippedItemId = itemId;
            _equippedDefinition = definition;
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                return;
            }

            ResolveWeaponEvents()?.RaiseWeaponEquipStarted(_equippedItemId);
            var state = GetOrCreateState(_equippedItemId, _equippedDefinition, seedFromDefinition: true);
            if (state == null)
            {
                return;
            }

            state.IsEquipped = true;
            ResolveWeaponEvents()?.RaiseWeaponEquipped(_equippedItemId);
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
            projectile?.Configure(_useRuntimeKernelWeaponEvents ? null : _weaponEvents);
            var firedDirection = ApplyDispersion(_muzzleTransform.forward, ballisticSpec.DispersionMoa, Random.value, Random.value);
            projectile?.Initialize(
                _equippedItemId,
                firedDirection,
                ballisticSpec.MuzzleVelocityFps * FeetToMeters,
                _equippedDefinition.ProjectileGravityMultiplier,
                _equippedDefinition.BaseDamage,
                _equippedDefinition.MaxRangeMeters / Mathf.Max(ballisticSpec.MuzzleVelocityFps * FeetToMeters, 0.01f),
                ballisticSpec.BallisticCoefficientG1,
                transform);

            ResolveWeaponEvents()?.RaiseWeaponFired(_equippedItemId, _muzzleTransform.position, firedDirection);
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
            ResolveWeaponEvents()?.RaiseWeaponReloadStarted(_equippedItemId);
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
                    ResolveInventoryEvents()?.RaiseInventoryChanged();
                }
            }

            state.SetReserveCount(_inventoryController.Runtime.GetItemQuantity(ammoItemId));
            ResolveWeaponEvents()?.RaiseWeaponReloaded(_equippedItemId, state.MagazineCount, state.ReserveCount);
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
            ResolveWeaponEvents()?.RaiseWeaponAimChanged(_equippedItemId, _isAiming);
        }

        private void TickScopeInput()
        {
            var scopeState = GetEquippedScopeState();
            if (!_isAiming || scopeState == null)
            {
                return;
            }

            var scrollDelta = NormalizeScrollDelta(_inputSource.ConsumeZoomInput());
            if (Mathf.Abs(scrollDelta) > 0.0001f)
            {
                scopeState.ApplyZoomDelta(scrollDelta * Mathf.Max(0.05f, _scopeZoomStep));
            }

            var zeroSteps = _inputSource.ConsumeZeroAdjustStep();
            if (zeroSteps != 0)
            {
                scopeState.ApplyZeroSteps(zeroSteps);
            }
        }

        private void TickScopeCameraZoom()
        {
            if (!TryGetCurrentFieldOfView(out var currentFieldOfView))
            {
                return;
            }

            var targetFieldOfView = _baseCameraFieldOfView;
            var scopeState = GetEquippedScopeState();
            if (_isAiming && scopeState != null)
            {
                targetFieldOfView = MagnificationToFieldOfView(_baseCameraFieldOfView, scopeState.CurrentZoom);
            }

            var nextFieldOfView = Mathf.SmoothDamp(
                currentFieldOfView,
                targetFieldOfView,
                ref _cameraFovVelocity,
                Mathf.Max(0.01f, _scopeZoomSmoothTime));

            TrySetCurrentFieldOfView(nextFieldOfView);
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
            ResolveWeaponEvents()?.RaiseWeaponReloadCancelled(itemId, reason);
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

        private WeaponScopeRuntimeState GetEquippedScopeState()
        {
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                return null;
            }

            if (_scopeStatesByItemId.TryGetValue(_equippedItemId, out var cachedState))
            {
                return cachedState;
            }

            var scopeConfiguration = _equippedDefinition.ScopeConfiguration;
            if (!scopeConfiguration.IsScopedWeapon)
            {
                return null;
            }

            var createdState = new WeaponScopeRuntimeState(scopeConfiguration);
            _scopeStatesByItemId[_equippedItemId] = createdState;
            return createdState;
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

        private static float MagnificationToFieldOfView(float referenceFieldOfView, float magnification)
        {
            var safeReferenceFov = Mathf.Clamp(referenceFieldOfView, 15f, 120f);
            var safeMagnification = Mathf.Max(1f, magnification);
            var referenceHalfAngle = Mathf.Deg2Rad * safeReferenceFov * 0.5f;
            var zoomedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) / safeMagnification);
            return Mathf.Clamp(zoomedHalfAngle * 2f * Mathf.Rad2Deg, 5f, safeReferenceFov);
        }

        private static float NormalizeScrollDelta(float scrollDelta)
        {
            var abs = Mathf.Abs(scrollDelta);
            if (abs <= 0.0001f)
            {
                return 0f;
            }

            if (abs > 10f)
            {
                return scrollDelta / 120f;
            }

            if (abs < 1f)
            {
                var scaled = 0.2f + (abs * 0.8f);
                return Mathf.Sign(scrollDelta) * Mathf.Clamp(scaled, 0.2f, 1f);
            }

            return scrollDelta;
        }

        private void OnGUI()
        {
            if (!_isAiming)
            {
                return;
            }

            var scopeState = GetEquippedScopeState();
            if (scopeState == null || !string.Equals(scopeState.Configuration.ReticleId, Ebr7cReticleId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_reticlePixelTexture == null)
            {
                _reticlePixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _reticlePixelTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.9f));
                _reticlePixelTexture.Apply();
            }

            DrawEbr7cReticle(_reticlePixelTexture, Mathf.Clamp(_reticleScreenScale, 0.2f, 1.5f));
        }

        private static void DrawEbr7cReticle(Texture2D pixel, float scale)
        {
            if (pixel == null)
            {
                return;
            }

            var color = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);

            var centerX = Screen.width * 0.5f;
            var centerY = Screen.height * 0.5f;
            var span = Mathf.Min(Screen.width, Screen.height) * 0.36f * scale;
            var thickness = Mathf.Max(1f, Mathf.Round(span * 0.008f));
            var gap = span * 0.05f;

            DrawLine(pixel, centerX - span, centerY, centerX - gap, centerY, thickness);
            DrawLine(pixel, centerX + gap, centerY, centerX + span, centerY, thickness);
            DrawLine(pixel, centerX, centerY - span, centerX, centerY - gap, thickness);
            DrawLine(pixel, centerX, centerY + gap, centerX, centerY + span, thickness);

            var hashStep = span / 10f;
            for (var i = 1; i <= 8; i++)
            {
                var length = i % 2 == 0 ? span * 0.04f : span * 0.025f;
                var x = centerX + (i * hashStep);
                var xMirror = centerX - (i * hashStep);
                DrawLine(pixel, x, centerY - length, x, centerY + length, thickness);
                DrawLine(pixel, xMirror, centerY - length, xMirror, centerY + length, thickness);
            }

            var dropLadderTop = centerY + (span * 0.12f);
            for (var i = 0; i < 6; i++)
            {
                var y = dropLadderTop + (i * hashStep * 0.7f);
                var width = span * (0.1f + (i * 0.03f));
                DrawLine(pixel, centerX - width, y, centerX + width, y, thickness);
            }

            GUI.color = color;
        }

        private static void DrawLine(Texture2D pixel, float x1, float y1, float x2, float y2, float width)
        {
            var delta = new Vector2(x2 - x1, y2 - y1);
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var length = delta.magnitude;
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
            GUI.DrawTexture(new Rect(x1, y1 - (width * 0.5f), length, width), pixel);
            GUI.matrix = previousMatrix;
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
