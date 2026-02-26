using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    public sealed class ViewmodelAnimationAdapter : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationContractProfile _contractProfile;

        private int _fireTriggerHash;
        private int _reloadTriggerHash;
        private int _isReloadingHash;
        private int _isAimingHash;
        private int _aimWeightHash;
        private string _equippedItemId;
        private IWeaponEvents _weaponEvents;
        private IWeaponEvents _subscribedWeaponEvents;
        private bool _useRuntimeKernelWeaponEvents = true;

        public bool IsReloadingDebug { get; private set; }
        public bool IsAimingDebug { get; private set; }
        public float AimWeightDebug { get; private set; }
        public int FireTriggerCountDebug { get; private set; }

        private void Awake()
        {
            CacheHashes();
            ResolveAnimator();
        }

        private void OnEnable()
        {
            CacheHashes();
            ResolveAnimator();
            SubscribeToRuntimeHubReconfigure();
            SubscribeToWeaponEvents(ResolveWeaponEvents());
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromWeaponEvents();
        }

        public void SetEquippedItemIdForTests(string itemId)
        {
            _equippedItemId = itemId;
        }

        public void Configure(Animator animator, AnimationContractProfile contractProfile = null)
        {
            _animator = animator;
            _contractProfile = contractProfile;
            CacheHashes();
        }

        public void ConfigureEventChannel(IWeaponEvents weaponEvents = null)
        {
            _useRuntimeKernelWeaponEvents = weaponEvents == null;
            _weaponEvents = weaponEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToWeaponEvents(ResolveWeaponEvents());
            }
        }

        private void HandleWeaponEquipped(string itemId)
        {
            _equippedItemId = itemId;
            IsReloadingDebug = false;
            SetBool(_isReloadingHash, false);
        }

        private void HandleWeaponFired(string itemId, Vector3 _, Vector3 __)
        {
            if (!MatchesEquipped(itemId))
            {
                return;
            }

            FireTriggerCountDebug++;
            SetTrigger(_fireTriggerHash);
        }

        private void HandleWeaponReloadStarted(string itemId)
        {
            if (!MatchesEquipped(itemId))
            {
                return;
            }

            IsReloadingDebug = true;
            SetBool(_isReloadingHash, true);
            SetTrigger(_reloadTriggerHash);
        }

        private void HandleWeaponReloadCancelled(string itemId, WeaponReloadCancelReason _)
        {
            if (!MatchesEquipped(itemId))
            {
                return;
            }

            IsReloadingDebug = false;
            SetBool(_isReloadingHash, false);
        }

        private void HandleWeaponReloaded(string itemId, int _, int __)
        {
            if (!MatchesEquipped(itemId))
            {
                return;
            }

            IsReloadingDebug = false;
            SetBool(_isReloadingHash, false);
        }

        private void HandleWeaponAimChanged(string itemId, bool isAiming)
        {
            if (!MatchesEquipped(itemId))
            {
                return;
            }

            IsAimingDebug = isAiming;
            AimWeightDebug = isAiming ? 1f : 0f;
            SetBool(_isAimingHash, isAiming);
            SetFloat(_aimWeightHash, AimWeightDebug);
        }

        private bool MatchesEquipped(string itemId)
        {
            return string.IsNullOrWhiteSpace(_equippedItemId) || _equippedItemId == itemId;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelWeaponEvents)
            {
                return;
            }

            SubscribeToWeaponEvents(ResolveWeaponEvents());
        }

        private IWeaponEvents ResolveWeaponEvents()
        {
            if (_useRuntimeKernelWeaponEvents)
            {
                var runtimeWeaponEvents = RuntimeKernelBootstrapper.WeaponEvents;
                if (!ReferenceEquals(_weaponEvents, runtimeWeaponEvents))
                {
                    _weaponEvents = runtimeWeaponEvents;
                    SubscribeToWeaponEvents(_weaponEvents);
                }
                else if (!ReferenceEquals(_subscribedWeaponEvents, _weaponEvents))
                {
                    SubscribeToWeaponEvents(_weaponEvents);
                }
            }
            else if (!ReferenceEquals(_subscribedWeaponEvents, _weaponEvents))
            {
                SubscribeToWeaponEvents(_weaponEvents);
            }

            return _weaponEvents;
        }

        private void SubscribeToWeaponEvents(IWeaponEvents weaponEvents)
        {
            if (weaponEvents == null)
            {
                UnsubscribeFromWeaponEvents();
                return;
            }

            if (ReferenceEquals(_subscribedWeaponEvents, weaponEvents))
            {
                return;
            }

            UnsubscribeFromWeaponEvents();
            _subscribedWeaponEvents = weaponEvents;
            _subscribedWeaponEvents.OnWeaponEquipped += HandleWeaponEquipped;
            _subscribedWeaponEvents.OnWeaponFired += HandleWeaponFired;
            _subscribedWeaponEvents.OnWeaponReloadStarted += HandleWeaponReloadStarted;
            _subscribedWeaponEvents.OnWeaponReloadCancelled += HandleWeaponReloadCancelled;
            _subscribedWeaponEvents.OnWeaponReloaded += HandleWeaponReloaded;
            _subscribedWeaponEvents.OnWeaponAimChanged += HandleWeaponAimChanged;
        }

        private void UnsubscribeFromWeaponEvents()
        {
            if (_subscribedWeaponEvents == null)
            {
                return;
            }

            _subscribedWeaponEvents.OnWeaponEquipped -= HandleWeaponEquipped;
            _subscribedWeaponEvents.OnWeaponFired -= HandleWeaponFired;
            _subscribedWeaponEvents.OnWeaponReloadStarted -= HandleWeaponReloadStarted;
            _subscribedWeaponEvents.OnWeaponReloadCancelled -= HandleWeaponReloadCancelled;
            _subscribedWeaponEvents.OnWeaponReloaded -= HandleWeaponReloaded;
            _subscribedWeaponEvents.OnWeaponAimChanged -= HandleWeaponAimChanged;
            _subscribedWeaponEvents = null;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }

        private void ResolveAnimator()
        {
            _animator ??= GetComponentInChildren<Animator>(true);
        }

        private void CacheHashes()
        {
            _fireTriggerHash = Animator.StringToHash(ResolveName(_contractProfile != null ? _contractProfile.FireTrigger : null, "Fire"));
            _reloadTriggerHash = Animator.StringToHash(ResolveName(_contractProfile != null ? _contractProfile.ReloadTrigger : null, "Reload"));
            _isReloadingHash = Animator.StringToHash(ResolveName(_contractProfile != null ? _contractProfile.IsReloadingParameter : null, "IsReloading"));
            _isAimingHash = Animator.StringToHash(ResolveName(_contractProfile != null ? _contractProfile.IsAimingParameter : null, "IsAiming"));
            _aimWeightHash = Animator.StringToHash(ResolveName(_contractProfile != null ? _contractProfile.AimWeightParameter : null, "AimWeight"));
        }

        private static string ResolveName(string configured, string fallback)
        {
            return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
        }

        private void SetTrigger(int hash)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetTrigger(hash);
        }

        private void SetBool(int hash, bool value)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetBool(hash, value);
        }

        private void SetFloat(int hash, float value)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetFloat(hash, value);
        }
    }
}
