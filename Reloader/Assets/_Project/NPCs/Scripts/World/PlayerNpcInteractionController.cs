using System;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.Player;
using Reloader.Player.Interaction;
using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class PlayerNpcInteractionController : MonoBehaviour, IPlayerInteractionCandidateProvider, IPlayerInteractionCoordinatorModeAware
    {
        private const string NpcHintContextId = "npc";
        private const string DefaultNpcActionText = "Interact";

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;
        [SerializeField] private MonoBehaviour _conversationModeSource;
        [Header("Interaction")]
        [SerializeField] private bool _interactionCoordinatorModeEnabled;
        [SerializeField] private int _interactionPriority = 40;

        private IPlayerInputSource _inputSource;
        private IPlayerNpcResolver _resolver;
        private DialogueConversationModeController _conversationModeController;
        private DialogueRuntimeController _dialogueRuntimeController;
        private bool _loggedMissingDependencies;
        private bool _flushPickupInputAtEndOfFrame;

        public event Action<NpcActionExecutionResult> InteractionProcessed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            _flushPickupInputAtEndOfFrame = false;
            if (!_interactionCoordinatorModeEnabled)
            {
                ClearInteractionHint();
            }
        }

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

        public void Configure(IPlayerInputSource inputSource, IPlayerNpcResolver resolver)
        {
            _inputSource = inputSource;
            _resolver = resolver;
        }

        public void Tick()
        {
            if (_inputSource == null || _resolver == null)
            {
                ResolveReferences();
            }

            if (!DependencyResolutionGuard.HasRequiredReferences(
                    ref _loggedMissingDependencies,
                    this,
                    "PlayerNpcInteractionController requires both input source and npc resolver references.",
                    _inputSource,
                    _resolver))
            {
                if (!_interactionCoordinatorModeEnabled)
                {
                    ClearInteractionHint();
                }

                return;
            }

            if (_interactionCoordinatorModeEnabled)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            if (IsConversationActive())
            {
                _flushPickupInputAtEndOfFrame = false;
                ClearInteractionHint();
                return;
            }

            if (!_resolver.TryResolveNpcAgent(out var target) || target == null)
            {
                _flushPickupInputAtEndOfFrame = true;
                ClearInteractionHint();
                return;
            }

            if (RuntimeKernelBootstrapper.UiStateEvents != null && RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen)
            {
                ClearInteractionHint();
            }
            else
            {
                PublishInteractionHint(target);
            }

            var pickupPressedThisFrame = _inputSource.ConsumePickupPressed();
            if (!pickupPressedThisFrame)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            TryInteract(target);
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

        public bool TryGetInteractionCandidate(out PlayerInteractionCandidate candidate)
        {
            candidate = default;
            if (_resolver == null)
            {
                ResolveReferences();
            }

            if (_resolver == null || !_resolver.TryResolveNpcAgent(out var target) || target == null)
            {
                return false;
            }

            var uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            if (IsConversationActive() || (uiStateEvents != null && uiStateEvents.IsAnyMenuOpen))
            {
                return false;
            }

            var actionText = DefaultNpcActionText;
            var actionKey = string.Empty;
            if (TrySelectDefaultAction(target.CollectActions(), out var selectedAction))
            {
                if (!string.IsNullOrWhiteSpace(selectedAction.DisplayName))
                {
                    actionText = selectedAction.DisplayName;
                }

                actionKey = selectedAction.ActionKey;
            }

            var stableTieBreaker = $"{target.GetInstanceID()}:{actionKey}";
            candidate = new PlayerInteractionCandidate(
                NpcHintContextId,
                actionText,
                target.name,
                _interactionPriority,
                stableTieBreaker,
                PlayerInteractionActionKind.NpcInteract,
                () => TryInteract(target));
            return true;
        }

        public bool TryInteract(string explicitActionKey = null, string explicitPayload = null)
        {
            if (_resolver == null)
            {
                ResolveReferences();
            }

            if (_resolver == null || !_resolver.TryResolveNpcAgent(out var target) || target == null)
            {
                var noTargetResult = new NpcActionExecutionResult(explicitActionKey ?? string.Empty, false, "npc.interaction.no-target");
                InteractionProcessed?.Invoke(noTargetResult);
                return false;
            }

            if (IsConversationActive())
            {
                var activeConversationResult = new NpcActionExecutionResult(explicitActionKey ?? string.Empty, false, "npc.interaction.conversation-active");
                InteractionProcessed?.Invoke(activeConversationResult);
                return false;
            }

            return TryInteract(target, explicitActionKey, explicitPayload);
        }

        private bool TryInteract(NpcAgent target, string explicitActionKey = null, string explicitPayload = null)
        {
            var actions = target.CollectActions();

            var actionKey = explicitActionKey;
            var payload = explicitPayload ?? string.Empty;
            if (string.IsNullOrWhiteSpace(actionKey))
            {
                if (!TrySelectDefaultAction(actions, out var selectedAction))
                {
                    var noActionsResult = new NpcActionExecutionResult(string.Empty, false, "npc.interaction.no-actions");
                    InteractionProcessed?.Invoke(noActionsResult);
                    return false;
                }

                actionKey = selectedAction.ActionKey;
                payload = selectedAction.Payload;
            }
            else if (string.IsNullOrEmpty(payload) && TryFindAction(actions, actionKey, out var providedAction))
            {
                payload = providedAction.Payload;
            }

            var executed = target.TryExecuteAction(actionKey, payload, out var executionResult);
            InteractionProcessed?.Invoke(executionResult);
            return executed;
        }

        private static bool TryFindAction(NpcActionCollection actions, string actionKey, out NpcActionDefinition action)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (!string.Equals(actions[i].ActionKey, actionKey, StringComparison.Ordinal))
                {
                    continue;
                }

                action = actions[i];
                return true;
            }

            action = default;
            return false;
        }

        private static void ClearInteractionHint()
        {
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(NpcHintContextId);
        }

        private void PublishInteractionHint(NpcAgent target)
        {
            var actionText = DefaultNpcActionText;
            if (target != null && TrySelectDefaultAction(target.CollectActions(), out var selectedAction))
            {
                if (!string.IsNullOrWhiteSpace(selectedAction.DisplayName))
                {
                    actionText = selectedAction.DisplayName;
                }
            }

            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintShown(
                new InteractionHintPayload(NpcHintContextId, actionText));
        }

        private static bool TrySelectDefaultAction(NpcActionCollection actions, out NpcActionDefinition selected)
        {
            if (actions == null || actions.Count == 0)
            {
                selected = default;
                return false;
            }

            selected = actions[0];
            for (var i = 1; i < actions.Count; i++)
            {
                if (actions[i].Priority > selected.Priority)
                {
                    selected = actions[i];
                }
            }

            return true;
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _resolver ??= _resolverBehaviour as IPlayerNpcResolver;
            _conversationModeController ??= _conversationModeSource as DialogueConversationModeController;
            _dialogueRuntimeController ??= _conversationModeSource as DialogueRuntimeController;

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(GetComponents<MonoBehaviour>());
            }

            _conversationModeController ??= GetComponent<DialogueConversationModeController>();
            _dialogueRuntimeController ??= GetComponent<DialogueRuntimeController>();

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(GetComponentsInParent<MonoBehaviour>(true));
            }

            _conversationModeController ??= GetComponentInParent<DialogueConversationModeController>(true);
            _dialogueRuntimeController ??= GetComponentInParent<DialogueRuntimeController>(true);

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            _conversationModeController ??= GetComponentInChildren<DialogueConversationModeController>(true);
            _dialogueRuntimeController ??= GetComponentInChildren<DialogueRuntimeController>(true);

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            _conversationModeController ??= FindFirstObjectByType<DialogueConversationModeController>(FindObjectsInactive.Include);
            _dialogueRuntimeController ??= FindFirstObjectByType<DialogueRuntimeController>(FindObjectsInactive.Include);
        }

        private bool IsConversationActive()
        {
            if (_dialogueRuntimeController == null && _conversationModeController == null)
            {
                ResolveReferences();
            }

            if (_dialogueRuntimeController != null)
            {
                return _dialogueRuntimeController.HasActiveConversation;
            }

            return _conversationModeController != null && _conversationModeController.IsConversationActive;
        }
    }
}
