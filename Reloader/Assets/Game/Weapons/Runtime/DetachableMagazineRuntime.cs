using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class DetachableMagazineRuntime : MonoBehaviour
    {
        [SerializeField] private Transform _magazineSocket;
        [SerializeField] private Transform _magazineDropSocket;
        [SerializeField] private MagazineAttachmentDefinition _defaultAttachment;

        private MagazineAttachmentDefinition _activeAttachment;
        private GameObject _attachedMagazineVisual;

        private void Awake()
        {
            if (_defaultAttachment != null)
            {
                SetAttachment(_defaultAttachment);
            }
        }

        public void SetAttachment(MagazineAttachmentDefinition definition)
        {
            _activeAttachment = definition;
            RebuildAttachedMagazineVisual();
        }

        public void HandleReloadStarted(string _)
        {
            if (_activeAttachment == null || !_activeAttachment.DetachOnReloadStart)
            {
                return;
            }

            if (_attachedMagazineVisual != null)
            {
                _attachedMagazineVisual.SetActive(false);
            }

            if (!_activeAttachment.SpawnDroppedMagazine || _activeAttachment.DroppedMagazinePrefab == null)
            {
                return;
            }

            var dropOrigin = _magazineDropSocket != null ? _magazineDropSocket : _magazineSocket;
            if (dropOrigin == null)
            {
                dropOrigin = transform;
            }

            var dropped = Instantiate(_activeAttachment.DroppedMagazinePrefab, dropOrigin.position, dropOrigin.rotation);
            if (_activeAttachment.DroppedMagazineLifetimeSeconds > 0f)
            {
                Destroy(dropped, _activeAttachment.DroppedMagazineLifetimeSeconds);
            }
        }

        public void HandleMagazineInserted(string _)
        {
            if (_attachedMagazineVisual == null)
            {
                RebuildAttachedMagazineVisual();
            }

            if (_attachedMagazineVisual != null)
            {
                _attachedMagazineVisual.SetActive(true);
            }
        }

        public void HandleReloadCompleted(string itemId)
        {
            HandleMagazineInserted(itemId);
        }

        private void RebuildAttachedMagazineVisual()
        {
            if (_attachedMagazineVisual != null)
            {
                Destroy(_attachedMagazineVisual);
                _attachedMagazineVisual = null;
            }

            if (_activeAttachment == null || _activeAttachment.MagazineVisualPrefab == null || _magazineSocket == null)
            {
                return;
            }

            _attachedMagazineVisual = Instantiate(_activeAttachment.MagazineVisualPrefab, _magazineSocket, false);
        }
    }
}
