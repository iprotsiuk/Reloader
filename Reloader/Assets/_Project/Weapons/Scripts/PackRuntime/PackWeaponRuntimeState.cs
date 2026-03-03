using UnityEngine;

namespace Reloader.Weapons.PackRuntime
{
    public sealed class PackWeaponRuntimeState
    {
        public PackWeaponRuntimeState(string itemId)
        {
            ItemId = itemId ?? string.Empty;
        }

        public string ItemId { get; }
        public bool IsEquipped { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadCompleteTime { get; private set; }
        public float NextFireTime { get; private set; }
        public float AimFovVelocity { get; set; }

        public void SetEquipped(bool isEquipped)
        {
            IsEquipped = isEquipped;
            if (!isEquipped)
            {
                IsAiming = false;
                IsReloading = false;
                ReloadCompleteTime = 0f;
                AimFovVelocity = 0f;
            }
        }

        public bool SetAiming(bool isAiming)
        {
            var effectiveAim = IsEquipped && isAiming && !IsReloading;
            if (IsAiming == effectiveAim)
            {
                return false;
            }

            IsAiming = effectiveAim;
            return true;
        }

        public bool StartReload(float now, float durationSeconds)
        {
            if (!IsEquipped || IsReloading)
            {
                return false;
            }

            IsReloading = true;
            IsAiming = false;
            ReloadCompleteTime = now + Mathf.Max(0.01f, durationSeconds);
            return true;
        }

        public bool CancelReload()
        {
            if (!IsReloading)
            {
                return false;
            }

            IsReloading = false;
            ReloadCompleteTime = 0f;
            return true;
        }

        public bool TryCompleteReload(float now)
        {
            if (!IsReloading || now < ReloadCompleteTime)
            {
                return false;
            }

            IsReloading = false;
            ReloadCompleteTime = 0f;
            return true;
        }

        public bool CanFire(float now)
        {
            return IsEquipped && !IsReloading && now >= NextFireTime;
        }

        public void MarkFired(float now, float fireIntervalSeconds)
        {
            NextFireTime = now + Mathf.Max(0.01f, fireIntervalSeconds);
        }
    }
}
