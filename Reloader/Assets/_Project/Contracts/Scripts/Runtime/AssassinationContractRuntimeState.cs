namespace Reloader.Contracts.Runtime
{
    public sealed class AssassinationContractRuntimeState
    {
        public AssassinationContractRuntimeState(string contractId, string targetId, float distanceBand, int payout)
        {
            ContractId = contractId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            DistanceBand = distanceBand;
            Payout = payout;
        }

        public string ContractId { get; }
        public string TargetId { get; }
        public float DistanceBand { get; }
        public int Payout { get; }
    }
}
