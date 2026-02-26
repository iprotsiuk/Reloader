using Reloader.Core;
using Reloader.Core.Runtime;
using Reloader.Player;
using System;
using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class PlayerShopVendorController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;

        private IPlayerInputSource _inputSource;
        private IPlayerShopVendorResolver _resolver;
        private IShopEvents _shopEvents;
        private IShopEvents _subscribedShopEvents;
        private bool _useRuntimeKernelShopEvents = true;
        private bool _isTradeOpen;
        private bool _loggedMissingDependencies;
        private bool _flushPickupInputAtEndOfFrame;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Tick();
        }

        private void LateUpdate()
        {
            if (!_flushPickupInputAtEndOfFrame || _inputSource == null)
            {
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            _inputSource.ConsumePickupPressed();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRuntimeHubReconfigure();
            SubscribeToShopEvents(ResolveShopEvents());
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromShopEvents();
            _isTradeOpen = false;
            _flushPickupInputAtEndOfFrame = false;
        }

        public void Configure(IPlayerInputSource inputSource, IPlayerShopVendorResolver resolver, IShopEvents shopEvents = null)
        {
            _inputSource = inputSource;
            _resolver = resolver;
            _useRuntimeKernelShopEvents = shopEvents == null;
            _shopEvents = shopEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToShopEvents(ResolveShopEvents());
            }
        }

        public void Tick()
        {
            if (_inputSource == null || _resolver == null)
            {
                ResolveReferences();
            }

            DependencyResolutionGuard.HasRequiredReferences(
                ref _loggedMissingDependencies,
                this,
                "PlayerShopVendorController requires both input source and vendor resolver references.",
                _inputSource,
                _resolver);

            if (_resolver == null || !_resolver.TryResolveVendorTarget(out var target) || target == null)
            {
                _flushPickupInputAtEndOfFrame = true;
                if (_isTradeOpen)
                {
                    ResolveShopEvents()?.RaiseShopTradeClosed();
                }
                return;
            }

            var pickupPressedThisFrame = _inputSource != null && _inputSource.ConsumePickupPressed();
            if (_isTradeOpen || !pickupPressedThisFrame)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            ResolveShopEvents()?.RaiseShopTradeOpenRequested(target.VendorId);
            target.OnTradeOpened();
        }

        private void HandleTradeOpened(string vendorId)
        {
            if (!IsCurrentTargetVendor(vendorId))
            {
                return;
            }

            _isTradeOpen = true;
        }

        private void HandleTradeClosed()
        {
            _isTradeOpen = false;
        }

        private bool IsCurrentTargetVendor(string vendorId)
        {
            if (_resolver == null || string.IsNullOrWhiteSpace(vendorId))
            {
                return false;
            }

            if (!_resolver.TryResolveVendorTarget(out var target) || target == null)
            {
                return false;
            }

            return string.Equals(target.VendorId, vendorId, StringComparison.Ordinal);
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelShopEvents)
            {
                return;
            }

            SubscribeToShopEvents(ResolveShopEvents());
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _resolver ??= _resolverBehaviour as IPlayerShopVendorResolver;

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerShopVendorResolver>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerShopVendorResolver>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerShopVendorResolver>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerShopVendorResolver>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            ResolveShopEvents();
        }

        private IShopEvents ResolveShopEvents()
        {
            if (_useRuntimeKernelShopEvents)
            {
                var runtimeShopEvents = RuntimeKernelBootstrapper.ShopEvents;
                if (!ReferenceEquals(_shopEvents, runtimeShopEvents))
                {
                    _shopEvents = runtimeShopEvents;
                }

                return _shopEvents;
            }

            return _shopEvents;
        }

        private void SubscribeToShopEvents(IShopEvents shopEvents)
        {
            if (shopEvents == null)
            {
                UnsubscribeFromShopEvents();
                return;
            }

            if (ReferenceEquals(_subscribedShopEvents, shopEvents))
            {
                return;
            }

            UnsubscribeFromShopEvents();
            _subscribedShopEvents = shopEvents;
            _subscribedShopEvents.OnShopTradeOpened += HandleTradeOpened;
            _subscribedShopEvents.OnShopTradeClosed += HandleTradeClosed;
        }

        private void UnsubscribeFromShopEvents()
        {
            if (_subscribedShopEvents == null)
            {
                return;
            }

            _subscribedShopEvents.OnShopTradeOpened -= HandleTradeOpened;
            _subscribedShopEvents.OnShopTradeClosed -= HandleTradeClosed;
            _subscribedShopEvents = null;
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
    }
}
