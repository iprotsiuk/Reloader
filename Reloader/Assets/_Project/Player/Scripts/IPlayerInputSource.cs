using UnityEngine;

namespace Reloader.Player
{
    public interface IPlayerInputSource
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }
        bool SprintHeld { get; }
        bool AimHeld { get; }
        bool ConsumeJumpPressed();
        bool ConsumeFirePressed();
        bool ConsumeReloadPressed();
        bool ConsumePickupPressed();
        int ConsumeBeltSelectPressed();
    }
}
