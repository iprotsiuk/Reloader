using System;
using Reloader.Core.Items;
using UnityEngine;

namespace Reloader.Inventory
{
    public sealed class WorldStorageContainerSeedLoadout : MonoBehaviour
    {
        [Serializable]
        private struct SeedEntry
        {
            [SerializeField] private ItemDefinition _itemDefinition;
            [SerializeField] private int _quantity;

            public ItemDefinition ItemDefinition => _itemDefinition;
            public int Quantity => Mathf.Max(1, _quantity);
        }

        [SerializeField] private WorldStorageContainer _container;
        [SerializeField] private bool _seedOnlyWhenEmpty = true;
        [SerializeField] private SeedEntry[] _entries = Array.Empty<SeedEntry>();

        private bool _seedAttempted;

        private void Awake()
        {
            EnsureSeeded();
        }

        public void EnsureSeeded()
        {
            if (_seedAttempted)
            {
                return;
            }

            _seedAttempted = true;
            var container = _container != null ? _container : GetComponent<WorldStorageContainer>();
            var runtime = container?.EnsureRegistered();
            if (runtime == null)
            {
                return;
            }

            if (_seedOnlyWhenEmpty && !IsContainerEmpty(runtime))
            {
                return;
            }

            SeedRuntime(runtime);
        }

        private static bool IsContainerEmpty(StorageContainerRuntime runtime)
        {
            for (var i = 0; i < runtime.SlotCount; i++)
            {
                if (!string.IsNullOrWhiteSpace(runtime.GetSlotItemId(i)))
                {
                    return false;
                }
            }

            return true;
        }

        private void SeedRuntime(StorageContainerRuntime runtime)
        {
            var slotIndex = 0;
            for (var i = 0; i < _entries.Length && slotIndex < runtime.SlotCount; i++)
            {
                var definition = _entries[i].ItemDefinition;
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                var remaining = _entries[i].Quantity;
                var maxStack = definition.StackPolicy == ItemStackPolicy.NonStackable ? 1 : definition.MaxStack;
                while (remaining > 0 && slotIndex < runtime.SlotCount)
                {
                    var stackQuantity = Mathf.Min(remaining, maxStack);
                    runtime.TrySetSlotStack(slotIndex, new ItemStackState(definition.DefinitionId, stackQuantity, maxStack));
                    remaining -= stackQuantity;
                    slotIndex++;
                }
            }
        }
    }
}
