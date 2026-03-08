namespace Reloader.Contracts.Runtime
{
    public readonly struct ContractOfferSnapshot
    {
        public ContractOfferSnapshot(
            bool hasAvailableContract,
            bool hasActiveContract,
            bool hasFailedContract,
            string contractId,
            string title,
            string targetId,
            string targetDisplayName,
            string targetDescription,
            string briefingText,
            float distanceBandMeters,
            int payout,
            bool canAccept,
            bool canCancel,
            bool canClaimReward,
            string statusText)
        {
            HasAvailableContract = hasAvailableContract;
            HasActiveContract = hasActiveContract;
            HasFailedContract = hasFailedContract;
            ContractId = contractId ?? string.Empty;
            Title = title ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            TargetDisplayName = targetDisplayName ?? string.Empty;
            TargetDescription = targetDescription ?? string.Empty;
            BriefingText = briefingText ?? string.Empty;
            DistanceBandMeters = distanceBandMeters;
            Payout = payout;
            CanAccept = canAccept;
            CanCancel = canCancel;
            CanClaimReward = canClaimReward;
            StatusText = statusText ?? string.Empty;
        }

        public bool HasAvailableContract { get; }
        public bool HasActiveContract { get; }
        public bool HasFailedContract { get; }
        public string ContractId { get; }
        public string Title { get; }
        public string TargetId { get; }
        public string TargetDisplayName { get; }
        public string TargetDescription { get; }
        public string BriefingText { get; }
        public float DistanceBandMeters { get; }
        public int Payout { get; }
        public bool CanAccept { get; }
        public bool CanCancel { get; }
        public bool CanClaimReward { get; }
        public string StatusText { get; }
    }
}
