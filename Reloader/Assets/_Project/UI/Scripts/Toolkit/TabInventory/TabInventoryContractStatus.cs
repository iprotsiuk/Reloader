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
        public string StatusText { get; }
    }
}
