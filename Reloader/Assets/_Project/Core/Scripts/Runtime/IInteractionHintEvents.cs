using System;
using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public interface IInteractionHintEvents
    {
        bool HasInteractionHint { get; }
        InteractionHintPayload CurrentInteractionHint { get; }

        event Action<InteractionHintPayload> OnInteractionHintShown;
        event Action OnInteractionHintCleared;

        void RaiseInteractionHintShown(InteractionHintPayload payload);
        void RaiseInteractionHintCleared();
    }
}
