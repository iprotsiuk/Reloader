using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class MuzzleAttachmentRuntime : MonoBehaviour
    {
        [SerializeField] private Transform _muzzleSocket;
        [SerializeField] private Transform _attachmentSlot;
        [SerializeField] private MuzzleAttachmentDefinition _defaultAttachment;

        private MuzzleAttachmentDefinition _activeAttachment;
        private GameObject _equippedMuzzleInstance;

        public MuzzleAttachmentDefinition ActiveAttachment => _activeAttachment;

        private void Awake()
        {
            if (_defaultAttachment != null)
            {
                Equip(_defaultAttachment);
            }
        }

        public void Equip(MuzzleAttachmentDefinition definition)
        {
            Unequip();
            _activeAttachment = definition;

            if (_activeAttachment == null || _activeAttachment.MuzzlePrefab == null || _attachmentSlot == null)
            {
                return;
            }

            _equippedMuzzleInstance = Instantiate(_activeAttachment.MuzzlePrefab, _attachmentSlot, false);
        }

        public void Unequip()
        {
            if (_equippedMuzzleInstance != null)
            {
                Destroy(_equippedMuzzleInstance);
                _equippedMuzzleInstance = null;
            }

            _activeAttachment = null;
        }

        public AudioClip TryGetFireClipOverride()
        {
            return _activeAttachment != null ? _activeAttachment.FireClipOverride : null;
        }

        public void HandleWeaponFired(string _)
        {
            var socket = _muzzleSocket != null ? _muzzleSocket : transform;
            if (_activeAttachment != null && _activeAttachment.FlashPrefab != null)
            {
                var flash = Instantiate(_activeAttachment.FlashPrefab, socket.position, socket.rotation, socket);
                Destroy(flash, 0.25f);
            }

            if (_activeAttachment != null && _activeAttachment.FlashLightDurationSeconds > 0f)
            {
                var lightGo = new GameObject("MuzzleFlashLight");
                lightGo.transform.SetPositionAndRotation(socket.position, socket.rotation);
                lightGo.transform.SetParent(socket, true);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.intensity = _activeAttachment.FlashLightIntensity;
                light.range = _activeAttachment.FlashLightRange;
                light.color = _activeAttachment.FlashLightColor;
                Destroy(lightGo, _activeAttachment.FlashLightDurationSeconds);
            }
        }
    }
}
