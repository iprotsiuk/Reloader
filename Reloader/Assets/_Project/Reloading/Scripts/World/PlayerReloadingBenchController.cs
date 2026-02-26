using Reloader.Player;
using Reloader.Core.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Reloading.World
{
    public sealed class PlayerReloadingBenchController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;

        private IPlayerInputSource _inputSource;
        private IPlayerReloadingBenchResolver _resolver;
        private IUiStateEvents _uiStateEvents;
        private IReloadingBenchTarget _activeTarget;
        private bool _useRuntimeKernelUiStateEvents = true;
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

        private void OnDisable()
        {
            CloseActiveWorkbenchIfAny();
            _flushPickupInputAtEndOfFrame = false;
        }

        public void Configure(IPlayerInputSource inputSource, IPlayerReloadingBenchResolver resolver, IUiStateEvents uiStateEvents = null)
        {
            _inputSource = inputSource;
            _resolver = resolver;
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
        }

        public void Tick()
        {
            ResolveReferences();

            if (_resolver == null || !_resolver.TryResolveBenchTarget(out var target) || target == null)
            {
                _flushPickupInputAtEndOfFrame = true;
                CloseActiveWorkbenchIfAny();
                return;
            }

            if (_activeTarget != null && !ReferenceEquals(_activeTarget, target) && _activeTarget.IsWorkbenchOpen)
            {
                _activeTarget.CloseWorkbench();
                RaiseWorkbenchMenuVisibilityChanged(false);
            }

            _activeTarget = target;

            var pickupPressedThisFrame = IsPickupPressedThisFrame();
            if (!pickupPressedThisFrame)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            target.OpenWorkbench();
            RaiseWorkbenchMenuVisibilityChanged(true);
        }

        private void CloseActiveWorkbenchIfAny()
        {
            if (_activeTarget != null && _activeTarget.IsWorkbenchOpen)
            {
                _activeTarget.CloseWorkbench();
                RaiseWorkbenchMenuVisibilityChanged(false);
            }

            _activeTarget = null;
        }

        private bool IsPickupPressedThisFrame()
        {
            // v0.1 workaround: inventory also consumes Pickup; checking E keeps bench interaction responsive.
            var keyboardPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (keyboardPressed)
            {
                return true;
            }

            if (_inputSource == null)
            {
                return false;
            }

            return _inputSource.ConsumePickupPressed();
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _resolver ??= _resolverBehaviour as IPlayerReloadingBenchResolver;

            if (_inputSource == null)
            {
                _inputSource = GetInterfaceFromBehaviours<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = GetInterfaceFromBehaviours<IPlayerReloadingBenchResolver>(GetComponents<MonoBehaviour>());
            }

            ResolveUiStateEvents();
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

        private void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
        {
            ResolveUiStateEvents()?.RaiseWorkbenchMenuVisibilityChanged(isVisible);
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            }

            return _uiStateEvents;
        }
    }
}
