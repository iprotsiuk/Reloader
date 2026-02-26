using System;
using Reloader.Core.Events;
using UnityEngine;

namespace Reloader.Core.Runtime
{
    public interface IWeaponEvents
    {
        event Action<string> OnWeaponEquipStarted;
        event Action<string> OnWeaponEquipped;
        event Action<string> OnWeaponUnequipStarted;
        event Action<string, Vector3, Vector3> OnWeaponFired;
        event Action<string> OnWeaponReloadStarted;
        event Action<string, WeaponReloadCancelReason> OnWeaponReloadCancelled;
        event Action<string, int, int> OnWeaponReloaded;
        event Action<string, bool> OnWeaponAimChanged;
        event Action<string, Vector3, float> OnProjectileHit;

        void RaiseWeaponEquipStarted(string itemId);
        void RaiseWeaponEquipped(string itemId);
        void RaiseWeaponUnequipStarted(string itemId);
        void RaiseWeaponFired(string itemId, Vector3 origin, Vector3 direction);
        void RaiseWeaponReloadStarted(string itemId);
        void RaiseWeaponReloadCancelled(string itemId, WeaponReloadCancelReason reason);
        void RaiseWeaponReloaded(string itemId, int magazineCount, int reserveCount);
        void RaiseWeaponAimChanged(string itemId, bool isAiming);
        void RaiseProjectileHit(string itemId, Vector3 point, float damage);
    }
}
