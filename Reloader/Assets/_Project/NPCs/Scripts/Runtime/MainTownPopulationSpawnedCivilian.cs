using Reloader.Core.Save.Modules;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class MainTownPopulationSpawnedCivilian : MonoBehaviour
    {
        [SerializeField] private string _civilianId = string.Empty;
        [SerializeField] private string _firstName = string.Empty;
        [SerializeField] private string _lastName = string.Empty;
        [SerializeField] private string _nickname = string.Empty;
        [SerializeField] private string _publicDisplayName = string.Empty;
        [SerializeField] private string _populationSlotId = string.Empty;
        [SerializeField] private string _poolId = string.Empty;
        [SerializeField] private string _spawnAnchorId = string.Empty;
        [SerializeField] private string _areaTag = string.Empty;

        public string CivilianId => _civilianId;
        public string FirstName => _firstName;
        public string LastName => _lastName;
        public string Nickname => _nickname;
        public string PublicDisplayName => _publicDisplayName;
        public string PopulationSlotId => _populationSlotId;
        public string PoolId => _poolId;
        public string SpawnAnchorId => _spawnAnchorId;
        public string AreaTag => _areaTag;

        public void Initialize(CivilianPopulationRecord record)
        {
            _civilianId = record?.CivilianId ?? string.Empty;
            _firstName = record?.FirstName ?? string.Empty;
            _lastName = record?.LastName ?? string.Empty;
            _nickname = record?.Nickname ?? string.Empty;
            _publicDisplayName = BuildPublicDisplayName(record);
            _populationSlotId = record?.PopulationSlotId ?? string.Empty;
            _poolId = record?.PoolId ?? string.Empty;
            _spawnAnchorId = record?.SpawnAnchorId ?? string.Empty;
            _areaTag = record?.AreaTag ?? string.Empty;

            var appearanceApplicator = GetComponent<MainTownNpcAppearanceApplicator>();
            appearanceApplicator?.Apply(record);
        }

        private static string BuildPublicDisplayName(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var firstName = record.FirstName?.Trim() ?? string.Empty;
            var lastName = record.LastName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return record.CivilianId ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                return lastName;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return firstName;
            }

            return string.Concat(firstName, " ", lastName);
        }
    }
}
