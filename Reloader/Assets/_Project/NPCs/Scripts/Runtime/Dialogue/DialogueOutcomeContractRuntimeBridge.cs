using Reloader.Contracts.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public sealed class DialogueOutcomeContractRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _runtimeControllerSource;
        [SerializeField] private MonoBehaviour _contractRuntimeProviderSource;

        private DialogueRuntimeController _runtimeController;
        private DialogueRuntimeController _subscribedRuntimeController;
        private StaticContractRuntimeProvider _contractRuntimeProvider;

        private void OnEnable()
        {
            RebindRuntime();
        }

        private void Update()
        {
            if (_subscribedRuntimeController == null)
            {
                RebindRuntime();
            }
        }

        private void OnDisable()
        {
            UnbindRuntime();
        }

        private void HandleDialogueConfirmed(DialogueConfirmResult result)
        {
            if (!result.Success || string.IsNullOrWhiteSpace(result.Outcome.ActionId))
            {
                return;
            }

            ResolveProvider()?.TryHandleDialogueAction(result.Outcome.ActionId, result.Outcome.Payload);
        }

        private void RebindRuntime()
        {
            var resolved = ResolveRuntime();
            if (ReferenceEquals(_subscribedRuntimeController, resolved))
            {
                return;
            }

            UnbindRuntime();
            _subscribedRuntimeController = resolved;
            if (_subscribedRuntimeController != null)
            {
                _subscribedRuntimeController.DialogueConfirmed += HandleDialogueConfirmed;
            }
        }

        private void UnbindRuntime()
        {
            if (_subscribedRuntimeController == null)
            {
                return;
            }

            _subscribedRuntimeController.DialogueConfirmed -= HandleDialogueConfirmed;
            _subscribedRuntimeController = null;
        }

        private DialogueRuntimeController ResolveRuntime()
        {
            _runtimeController ??= _runtimeControllerSource as DialogueRuntimeController;
            _runtimeController ??= GetComponent<DialogueRuntimeController>();
            _runtimeController ??= GetComponentInParent<DialogueRuntimeController>(true);
            _runtimeController ??= GetComponentInChildren<DialogueRuntimeController>(true);
            _runtimeController ??= FindFirstObjectByType<DialogueRuntimeController>(FindObjectsInactive.Include);
            return _runtimeController;
        }

        private StaticContractRuntimeProvider ResolveProvider()
        {
            _contractRuntimeProvider ??= _contractRuntimeProviderSource as StaticContractRuntimeProvider;
            _contractRuntimeProvider ??= GetComponent<StaticContractRuntimeProvider>();
            _contractRuntimeProvider ??= GetComponentInParent<StaticContractRuntimeProvider>(true);
            _contractRuntimeProvider ??= GetComponentInChildren<StaticContractRuntimeProvider>(true);
            _contractRuntimeProvider ??= FindFirstObjectByType<StaticContractRuntimeProvider>(FindObjectsInactive.Include);
            return _contractRuntimeProvider;
        }
    }
}
