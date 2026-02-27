using System;
using Reloader.Inventory;
using Reloader.Core.Persistence;
using UnityEngine;

namespace Reloader.Weapons.World
{
    [RequireComponent(typeof(WorldObjectIdentity))]
    public sealed class AmmoStackPickupTarget : MonoBehaviour, IInventoryStackPickupTarget
    {
        [SerializeField] private string _itemId = "ammo-factory-308-147-fmj";
        [SerializeField] private int _quantity = 20;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _disableGameObjectOnPickup = true;
        [SerializeField] private WorldObjectIdentity _identity;

        public string ItemId => _itemId;
        public int Quantity => Mathf.Max(1, _quantity);
        public string ObjectId
        {
            get
            {
                return ResolveIdentity().ObjectId;
            }
        }

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

        private void Awake()
        {
            ResolveIdentity();
        }

        private void OnValidate()
        {
            ResolveIdentity();
        }

        private WorldObjectIdentity ResolveIdentity()
        {
            if (_identity == null)
            {
                _identity = GetComponent<WorldObjectIdentity>();
            }

            if (_identity == null)
            {
                throw new InvalidOperationException($"{nameof(AmmoStackPickupTarget)} requires {nameof(WorldObjectIdentity)} on '{name}'.");
            }

            return _identity;
        }
    }
}
