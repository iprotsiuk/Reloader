using System;
using UnityEngine;

namespace Reloader.Weapons.PackRuntime
{
    public sealed class PackWeaponRuntimeDriver
    {
        private Animator _animator;

        public PackWeaponRuntimeDriver(
            PackWeaponRuntimeState state,
            PackWeaponPresentationConfig presentationConfig,
            Animator animator = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            PresentationConfig = presentationConfig ?? new PackWeaponPresentationConfig();
            _animator = animator;
        }

        public PackWeaponRuntimeState State { get; }
        public PackWeaponPresentationConfig PresentationConfig { get; private set; }

        public event Action<bool> AimStateChanged;
        public event Action<bool> ReloadStateChanged;
        public event Action FirePresented;

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void SetPresentationConfig(PackWeaponPresentationConfig presentationConfig)
        {
            PresentationConfig = presentationConfig ?? new PackWeaponPresentationConfig();
        }

        public void SetEquipped(bool isEquipped)
        {
            var wasAiming = State.IsAiming;
            var wasReloading = State.IsReloading;
            State.SetEquipped(isEquipped);
            SetAnimatorBool(PresentationConfig.EquippedBoolParameter, isEquipped);
            SetAnimatorBool(PresentationConfig.AimBoolParameter, State.IsAiming);
            SetAnimatorBool(PresentationConfig.ReloadBoolParameter, State.IsReloading);

            if (wasAiming != State.IsAiming)
            {
                AimStateChanged?.Invoke(State.IsAiming);
            }

            if (wasReloading != State.IsReloading)
            {
                ReloadStateChanged?.Invoke(State.IsReloading);
            }
        }

        public float TickAimFov(bool aimHeld, float currentFov, float baseFov, float deltaTime = -1f)
        {
            var changed = State.SetAiming(aimHeld);
            if (changed)
            {
                SetAnimatorBool(PresentationConfig.AimBoolParameter, State.IsAiming);
                AimStateChanged?.Invoke(State.IsAiming);
            }

            var clampedBaseFov = Mathf.Clamp(baseFov, 1f, 179f);
            var targetFov = State.IsAiming ? Mathf.Min(clampedBaseFov, PresentationConfig.AdsFieldOfView) : clampedBaseFov;
            var aimVelocity = State.AimFovVelocity;
            var effectiveDeltaTime = deltaTime > 0f ? deltaTime : Mathf.Max(Time.deltaTime, 1f / 60f);
            var nextFov = Mathf.SmoothDamp(
                currentFov,
                targetFov,
                ref aimVelocity,
                PresentationConfig.AdsFovLerpTime,
                Mathf.Infinity,
                effectiveDeltaTime);
            State.AimFovVelocity = aimVelocity;
            return nextFov;
        }

        public bool TryStartReload(float now, float durationSeconds)
        {
            var wasAiming = State.IsAiming;
            if (!State.StartReload(now, durationSeconds))
            {
                return false;
            }

            if (wasAiming != State.IsAiming)
            {
                SetAnimatorBool(PresentationConfig.AimBoolParameter, State.IsAiming);
                AimStateChanged?.Invoke(State.IsAiming);
            }

            SetAnimatorBool(PresentationConfig.ReloadBoolParameter, true);
            SetAnimatorTrigger(PresentationConfig.ReloadTriggerParameter);
            ReloadStateChanged?.Invoke(true);
            return true;
        }

        public bool CancelReload()
        {
            if (!State.CancelReload())
            {
                return false;
            }

            SetAnimatorBool(PresentationConfig.ReloadBoolParameter, false);
            ReloadStateChanged?.Invoke(false);
            return true;
        }

        public bool TryCompleteReload(float now)
        {
            if (!State.TryCompleteReload(now))
            {
                return false;
            }

            SetAnimatorBool(PresentationConfig.ReloadBoolParameter, false);
            ReloadStateChanged?.Invoke(false);
            return true;
        }

        public bool CanFire(float now)
        {
            return State.CanFire(now);
        }

        public void NotifyFire(float now, float fireIntervalSeconds)
        {
            State.MarkFired(now, fireIntervalSeconds);
            SetAnimatorTrigger(PresentationConfig.FireTriggerParameter);
            FirePresented?.Invoke();
        }

        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (!CanDriveAnimator() || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            _animator.SetBool(parameterName, value);
        }

        private void SetAnimatorTrigger(string parameterName)
        {
            if (!CanDriveAnimator() || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            _animator.SetTrigger(parameterName);
        }

        private bool CanDriveAnimator()
        {
            return PresentationConfig.UseAnimatorHooks && _animator != null;
        }
    }
}
