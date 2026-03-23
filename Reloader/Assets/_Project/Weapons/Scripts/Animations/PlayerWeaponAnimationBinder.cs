using Reloader.Core.Runtime;
using Reloader.Player;
using UnityEngine;

namespace Reloader.Weapons.Animations
{
    public sealed class PlayerWeaponAnimationBinder : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private WeaponAnimatorOverrideProfile _animationProfile;
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;

        private IWeaponEvents _subscribedWeaponEvents;
        private Controllers.PlayerWeaponController _weaponController;
        private IWeaponEvents _weaponEvents;
        private bool _useRuntimeKernelWeaponEvents = true;

        private void Awake()
        {
            ResolveReferences();
            ApplyController(_weaponController != null ? _weaponController.EquippedItemId : null);
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRuntimeHubReconfigure();
            SubscribeToWeaponEvents(ResolveWeaponEvents());
            ApplyController(_weaponController != null ? _weaponController.EquippedItemId : null);
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromWeaponEvents();
        }

        public void Configure(Animator animator, WeaponAnimatorOverrideProfile profile)
        {
            _animator = animator;
            _animationProfile = profile;
            ResolveReferences();
            ApplyController(_weaponController != null ? _weaponController.EquippedItemId : null);
        }

        public void ConfigureEventChannel(IWeaponEvents weaponEvents = null)
        {
            _useRuntimeKernelWeaponEvents = weaponEvents == null;
            _weaponEvents = weaponEvents;
            if (!isActiveAndEnabled)
            {
                return;
            }

            SubscribeToWeaponEvents(ResolveWeaponEvents());
        }

        private void ResolveReferences()
        {
            _cameraDefaults ??= GetComponent<PlayerCameraDefaults>();
            _weaponController ??= GetComponent<Controllers.PlayerWeaponController>();
            if (!IsAnimatorOnPlayerHierarchy(_animator))
            {
                _animator = ResolveViewmodelAnimator();
            }

            if (_animator != null)
            {
                PlayerArmsAnimationEventReceiver.EnsureReceiver(_animator);
            }
        }

        private void HandleWeaponEquipped(string itemId)
        {
            ApplyController(itemId);
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SubscribeToWeaponEvents(ResolveWeaponEvents());
        }

        private void SubscribeToWeaponEvents(IWeaponEvents weaponEvents)
        {
            if (ReferenceEquals(_subscribedWeaponEvents, weaponEvents))
            {
                return;
            }

            UnsubscribeFromWeaponEvents();
            _subscribedWeaponEvents = weaponEvents;
            if (_subscribedWeaponEvents == null)
            {
                return;
            }

            _subscribedWeaponEvents.OnWeaponEquipped += HandleWeaponEquipped;
        }

        private void UnsubscribeFromWeaponEvents()
        {
            if (_subscribedWeaponEvents == null)
            {
                return;
            }

            _subscribedWeaponEvents.OnWeaponEquipped -= HandleWeaponEquipped;
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

        private void ApplyController(string itemId)
        {
            if (_animator == null || _animationProfile == null)
            {
                return;
            }

            var controller = _animationProfile.ResolveController(itemId);
            if (controller == null)
            {
                return;
            }

            if (_animator.runtimeAnimatorController != controller)
            {
                _animator.runtimeAnimatorController = controller;
            }

            ForceAnimatorEvaluation();
        }

        private void ForceAnimatorEvaluation()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || !_animator.gameObject.activeInHierarchy)
            {
                return;
            }

            _animator.Update(0f);
        }

        private Animator ResolveViewmodelAnimator()
        {
            if (_cameraDefaults != null && _cameraDefaults.TryGetPlayerArmsAnimator(out var playerArmsAnimator))
            {
                return playerArmsAnimator;
            }

            return _weaponController != null ? _weaponController.PackAnimator : null;
        }

        private bool IsAnimatorOnPlayerHierarchy(Animator animator)
        {
            return animator != null
                && animator.transform != null
                && (animator.transform == transform || animator.transform.IsChildOf(transform));
        }

        private IWeaponEvents ResolveWeaponEvents()
        {
            return _useRuntimeKernelWeaponEvents ? RuntimeKernelBootstrapper.WeaponEvents : _weaponEvents;
        }
    }
}
