namespace Reloader.UI.Toolkit.TabInventory
{
    public readonly struct TabInventoryContractStatus
    {
        public TabInventoryContractStatus(
            bool hasAvailableContract,
            bool hasActiveContract,
            string contractTitle,
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
            ContractTitle = contractTitle ?? string.Empty;
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
        public string ContractTitle { get; }
        public string TargetDisplayName { get; }
        public string TargetDescription { get; }
        public string BriefingText { get; }
        public float DistanceBandMeters { get; }
        public int Payout { get; }
        public bool CanAccept { get; }
        public bool CanCancel { get; }
        public bool CanClaimReward { get; }
        public string StatusText { get; }

        public static TabInventoryContractStatus CreateDefault()
        {
            return new TabInventoryContractStatus(
                hasAvailableContract: false,
                hasActiveContract: false,
                contractTitle: "No posted contracts",
                targetDisplayName: "--",
                targetDescription: "Check back later for fresh contract offers.",
                briefingText: "Check back later for fresh contract offers.",
                distanceBandMeters: 0f,
                payout: 0,
                canAccept: false,
                canCancel: false,
                canClaimReward: false,
                statusText: "No contracts currently posted");
        }
    }
}
