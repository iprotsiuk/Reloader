using Reloader.Core.Events;
using Reloader.UI.Toolkit.Contracts;
using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.UI.Toolkit.AmmoHud
{
    public sealed class AmmoHudController : MonoBehaviour, IUiController
    {
        [SerializeField] private PlayerWeaponController _playerWeaponController;

        private AmmoHudViewBinder _viewBinder;
        private string _currentItemId;

        private void OnEnable()
        {
            GameEvents.OnWeaponEquipped += HandleWeaponEquipped;
            GameEvents.OnWeaponFired += HandleWeaponFired;
            GameEvents.OnWeaponReloaded += HandleWeaponReloaded;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnWeaponEquipped -= HandleWeaponEquipped;
            GameEvents.OnWeaponFired -= HandleWeaponFired;
            GameEvents.OnWeaponReloaded -= HandleWeaponReloaded;
        }

        public void SetViewBinder(AmmoHudViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void SetWeaponController(PlayerWeaponController weaponController)
        {
            _playerWeaponController = weaponController;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        public void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            ResolveReferences();
            if (!TryBuildText(out var labelText))
            {
                _viewBinder.Render(new AmmoHudUiState("-- 0/0", false));
                return;
            }

            _viewBinder.Render(new AmmoHudUiState(labelText, true));
        }

        private void ResolveReferences()
        {
            if (_playerWeaponController == null)
            {
                _playerWeaponController = FindAnyObjectByType<PlayerWeaponController>();
            }
        }

        private bool TryBuildText(out string labelText)
        {
            labelText = "-- 0/0";
            if (_playerWeaponController == null || string.IsNullOrWhiteSpace(_currentItemId))
            {
                return false;
            }

            if (!_playerWeaponController.TryGetRuntimeState(_currentItemId, out var state) || state == null)
            {
                return false;
            }

            var ammoName = state.ChamberRound.HasValue && !string.IsNullOrWhiteSpace(state.ChamberRound.Value.DisplayName)
                ? state.ChamberRound.Value.DisplayName
                : "--";

            labelText = $"{ammoName} {Mathf.Max(0, state.MagazineCount)}/{Mathf.Max(0, state.ReserveCount)}";
            return true;
        }

        private void HandleWeaponEquipped(string itemId)
        {
            _currentItemId = itemId;
            Refresh();
        }

        private void HandleWeaponFired(string itemId, Vector3 _, Vector3 __)
        {
            _currentItemId = itemId;
            Refresh();
        }

        private void HandleWeaponReloaded(string itemId, int _, int __)
        {
            _currentItemId = itemId;
            Refresh();
        }
    }
}
