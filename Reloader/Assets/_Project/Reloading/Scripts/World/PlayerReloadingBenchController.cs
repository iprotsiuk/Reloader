using Reloader.Player;
using Reloader.Core.Events;
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
        private IReloadingBenchTarget _activeTarget;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(IPlayerInputSource inputSource, IPlayerReloadingBenchResolver resolver)
        {
            _inputSource = inputSource;
            _resolver = resolver;
        }

        public void Tick()
        {
            ResolveReferences();
            if (_resolver == null || !_resolver.TryResolveBenchTarget(out var target) || target == null)
            {
                CloseActiveWorkbenchIfAny();
                return;
            }

            if (_activeTarget != null && !ReferenceEquals(_activeTarget, target) && _activeTarget.IsWorkbenchOpen)
            {
                _activeTarget.CloseWorkbench();
                GameEvents.RaiseWorkbenchMenuVisibilityChanged(false);
            }

            _activeTarget = target;

            if (!IsPickupPressedThisFrame())
            {
                return;
            }

            target.OpenWorkbench();
            GameEvents.RaiseWorkbenchMenuVisibilityChanged(true);
        }

        private void CloseActiveWorkbenchIfAny()
        {
            if (_activeTarget != null && _activeTarget.IsWorkbenchOpen)
            {
                _activeTarget.CloseWorkbench();
                GameEvents.RaiseWorkbenchMenuVisibilityChanged(false);
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
