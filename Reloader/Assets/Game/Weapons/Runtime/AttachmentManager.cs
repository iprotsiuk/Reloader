using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class AttachmentManager : MonoBehaviour
    {
        private const string SightAnchorName = "SightAnchor";

        [Header("Weapon Mounts")]
        [SerializeField] private Transform _scopeSlot;

        [Header("Fallback")]
        [SerializeField] private Transform _ironSightAnchor;

        private GameObject _equippedOpticInstance;
        private Transform _activeSightAnchor;
        private OpticDefinition _activeOpticDefinition;

        public OpticDefinition ActiveOpticDefinition => _activeOpticDefinition;

        private void Awake()
        {
            RefreshSightAnchor();
        }

        public bool EquipOptic(OpticDefinition optic)
        {
            if (_scopeSlot == null)
            {
                Debug.LogWarning("AttachmentManager: ScopeSlot is not assigned.", this);
                return false;
            }

            UnequipOptic();

            if (optic == null)
            {
                RefreshSightAnchor();
                return _activeSightAnchor != null;
            }

            if (optic.OpticPrefab == null)
            {
                Debug.LogWarning("AttachmentManager: OpticDefinition has no OpticPrefab. Equip rejected.", optic);
                RefreshSightAnchor();
                return false;
            }

            _activeOpticDefinition = optic;

            _equippedOpticInstance = Instantiate(_activeOpticDefinition.OpticPrefab, _scopeSlot, false);
            _activeSightAnchor = ResolveSightAnchor(_equippedOpticInstance.transform);
            if (_activeSightAnchor == null)
            {
                Debug.LogWarning("AttachmentManager: Equipped optic has no SightAnchor. Falling back to optic root.", _equippedOpticInstance);
                _activeSightAnchor = _equippedOpticInstance.transform;
            }

            return true;
        }

        public void UnequipOptic()
        {
            if (_equippedOpticInstance != null)
            {
                Destroy(_equippedOpticInstance);
                _equippedOpticInstance = null;
            }

            _activeOpticDefinition = null;
            RefreshSightAnchor();
        }

        public Transform GetActiveSightAnchor()
        {
            if (_activeSightAnchor == null)
            {
                RefreshSightAnchor();
            }

            return _activeSightAnchor;
        }

        private void RefreshSightAnchor()
        {
            _activeSightAnchor = _ironSightAnchor;
        }

        private static Transform ResolveSightAnchor(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == SightAnchorName)
                {
                    return transforms[i];
                }
            }

            return null;
        }
    }
}
