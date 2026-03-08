using System;
using UnityEngine;

namespace Reloader.NPCs.Generation
{
    [Serializable]
    public sealed class MainTownPopulationSlotDefinition
    {
        [SerializeField] private string _populationSlotId = string.Empty;
        [SerializeField] private string _poolId = string.Empty;
        [SerializeField] private string _areaTag = string.Empty;
        [SerializeField] private string _spawnAnchorId = string.Empty;
        [SerializeField] private bool _isProtectedFromContracts;

        public string PopulationSlotId
        {
            get => _populationSlotId;
            set => _populationSlotId = value ?? string.Empty;
        }

        public string PoolId
        {
            get => _poolId;
            set => _poolId = value ?? string.Empty;
        }

        public string AreaTag
        {
            get => _areaTag;
            set => _areaTag = value ?? string.Empty;
        }

        public string SpawnAnchorId
        {
            get => _spawnAnchorId;
            set => _spawnAnchorId = value ?? string.Empty;
        }

        public bool IsProtectedFromContracts
        {
            get => _isProtectedFromContracts;
            set => _isProtectedFromContracts = value;
        }
    }
}
