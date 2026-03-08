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
        [SerializeField] private ContractFailurePolicy _failurePolicy = new();
        [SerializeField] private ContractObjectivePolicy _objectivePolicy = new();

        public string ContractId => _contractId;
        public string TargetId => _targetId;
        public AssassinationContractArchetype Archetype => _archetype;
        public string Title => _title;
        public string TargetDisplayName => _targetDisplayName;
        public string TargetDescription => _targetDescription;
        public string BriefingText => _briefingText;
        public float DistanceBand => _distanceBand;
        public int Payout => _payout;
        public ContractFailurePolicy FailurePolicy => _failurePolicy ??= new ContractFailurePolicy();
        public ContractObjectivePolicy ObjectivePolicy => _objectivePolicy ??= new ContractObjectivePolicy();
        public bool FailsOnWrongTargetKill => FailurePolicy.ContainsRule(AssassinationContractFailureRuleType.WrongTargetKill);

        public void ConfigureRuntimeOffer(
            string contractId,
            string targetId,
            string title,
            string targetDisplayName,
            string targetDescription,
            string briefingText,
            float distanceBand,
            int payout,
            AssassinationContractArchetype archetype = AssassinationContractArchetype.StreetRoutineTarget,
            ContractFailurePolicy failurePolicy = null,
            ContractObjectivePolicy objectivePolicy = null)
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
            _failurePolicy = failurePolicy ?? new ContractFailurePolicy();
            _objectivePolicy = objectivePolicy ?? new ContractObjectivePolicy();
        }

        public string BuildRestrictionsText()
        {
            var objectiveText = ObjectivePolicy.BuildRestrictionsText();
            var failureText = FailurePolicy.BuildRestrictionsText();
            if (string.IsNullOrWhiteSpace(objectiveText))
            {
                return failureText;
            }

            if (string.IsNullOrWhiteSpace(failureText))
            {
                return objectiveText;
            }

            return string.Concat(objectiveText, " • ", failureText);
        }

        public string BuildFailureConditionsText()
        {
            var failureText = FailurePolicy.BuildFailureConditionsText();
            if (string.IsNullOrWhiteSpace(failureText))
            {
                return "Manual cancel";
            }

            return string.Concat(failureText, " • Manual cancel");
        }
    }
}
