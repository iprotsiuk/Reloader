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

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Tick();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToShopEvents(ResolveShopEvents());
        }

        private void OnDisable()
        {
            UnsubscribeFromShopEvents();
            _isTradeOpen = false;
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
            DependencyResolutionGuard.HasRequiredReferences(
                ref _loggedMissingDependencies,
                this,
                "PlayerShopVendorController requires both input source and vendor resolver references.",
                _inputSource,
                _resolver);

            var pickupPressedThisFrame = _inputSource != null && _inputSource.ConsumePickupPressed();

            if (_resolver == null || !_resolver.TryResolveVendorTarget(out var target) || target == null)
            {
                if (_isTradeOpen)
                {
                    ResolveShopEvents()?.RaiseShopTradeClosed();
                }
                return;
            }

            if (_isTradeOpen || !pickupPressedThisFrame)
            {
                return;
            }

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
                    SubscribeToShopEvents(_shopEvents);
                }
                else if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
                {
                    SubscribeToShopEvents(_shopEvents);
                }

                return _shopEvents;
            }

            if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
            {
                SubscribeToShopEvents(_shopEvents);
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
    }
}
