using System;
using Reloader.Core;
using Reloader.NPCs.Runtime;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class PlayerNpcInteractionController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;

        private IPlayerInputSource _inputSource;
        private IPlayerNpcResolver _resolver;
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
                return;
            }

            if (!_resolver.TryResolveNpcAgent(out var target) || target == null)
            {
                _flushPickupInputAtEndOfFrame = true;
                return;
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

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            if (_resolver == null)
            {
                _resolver = DependencyResolutionGuard.FindInterface<IPlayerNpcResolver>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }
        }
    }
}
