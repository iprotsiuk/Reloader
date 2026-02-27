using Reloader.Inventory;
using UnityEngine;

namespace Reloader.UI
{
    public sealed class BeltHudBootstrap : MonoBehaviour
    {
        [SerializeField] private BeltHudPresenter _beltHudPresenter;

        private bool _warningLogged;

        private void Start()
        {
            ResolveInventoryController();
        }

        private void Update()
        {
            ResolveInventoryController();
        }

        private void ResolveInventoryController()
        {
            if (_beltHudPresenter == null)
            {
                _beltHudPresenter = GetComponentInChildren<BeltHudPresenter>(true);
                if (_beltHudPresenter == null && !_warningLogged)
                {
                    Debug.LogWarning("BeltHudBootstrap expects a scene-authored BeltHudPresenter reference.", this);
                    _warningLogged = true;
                }
            }

            if (_beltHudPresenter == null || _beltHudPresenter.InventoryController != null)
            {
                return;
            }

            var inventoryController = FindAnyObjectByType<PlayerInventoryController>();
            if (inventoryController == null)
            {
                if (!_warningLogged)
                {
                    Debug.LogWarning("BeltHudBootstrap could not find PlayerInventoryController in the scene.", this);
                    _warningLogged = true;
                }

                return;
            }

            _beltHudPresenter.SetInventoryController(inventoryController);
        }
    }
}
