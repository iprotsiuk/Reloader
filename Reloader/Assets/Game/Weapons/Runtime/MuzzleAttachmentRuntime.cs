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
        private bool _loggedUnsafeMuzzlePrefab;
        private bool _loggedUnsafeFlashPrefab;

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
            if (definition == null || definition.MuzzlePrefab == null || _attachmentSlot == null)
            {
                return;
            }

            if (HasMissingScriptsInPrefab(definition.MuzzlePrefab))
            {
                if (!_loggedUnsafeMuzzlePrefab)
                {
                    Debug.LogWarning("MuzzleAttachmentRuntime: Skipping muzzle attachment instantiate because prefab has missing scripts.", definition.MuzzlePrefab);
                    _loggedUnsafeMuzzlePrefab = true;
                }

                return;
            }

            _activeAttachment = definition;
            _equippedMuzzleInstance = Instantiate(_activeAttachment.MuzzlePrefab, _attachmentSlot, false);
        }

        public void Unequip()
        {
            if (_equippedMuzzleInstance != null)
            {
                _equippedMuzzleInstance.transform.SetParent(null, false);
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
                if (!HasMissingScriptsInPrefab(_activeAttachment.FlashPrefab))
                {
                    var flash = Instantiate(_activeAttachment.FlashPrefab, socket.position, socket.rotation, socket);
                    Destroy(flash, 0.25f);
                }
                else if (!_loggedUnsafeFlashPrefab)
                {
                    Debug.LogWarning("MuzzleAttachmentRuntime: Skipping flash instantiate because prefab has missing scripts.", _activeAttachment.FlashPrefab);
                    _loggedUnsafeFlashPrefab = true;
                }
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

        private static bool HasMissingScriptsInPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return true;
            }

            var components = prefab.GetComponentsInChildren<Component>(true);
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
