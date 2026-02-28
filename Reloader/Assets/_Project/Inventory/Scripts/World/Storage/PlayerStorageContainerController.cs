using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Core;
using Reloader.Player;
using Reloader.Player.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Inventory
{
    public sealed class PlayerStorageContainerController : MonoBehaviour, IPlayerInteractionCandidateProvider, IPlayerInteractionCoordinatorModeAware
    {
        private const string StorageHintContextId = "storage";
        private const string StorageHintActionText = "Open storage";

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;
        [Header("Interaction")]
        [SerializeField] private bool _interactionCoordinatorModeEnabled;
        [SerializeField] private int _interactionPriority = 45;

        private IPlayerInputSource _inputSource;
        private IPlayerStorageContainerResolver _resolver;
        private bool _flushPickupInputAtEndOfFrame;

        private void Update()
        {
            Tick();
        }

        private void LateUpdate()
        {
            if (_interactionCoordinatorModeEnabled)
            {
                return;
            }

            if (!_flushPickupInputAtEndOfFrame || _inputSource == null)
            {
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            _inputSource.ConsumePickupPressed();
        }

        public void Tick()
        {
            ResolveReferences();

            if (_resolver == null || !_resolver.TryResolveStorageContainer(out var container) || container == null)
            {
                _flushPickupInputAtEndOfFrame = !_interactionCoordinatorModeEnabled;
                if (!_interactionCoordinatorModeEnabled)
                {
                    ClearInteractionHint();
                }

                return;
            }

            var isAnyMenuOpen = RuntimeKernelBootstrapper.UiStateEvents?.IsAnyMenuOpen ?? false;
            if (!_interactionCoordinatorModeEnabled)
            {
                if (isAnyMenuOpen)
                {
                    ClearInteractionHint();
                }
                else
                {
                    PublishInteractionHint(container.DisplayName);
                }
            }

            if (_interactionCoordinatorModeEnabled)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            if (isAnyMenuOpen)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            if (!IsPickupPressedThisFrame())
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            OpenStorage(container);
        }

        public bool TryGetInteractionCandidate(out PlayerInteractionCandidate candidate)
        {
            candidate = default;
            ResolveReferences();
            if (_resolver == null || !_resolver.TryResolveStorageContainer(out var container) || container == null)
            {
                return false;
            }

            if (RuntimeKernelBootstrapper.UiStateEvents?.IsAnyMenuOpen ?? false)
            {
                return false;
            }

            var stableTieBreaker = container.GetInstanceID().ToString();
            candidate = new PlayerInteractionCandidate(
                StorageHintContextId,
                StorageHintActionText,
                container.DisplayName,
                _interactionPriority,
                stableTieBreaker,
                PlayerInteractionActionKind.Pickup,
                () => OpenStorage(container));
            return true;
        }

        public void SetInteractionCoordinatorMode(bool isEnabled)
        {
            _interactionCoordinatorModeEnabled = isEnabled;
            if (isEnabled)
            {
                _flushPickupInputAtEndOfFrame = false;
                ClearInteractionHint();
            }
        }

        private void OpenStorage(WorldStorageContainer container)
        {
            if (container == null)
            {
                return;
            }

            var runtime = container.EnsureRegistered();
            if (runtime == null)
            {
                return;
            }

            StorageUiSession.Open(runtime.ContainerId);
        }

        private bool IsPickupPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                _inputSource?.ConsumePickupPressed();
                return true;
            }

            return _inputSource != null && _inputSource.ConsumePickupPressed();
        }

        private void ResolveReferences()
        {
            if (_inputSource is UnityEngine.Object inputObject && inputObject == null)
            {
                _inputSource = null;
            }

            if (_resolver is UnityEngine.Object resolverObject && resolverObject == null)
            {
                _resolver = null;
            }

            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _resolver ??= _resolverBehaviour as IPlayerStorageContainerResolver;

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerStorageContainerResolver>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerStorageContainerResolver>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerStorageContainerResolver>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerStorageContainerResolver>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }
        }

        private static void PublishInteractionHint(string subjectText)
        {
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintShown(
                new InteractionHintPayload(StorageHintContextId, StorageHintActionText, subjectText));
        }

        private static void ClearInteractionHint()
        {
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(StorageHintContextId);
        }
    }
}
