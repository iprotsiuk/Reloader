namespace Reloader.Reloading.Runtime
{
    public readonly struct MockOperationResult
    {
        public MockOperationResult(
            bool success,
            ReloadingOperationType operation,
            string message,
            float timeCostSeconds,
            MockMaterialDelta materialDelta)
        {
            Success = success;
            Operation = operation;
            Message = message;
            TimeCostSeconds = timeCostSeconds;
            MaterialDelta = materialDelta;
        }

        public bool Success { get; }
        public ReloadingOperationType Operation { get; }
        public string Message { get; }
        public float TimeCostSeconds { get; }
        public MockMaterialDelta MaterialDelta { get; }
    }
}
