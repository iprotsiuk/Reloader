using UnityEngine;

namespace Reloader.Inventory
{
    public sealed class WorldStorageContainer : MonoBehaviour
    {
        [SerializeField] private string _containerId = "chest.mainTown.workbench.001";
        [SerializeField] private string _displayName = "Storage Chest";
        [SerializeField] private int _slotCapacity = 20;
        [SerializeField] private StorageContainerPolicy _policy = StorageContainerPolicy.Persistent;

        public string ContainerId => string.IsNullOrWhiteSpace(_containerId) ? string.Empty : _containerId.Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName.Trim();
        public int SlotCapacity => _slotCapacity;
        public StorageContainerPolicy Policy => _policy;

        private void Start()
        {
            EnsureRegistered();
        }

        public void ConfigureRuntimeIdentity(
            string containerId,
            string displayName,
            int slotCapacity,
            StorageContainerPolicy policy)
        {
            _containerId = containerId ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _slotCapacity = Mathf.Max(1, slotCapacity);
            _policy = policy;
        }

        public StorageContainerRuntime EnsureRegistered()
        {
            var id = ContainerId;
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (StorageRuntimeBridge.Registry.TryGet(id, out var existing) && existing != null)
            {
                return existing;
            }

            var runtime = new StorageContainerRuntime(id, Mathf.Max(1, _slotCapacity), _policy);
            StorageRuntimeBridge.Registry.Upsert(runtime);
            return runtime;
        }
    }
}
