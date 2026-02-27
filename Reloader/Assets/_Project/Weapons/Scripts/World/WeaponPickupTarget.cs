using System;
using Reloader.Inventory;
using Reloader.Core.Persistence;
using UnityEngine;

namespace Reloader.Weapons.World
{
    [RequireComponent(typeof(WorldObjectIdentity))]
    public sealed class WeaponPickupTarget : MonoBehaviour, IInventoryPickupTarget
    {
        [SerializeField] private string _itemId;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _disableGameObjectOnPickup = true;
        [SerializeField] private WorldObjectIdentity _identity;

        public string ItemId => _itemId;
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

        public void SetItemIdForTests(string itemId)
        {
            _itemId = itemId;
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
                throw new InvalidOperationException($"{nameof(WeaponPickupTarget)} requires {nameof(WorldObjectIdentity)} on '{name}'.");
            }

            return _identity;
        }
    }
}
