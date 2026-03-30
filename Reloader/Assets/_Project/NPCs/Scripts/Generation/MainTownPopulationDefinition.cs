using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.NPCs.Generation
{
    [CreateAssetMenu(
        fileName = "MainTownPopulationDefinition",
        menuName = "Reloader/NPCs/MainTown Population Definition")]
    public sealed class MainTownPopulationDefinition : ScriptableObject
    {
        private const string PlaygroundAreaTagPrefix = "maintown.playground.";

        [SerializeField] private MainTownPopulationPoolDefinition[] _pools = Array.Empty<MainTownPopulationPoolDefinition>();

        public MainTownPopulationPoolDefinition[] Pools
        {
            get => _pools;
            set => _pools = value ?? Array.Empty<MainTownPopulationPoolDefinition>();
        }

        public void Validate()
        {
            if (Pools == null || Pools.Length == 0)
            {
                throw new ArgumentException("MainTownPopulationDefinition must define at least one pool.");
            }

            var seenSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var seenPlaygroundAreaTags = new HashSet<string>(StringComparer.Ordinal);

            for (var poolIndex = 0; poolIndex < Pools.Length; poolIndex++)
            {
                var pool = Pools[poolIndex];
                if (pool == null)
                {
                    throw new ArgumentException($"Pools[{poolIndex}] cannot be null.");
                }

                if (string.IsNullOrWhiteSpace(pool.PoolId))
                {
                    throw new ArgumentException($"Pools[{poolIndex}].poolId is required.");
                }

                if (pool.Slots == null || pool.Slots.Length == 0)
                {
                    throw new ArgumentException($"Pools[{poolIndex}] must define at least one slot.");
                }

                for (var slotIndex = 0; slotIndex < pool.Slots.Length; slotIndex++)
                {
                    var slot = pool.Slots[slotIndex];
                    if (slot == null)
                    {
                        throw new ArgumentException($"Pools[{poolIndex}].slots[{slotIndex}] cannot be null.");
                    }

                    if (string.IsNullOrWhiteSpace(slot.PopulationSlotId))
                    {
                        throw new ArgumentException($"Pools[{poolIndex}].slots[{slotIndex}].populationSlotId is required.");
                    }

                    if (!seenSlotIds.Add(slot.PopulationSlotId.Trim()))
                    {
                        throw new ArgumentException($"Duplicate populationSlotId '{slot.PopulationSlotId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(slot.PoolId))
                    {
                        throw new ArgumentException(
                            $"Pools[{poolIndex}].slots[{slotIndex}].poolId is required.");
                    }

                    if (!string.Equals(slot.PoolId.Trim(), pool.PoolId.Trim(), StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            $"Pools[{poolIndex}].slots[{slotIndex}] must use poolId '{pool.PoolId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(slot.AreaTag))
                    {
                        throw new ArgumentException(
                            $"Pools[{poolIndex}].slots[{slotIndex}].areaTag is required.");
                    }

                    var trimmedAreaTag = slot.AreaTag.Trim();
                    if (trimmedAreaTag.StartsWith(PlaygroundAreaTagPrefix, StringComparison.Ordinal) &&
                        !seenPlaygroundAreaTags.Add(trimmedAreaTag))
                    {
                        throw new ArgumentException(
                            $"Duplicate playground areaTag '{trimmedAreaTag}'. Playground markers require exactly one authored slot per playground areaTag.");
                    }

                    if (string.IsNullOrWhiteSpace(slot.SpawnAnchorId))
                    {
                        throw new ArgumentException(
                            $"Pools[{poolIndex}].slots[{slotIndex}].spawnAnchorId is required.");
                    }

                    if (!Enum.IsDefined(typeof(MainTownPopulationHabitat), slot.Habitat) ||
                        slot.Habitat == MainTownPopulationHabitat.Any)
                    {
                        throw new ArgumentException(
                            $"Pools[{poolIndex}].slots[{slotIndex}].habitat must use a concrete authored habitat.");
                    }
                }
            }
        }

        public IEnumerable<MainTownPopulationSlotDefinition> GetSlotsForHabitat(MainTownPopulationHabitat habitat)
        {
            var filterByHabitat = habitat != MainTownPopulationHabitat.Any;
            var pools = Pools ?? Array.Empty<MainTownPopulationPoolDefinition>();
            for (var poolIndex = 0; poolIndex < pools.Length; poolIndex++)
            {
                var pool = pools[poolIndex];
                if (pool?.Slots == null)
                {
                    continue;
                }

                for (var slotIndex = 0; slotIndex < pool.Slots.Length; slotIndex++)
                {
                    var slot = pool.Slots[slotIndex];
                    if (slot == null)
                    {
                        continue;
                    }

                    if (filterByHabitat && slot.Habitat != habitat)
                    {
                        continue;
                    }

                    yield return slot;
                }
            }
        }
    }
}
