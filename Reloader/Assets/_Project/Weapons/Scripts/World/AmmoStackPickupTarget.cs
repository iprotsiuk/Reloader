using Reloader.Inventory;
using UnityEngine;

namespace Reloader.Weapons.World
{
    public sealed class AmmoStackPickupTarget : MonoBehaviour, IInventoryStackPickupTarget
    {
        [SerializeField] private string _itemId = "ammo-factory-308-147-fmj";
        [SerializeField] private int _quantity = 20;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _disableGameObjectOnPickup = true;

        public string ItemId => _itemId;
        public int Quantity => Mathf.Max(1, _quantity);

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
    }
}
