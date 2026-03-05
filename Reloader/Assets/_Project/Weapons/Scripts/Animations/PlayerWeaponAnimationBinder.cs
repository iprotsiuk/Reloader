using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Weapons.Animations
{
    public sealed class PlayerWeaponAnimationBinder : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private WeaponAnimatorOverrideProfile _animationProfile;

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
            if (!IsAnimatorOnPlayerHierarchy(_animator))
            {
                _animator = ResolveViewmodelAnimator();
            }

            if (_animator != null)
            {
                PlayerArmsAnimationEventReceiver.EnsureReceiver(_animator);
            }

            _weaponController ??= GetComponent<Controllers.PlayerWeaponController>();
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
            if (controller == null || _animator.runtimeAnimatorController == controller)
            {
                return;
            }

            _animator.runtimeAnimatorController = controller;
        }

        private Animator ResolveViewmodelAnimator()
        {
            var explicitPath = transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual");
            if (explicitPath != null)
            {
                var explicitAnimator = explicitPath.GetComponent<Animator>() ?? explicitPath.GetComponentInChildren<Animator>(true);
                if (explicitAnimator != null)
                {
                    return explicitAnimator;
                }
            }

            var byName = FindDescendantByName(transform, "PlayerArmsVisual");
            if (byName != null)
            {
                var namedAnimator = byName.GetComponent<Animator>() ?? byName.GetComponentInChildren<Animator>(true);
                if (namedAnimator != null)
                {
                    return namedAnimator;
                }
            }

            return GetComponentInChildren<Animator>(true);
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendantByName(root.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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
