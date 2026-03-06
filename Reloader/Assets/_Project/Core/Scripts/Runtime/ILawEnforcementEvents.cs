using System;
using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public interface ILawEnforcementEvents
    {
        event Action<PoliceHeatState> OnHeatChanged;

        void RaiseHeatChanged(PoliceHeatState state);
    }
}
