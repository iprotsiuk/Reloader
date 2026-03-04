using UnityEngine;

namespace Reloader.Game.Weapons
{
    [CreateAssetMenu(fileName = "MagazineAttachmentDefinition", menuName = "Reloader/Game/Weapons/Magazine Attachment Definition")]
    public sealed class MagazineAttachmentDefinition : ScriptableObject
    {
        [SerializeField] private string _attachmentId = string.Empty;
        [SerializeField] private GameObject _magazineVisualPrefab;
        [SerializeField] private GameObject _droppedMagazinePrefab;
        [SerializeField] private bool _detachOnReloadStart = true;
        [SerializeField] private bool _spawnDroppedMagazine = true;
        [SerializeField, Min(0f)] private float _droppedMagazineLifetimeSeconds = 3f;

        public string AttachmentId => string.IsNullOrWhiteSpace(_attachmentId) ? string.Empty : _attachmentId;
        public GameObject MagazineVisualPrefab => _magazineVisualPrefab;
        public GameObject DroppedMagazinePrefab => _droppedMagazinePrefab;
        public bool DetachOnReloadStart => _detachOnReloadStart;
        public bool SpawnDroppedMagazine => _spawnDroppedMagazine;
        public float DroppedMagazineLifetimeSeconds => Mathf.Max(0f, _droppedMagazineLifetimeSeconds);
    }
}
