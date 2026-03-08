using System;
using UnityEngine;

namespace Reloader.NPCs.Generation
{
    [Serializable]
    public sealed class MainTownPopulationPoolDefinition
    {
        [SerializeField] private string _poolId = string.Empty;
        [SerializeField] private MainTownPopulationSlotDefinition[] _slots = Array.Empty<MainTownPopulationSlotDefinition>();

        public string PoolId
        {
            get => _poolId;
            set => _poolId = value ?? string.Empty;
        }

        public MainTownPopulationSlotDefinition[] Slots
        {
            get => _slots;
            set => _slots = value ?? Array.Empty<MainTownPopulationSlotDefinition>();
        }
    }
}
