using Reloader.Core.Save.Modules;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class MainTownPopulationSpawnedCivilian : MonoBehaviour
    {
        [SerializeField] private string _civilianId = string.Empty;
        [SerializeField] private string _populationSlotId = string.Empty;
        [SerializeField] private string _poolId = string.Empty;
        [SerializeField] private string _spawnAnchorId = string.Empty;
        [SerializeField] private string _areaTag = string.Empty;

        public string CivilianId => _civilianId;
        public string PopulationSlotId => _populationSlotId;
        public string PoolId => _poolId;
        public string SpawnAnchorId => _spawnAnchorId;
        public string AreaTag => _areaTag;

        public void Initialize(CivilianPopulationRecord record)
        {
            _civilianId = record?.CivilianId ?? string.Empty;
            _populationSlotId = record?.PopulationSlotId ?? string.Empty;
            _poolId = record?.PoolId ?? string.Empty;
            _spawnAnchorId = record?.SpawnAnchorId ?? string.Empty;
            _areaTag = record?.AreaTag ?? string.Empty;
        }
    }
}
