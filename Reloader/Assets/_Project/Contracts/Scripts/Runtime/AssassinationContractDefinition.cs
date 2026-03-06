using UnityEngine;

namespace Reloader.Contracts.Runtime
{
    [CreateAssetMenu(
        fileName = "AssassinationContractDefinition",
        menuName = "Reloader/Contracts/Assassination Contract")]
    public sealed class AssassinationContractDefinition : ScriptableObject
    {
        [SerializeField] private string _contractId = string.Empty;
        [SerializeField] private string _targetId = string.Empty;
        [SerializeField] private AssassinationContractArchetype _archetype = AssassinationContractArchetype.StreetRoutineTarget;
        [SerializeField] private float _distanceBand;
        [SerializeField] private int _payout;

        public string ContractId => _contractId;
        public string TargetId => _targetId;
        public AssassinationContractArchetype Archetype => _archetype;
        public float DistanceBand => _distanceBand;
        public int Payout => _payout;
    }
}
