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
        [SerializeField] private string _title = string.Empty;
        [SerializeField] private string _targetDisplayName = string.Empty;
        [SerializeField] private string _targetDescription = string.Empty;
        [SerializeField] private string _briefingText = string.Empty;
        [SerializeField] private float _distanceBand;
        [SerializeField] private int _payout;

        public string ContractId => _contractId;
        public string TargetId => _targetId;
        public AssassinationContractArchetype Archetype => _archetype;
        public string Title => _title;
        public string TargetDisplayName => _targetDisplayName;
        public string TargetDescription => _targetDescription;
        public string BriefingText => _briefingText;
        public float DistanceBand => _distanceBand;
        public int Payout => _payout;

        public void ConfigureRuntimeOffer(
            string contractId,
            string targetId,
            string title,
            string targetDisplayName,
            string targetDescription,
            string briefingText,
            float distanceBand,
            int payout,
            AssassinationContractArchetype archetype = AssassinationContractArchetype.StreetRoutineTarget)
        {
            _contractId = contractId ?? string.Empty;
            _targetId = targetId ?? string.Empty;
            _archetype = archetype;
            _title = title ?? string.Empty;
            _targetDisplayName = targetDisplayName ?? string.Empty;
            _targetDescription = targetDescription ?? string.Empty;
            _briefingText = briefingText ?? string.Empty;
            _distanceBand = distanceBand;
            _payout = payout;
        }
    }
}
