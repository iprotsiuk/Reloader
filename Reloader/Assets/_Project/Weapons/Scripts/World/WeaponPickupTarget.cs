using Reloader.Inventory;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class WeaponPickupTarget : MonoBehaviour, IInventoryPickupTarget
    {
        [SerializeField] private string _itemId;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _disableGameObjectOnPickup = true;

        public string ItemId => _itemId;

        public void OnPickedUp()
        {
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }

            if (_disableGameObjectOnPickup)
            {
                gameObject.SetActive(false);
            }
        }

        public void SetItemIdForTests(string itemId)
        {
            _itemId = itemId;
        }
    }
}
