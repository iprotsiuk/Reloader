using Reloader.Core.Persistence;
using UnityEngine;

namespace Reloader.Inventory
{
    [RequireComponent(typeof(WorldObjectIdentity))]
    public sealed class RuntimeStackPickupTarget : MonoBehaviour, IInventoryStackPickupTarget
    {
        [SerializeField] private string _itemId;
        [SerializeField] private int _quantity = 1;
        [SerializeField] private bool _disableGameObjectOnPickup = true;

        public string ItemId => _itemId;
        public int Quantity => Mathf.Max(1, _quantity);

        public void SetValuesForTests(string itemId, int quantity)
        {
            _itemId = itemId;
            _quantity = Mathf.Max(1, quantity);
        }

        public void OnPickedUp()
        {
            if (_disableGameObjectOnPickup)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
