namespace Reloader.Contracts.Runtime
{
    public interface IContractTargetEliminationSink
    {
        void ReportContractTargetEliminated(string targetId, bool wasExposed);
    }
}
