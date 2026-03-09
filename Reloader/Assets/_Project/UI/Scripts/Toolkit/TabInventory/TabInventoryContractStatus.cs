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
            string statusText,
            string trackingText = "",
            bool hasFailedContract = false,
            string restrictionsText = "",
            string failureConditionsText = "Manual cancel",
            bool canClearFailed = false)
        {
            HasAvailableContract = hasAvailableContract;
            HasActiveContract = hasActiveContract;
            HasFailedContract = hasFailedContract;
            ContractTitle = contractTitle ?? string.Empty;
            TargetDisplayName = targetDisplayName ?? string.Empty;
            TargetDescription = targetDescription ?? string.Empty;
            BriefingText = briefingText ?? string.Empty;
            DistanceBandMeters = distanceBandMeters;
            Payout = payout;
            CanAccept = canAccept;
            CanCancel = canCancel;
            CanClaimReward = canClaimReward;
            CanClearFailed = canClearFailed;
            StatusText = statusText ?? string.Empty;
            RestrictionsText = restrictionsText ?? string.Empty;
            FailureConditionsText = failureConditionsText ?? string.Empty;
            TrackingText = trackingText ?? string.Empty;
        }

        public bool HasAvailableContract { get; }
        public bool HasActiveContract { get; }
        public bool HasFailedContract { get; }
        public string ContractTitle { get; }
        public string TargetDisplayName { get; }
        public string TargetDescription { get; }
        public string BriefingText { get; }
        public float DistanceBandMeters { get; }
        public int Payout { get; }
        public bool CanAccept { get; }
        public bool CanCancel { get; }
        public bool CanClaimReward { get; }
        public bool CanClearFailed { get; }
        public string StatusText { get; }
        public string RestrictionsText { get; }
        public string FailureConditionsText { get; }
        public string TrackingText { get; }

        public static TabInventoryContractStatus CreateDefault()
        {
            return new TabInventoryContractStatus(
                hasAvailableContract: false,
                hasActiveContract: false,
                hasFailedContract: false,
                contractTitle: "No posted contracts",
                targetDisplayName: "--",
                targetDescription: "Check back later for fresh contract offers.",
                briefingText: "Check back later for fresh contract offers.",
                distanceBandMeters: 0f,
                payout: 0,
                canAccept: false,
                canCancel: false,
                canClaimReward: false,
                canClearFailed: false,
                statusText: "No contracts currently posted",
                restrictionsText: "None",
                failureConditionsText: "Manual cancel",
                trackingText: string.Empty);
        }
    }
}
