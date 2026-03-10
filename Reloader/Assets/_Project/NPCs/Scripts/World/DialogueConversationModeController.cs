using Reloader.Player;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class DialogueConversationModeController : MonoBehaviour
    {
        [SerializeField] private PlayerMover _playerMover;
        [SerializeField] private PlayerLookController _playerLookController;
        [SerializeField] private PlayerCursorLockController _cursorLockController;
        [SerializeField] private MonoBehaviour _runtimeControllerSource;

        public bool IsConversationActive { get; private set; }
        public Transform ActiveFocusTarget { get; private set; }

        private DialogueRuntimeController _runtimeController;
        private bool _runtimeOwnsConversationMode;
        private NpcDialogueFacingController _activeFacingController;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            RefreshConversationMode();
        }

        public void EnterConversation(Transform focusTarget)
        {
            ResolveReferences();
            if (focusTarget == null)
            {
                ExitConversation();
                return;
            }

            if (IsConversationActive && ReferenceEquals(ActiveFocusTarget, focusTarget))
            {
                return;
            }

            _runtimeOwnsConversationMode = false;
            ApplyConversationState(focusTarget, true);
        }

        public void ExitConversation()
        {
            _runtimeOwnsConversationMode = false;
            ApplyConversationState(null, false);
        }

        public void RefreshConversationMode()
        {
            ResolveReferences();
            if (_runtimeController == null)
            {
                if (IsConversationActive || _runtimeOwnsConversationMode)
                {
                    _runtimeOwnsConversationMode = false;
                    ApplyConversationState(null, false);
                }

                return;
            }

            if (_runtimeController.HasActiveConversation)
            {
                var speakerTransform = _runtimeController.ActiveConversation?.SpeakerTransform;
                if (speakerTransform != null)
                {
                    _runtimeOwnsConversationMode = true;
                    ApplyConversationState(speakerTransform, true);
                    return;
                }
            }

            if (_runtimeOwnsConversationMode)
            {
                _runtimeOwnsConversationMode = false;
                ApplyConversationState(null, false);
            }
        }

        private void ApplyConversationState(Transform focusTarget, bool isActive)
        {
            if (!IsConversationActive && ActiveFocusTarget == null)
            {
                if (!isActive)
                {
                    return;
                }
            }

            ActiveFocusTarget = focusTarget;
            IsConversationActive = isActive;

            _playerMover?.SetMovementLocked(isActive);
            _playerLookController?.SetFocusTargetOverride(focusTarget);
            _cursorLockController?.SetForcedCursorUnlock(isActive);

            UpdateSpeakerFacingController(focusTarget, isActive);
        }

        private void ResolveReferences()
        {
            _playerMover ??= GetComponent<PlayerMover>();
            _playerLookController ??= GetComponent<PlayerLookController>();
            _cursorLockController ??= GetComponent<PlayerCursorLockController>();
            _runtimeController ??= _runtimeControllerSource as DialogueRuntimeController;
            _runtimeController ??= GetComponent<DialogueRuntimeController>();
            _runtimeController ??= GetComponentInParent<DialogueRuntimeController>(true);
            _runtimeController ??= GetComponentInChildren<DialogueRuntimeController>(true);
            _runtimeController ??= FindFirstObjectByType<DialogueRuntimeController>(FindObjectsInactive.Include);
        }

        private void UpdateSpeakerFacingController(Transform focusTarget, bool isActive)
        {
            if (_activeFacingController != null)
            {
                _activeFacingController.StopFacing();
                _activeFacingController = null;
            }

            if (!isActive || focusTarget == null)
            {
                return;
            }

            _activeFacingController = focusTarget.GetComponentInParent<NpcDialogueFacingController>();
            _activeFacingController?.StartFacing(transform);
        }
    }
}
