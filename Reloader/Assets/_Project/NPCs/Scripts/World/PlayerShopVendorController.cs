using Reloader.Core.Events;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class PlayerShopVendorController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;

        private IPlayerInputSource _inputSource;
        private IPlayerShopVendorResolver _resolver;
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
            GameEvents.OnShopTradeOpened += HandleTradeOpened;
            GameEvents.OnShopTradeClosed += HandleTradeClosed;
        }

        private void OnDisable()
        {
            GameEvents.OnShopTradeOpened -= HandleTradeOpened;
            GameEvents.OnShopTradeClosed -= HandleTradeClosed;
            _isTradeOpen = false;
        }

        public void Configure(IPlayerInputSource inputSource, IPlayerShopVendorResolver resolver)
        {
            _inputSource = inputSource;
            _resolver = resolver;
        }

        public void Tick()
        {
            if ((_inputSource == null || _resolver == null) && !_loggedMissingDependencies)
            {
                Debug.LogError("PlayerShopVendorController requires both input source and vendor resolver references.", this);
                _loggedMissingDependencies = true;
            }

            if (_resolver == null || !_resolver.TryResolveVendorTarget(out var target) || target == null)
            {
                if (_isTradeOpen)
                {
                    GameEvents.RaiseShopTradeClosed();
                }
                return;
            }

            if (_isTradeOpen || _inputSource == null)
            {
                return;
            }

            if (!_inputSource.ConsumePickupPressed())
            {
                return;
            }

            GameEvents.RaiseShopTradeOpenRequested(target.VendorId);
            target.OnTradeOpened();
        }

        private void HandleTradeOpened(string _)
        {
            _isTradeOpen = true;
        }

        private void HandleTradeClosed()
        {
            _isTradeOpen = false;
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _resolver ??= _resolverBehaviour as IPlayerShopVendorResolver;

            if (_inputSource == null)
            {
                _inputSource = GetInterfaceFromBehaviours<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = GetInterfaceFromBehaviours<IPlayerShopVendorResolver>(GetComponents<MonoBehaviour>());
            }
        }

        private static TInterface GetInterfaceFromBehaviours<TInterface>(MonoBehaviour[] behaviours) where TInterface : class
        {
            if (behaviours == null)
            {
                return null;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is TInterface typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}
