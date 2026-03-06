using System;

namespace Reloader.Core.Runtime
{
    public interface IContractEvents
    {
        event Action<string> OnContractAccepted;
        event Action<string> OnContractFailed;
        event Action<string, int> OnContractCompleted;

        void RaiseContractAccepted(string contractId);
        void RaiseContractFailed(string contractId);
        void RaiseContractCompleted(string contractId, int payout);
    }
}
