using Reloader.NPCs.Data;
using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public sealed class DialogueProximityInitiator : MonoBehaviour
    {
        [SerializeField] private DialogueDefinition _definition;
        [SerializeField] private Transform _speakerTransformOverride;
        [SerializeField] private Transform _playerTransformOverride;
        [SerializeField] private float _triggerDistanceMeters = 2f;
        [SerializeField] private bool _oneShot;
        [SerializeField] private float _cooldownSeconds;

        private bool _hasTriggered;
        private float _cooldownRemainingSeconds;
        private Transform _resolvedPlayerTransform;

        private void Update()
        {
            if (_cooldownRemainingSeconds > 0f)
            {
                _cooldownRemainingSeconds = Mathf.Max(0f, _cooldownRemainingSeconds - Time.deltaTime);
            }

            Tick();
        }

        public void Tick()
        {
            if (_definition == null || !_definition.IsValid(out _))
            {
                return;
            }

            if (_oneShot && _hasTriggered)
            {
                return;
            }

            if (_cooldownRemainingSeconds > 0f)
            {
                return;
            }

            var playerTransform = ResolvePlayerTransform();
            var speakerTransform = _speakerTransformOverride != null ? _speakerTransformOverride : transform;
            if (playerTransform == null || speakerTransform == null)
            {
                return;
            }

            var triggerDistance = Mathf.Max(0f, _triggerDistanceMeters);
            if ((playerTransform.position - speakerTransform.position).sqrMagnitude > triggerDistance * triggerDistance)
            {
                return;
            }

            var request = new DialogueStartRequest(
                _definition,
                speakerTransform,
                DialogueStartSourceKind.NpcInitiated,
                string.Empty,
                DialogueInterruptPolicy.DenyIfActive);
            if (!DialogueOrchestrator.TryStartConversation(this, request, out _))
            {
                return;
            }

            _hasTriggered = true;
            _cooldownRemainingSeconds = Mathf.Max(0f, _cooldownSeconds);
        }

        private Transform ResolvePlayerTransform()
        {
            if (_playerTransformOverride != null)
            {
                return _playerTransformOverride;
            }

            if (_resolvedPlayerTransform != null)
            {
                return _resolvedPlayerTransform;
            }

            var interactionController = FindFirstObjectByType<PlayerNpcInteractionController>(FindObjectsInactive.Include);
            if (interactionController != null)
            {
                _resolvedPlayerTransform = interactionController.transform;
                return _resolvedPlayerTransform;
            }

            var cursorLockController = FindFirstObjectByType<PlayerCursorLockController>(FindObjectsInactive.Include);
            if (cursorLockController != null)
            {
                _resolvedPlayerTransform = cursorLockController.transform;
                return _resolvedPlayerTransform;
            }

            var mover = FindFirstObjectByType<PlayerMover>(FindObjectsInactive.Include);
            if (mover != null)
            {
                _resolvedPlayerTransform = mover.transform;
                return _resolvedPlayerTransform;
            }

            return null;
        }
    }
}
