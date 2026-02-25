using UnityEngine;

namespace Reloader.Inventory
{
    public sealed class DefinitionPickupTarget : MonoBehaviour, IInventoryDefinitionPickupTarget, IInventoryStackPickupTarget
    {
        [SerializeField] private ItemSpawnDefinition _spawnDefinition;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _disableGameObjectOnPickup = true;

        public ItemSpawnDefinition SpawnDefinition => _spawnDefinition;
        public string ItemId => _spawnDefinition != null ? _spawnDefinition.ItemId : null;
        public int Quantity => _spawnDefinition != null ? _spawnDefinition.Quantity : 1;

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

        public void SetSpawnDefinitionForTests(ItemSpawnDefinition spawnDefinition)
        {
            _spawnDefinition = spawnDefinition;
        }
    }
}
