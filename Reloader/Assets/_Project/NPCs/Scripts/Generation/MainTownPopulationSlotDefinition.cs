using System;
using UnityEngine;

namespace Reloader.NPCs.Generation
{
    public enum MainTownPopulationHabitat
    {
        Any = 0,
        Town = 1,
        Quarry = 2,
        Forest = 3
    }

    [Serializable]
    public sealed class MainTownPopulationSlotDefinition
    {
        [SerializeField] private string _populationSlotId = string.Empty;
        [SerializeField] private string _poolId = string.Empty;
        [SerializeField] private string _areaTag = string.Empty;
        [SerializeField] private string _spawnAnchorId = string.Empty;
        [SerializeField] private MainTownPopulationHabitat _habitat = MainTownPopulationHabitat.Town;
        [SerializeField] private bool _spawnOnSceneLoad = true;
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

        public MainTownPopulationHabitat Habitat
        {
            get => _habitat;
            set => _habitat = value;
        }

        public bool SpawnOnSceneLoad
        {
            get => _spawnOnSceneLoad;
            set => _spawnOnSceneLoad = value;
        }

        public bool IsProtectedFromContracts
        {
            get => _isProtectedFromContracts;
            set => _isProtectedFromContracts = value;
        }
    }
}
