using System.Collections.Generic;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Weapons.Controllers
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private WeaponRegistry _weaponRegistry;
        [SerializeField] private WeaponProjectile _projectilePrefab;
        [SerializeField] private Transform _muzzleTransform;

        private IPlayerInputSource _inputSource;
        private readonly Dictionary<string, WeaponRuntimeState> _statesByItemId = new Dictionary<string, WeaponRuntimeState>();
        private string _equippedItemId;
        private WeaponDefinition _equippedDefinition;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (_inputSource == null || _inventoryController == null || _weaponRegistry == null)
            {
                return;
            }

            UpdateEquipFromSelection();
            TickFire();
            TickReload();
        }

        public bool TryGetRuntimeState(string itemId, out WeaponRuntimeState state)
        {
            return _statesByItemId.TryGetValue(itemId ?? string.Empty, out state);
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            if (_inputSource == null)
            {
                _inputSource = GetInterfaceFromBehaviours<IPlayerInputSource>(GetComponents<MonoBehaviour>());
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

            _equippedItemId = itemId;
            _equippedDefinition = definition;
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                return;
            }

            var state = GetOrCreateState(_equippedItemId, _equippedDefinition);
            state.IsEquipped = true;
            GameEvents.RaiseWeaponEquipped(_equippedItemId);
        }

        private void TickFire()
        {
            if (!_inputSource.ConsumeFirePressed())
            {
                return;
            }

            if (!TryGetEquippedState(out var state))
            {
                return;
            }

            if (!state.TryFire(Time.time, out _))
            {
                return;
            }

            if (_projectilePrefab != null && _muzzleTransform != null)
            {
                var projectile = Instantiate(_projectilePrefab, _muzzleTransform.position, _muzzleTransform.rotation);
                projectile.Initialize(
                    _equippedItemId,
                    _muzzleTransform.forward,
                    _equippedDefinition.ProjectileSpeed,
                    _equippedDefinition.ProjectileGravityMultiplier,
                    _equippedDefinition.BaseDamage,
                    _equippedDefinition.MaxRangeMeters / Mathf.Max(_equippedDefinition.ProjectileSpeed, 0.01f));
            }

            GameEvents.RaiseWeaponFired(_equippedItemId, _muzzleTransform.position, _muzzleTransform.forward);
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

            if (!state.TryReload())
            {
                return;
            }

            GameEvents.RaiseWeaponReloaded(_equippedItemId, state.MagazineCount, state.ReserveCount);
        }

        private bool TryGetEquippedState(out WeaponRuntimeState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(_equippedItemId) || _equippedDefinition == null)
            {
                return false;
            }

            state = GetOrCreateState(_equippedItemId, _equippedDefinition);
            return state != null;
        }

        private WeaponRuntimeState GetOrCreateState(string itemId, WeaponDefinition definition)
        {
            if (_statesByItemId.TryGetValue(itemId, out var existing))
            {
                return existing;
            }

            var state = new WeaponRuntimeState(
                itemId,
                definition.MagazineCapacity,
                definition.FireIntervalSeconds,
                definition.StartingMagazineCount,
                definition.StartingReserveCount,
                definition.StartingChamberLoaded);
            _statesByItemId[itemId] = state;
            return state;
        }

        private static TInterface GetInterfaceFromBehaviours<TInterface>(MonoBehaviour[] behaviours) where TInterface : class
        {
            if (behaviours == null)
            {
                return null;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is TInterface typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}
