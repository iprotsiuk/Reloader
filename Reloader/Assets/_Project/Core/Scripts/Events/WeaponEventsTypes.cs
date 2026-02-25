namespace Reloader.Core.Events
{
    public enum WeaponReloadCancelReason
    {
        Sprint = 0,
        Unequip = 1,
        DryStateInvalidated = 2,
        InterruptedByAction = 3
    }
}
