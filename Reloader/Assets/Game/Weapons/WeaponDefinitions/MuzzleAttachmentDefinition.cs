using UnityEngine;

namespace Reloader.Game.Weapons
{
    [CreateAssetMenu(fileName = "MuzzleAttachmentDefinition", menuName = "Reloader/Game/Weapons/Muzzle Attachment Definition")]
    public sealed class MuzzleAttachmentDefinition : ScriptableObject
    {
        [SerializeField] private string _attachmentId = string.Empty;
        [SerializeField] private GameObject _muzzlePrefab;
        [SerializeField] private GameObject _flashPrefab;
        [SerializeField] private AudioClip _fireClipOverride;
        [SerializeField, Min(0f)] private float _flashLightIntensity = 3f;
        [SerializeField, Min(0f)] private float _flashLightRange = 4f;
        [SerializeField, Min(0f)] private float _flashLightDurationSeconds = 0.04f;
        [SerializeField] private Color _flashLightColor = new Color(1f, 0.78f, 0.4f, 1f);

        public string AttachmentId => string.IsNullOrWhiteSpace(_attachmentId) ? string.Empty : _attachmentId;
        public GameObject MuzzlePrefab => _muzzlePrefab;
        public GameObject FlashPrefab => _flashPrefab;
        public AudioClip FireClipOverride => _fireClipOverride;
        public float FlashLightIntensity => Mathf.Max(0f, _flashLightIntensity);
        public float FlashLightRange => Mathf.Max(0f, _flashLightRange);
        public float FlashLightDurationSeconds => Mathf.Max(0f, _flashLightDurationSeconds);
        public Color FlashLightColor => _flashLightColor;
    }
}
