using System;
using System.Collections.Generic;

namespace Reloader.Inventory
{
    public sealed class StorageContainerRegistry
    {
        private readonly Dictionary<string, StorageContainerRuntime> _byId =
            new Dictionary<string, StorageContainerRuntime>(StringComparer.Ordinal);

        public void Upsert(StorageContainerRuntime container)
        {
            if (container == null || string.IsNullOrWhiteSpace(container.ContainerId))
            {
                return;
            }

            _byId[container.ContainerId] = container;
        }

        public bool TryGet(string containerId, out StorageContainerRuntime container)
        {
            container = null;
            if (string.IsNullOrWhiteSpace(containerId))
            {
                return false;
            }

            return _byId.TryGetValue(containerId, out container);
        }
    }
}
