using System;
using System.Reflection;
using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class AttachmentManager : MonoBehaviour
    {
        private const string SightAnchorName = "SightAnchor";

        [Header("Weapon Mounts")]
        [SerializeField] private Transform _scopeSlot;
        [SerializeField] private Transform _muzzleSlot;
        [SerializeField] private MuzzleAttachmentRuntime _muzzleRuntime;

        [Header("Fallback")]
        [SerializeField] private Transform _ironSightAnchor;

        private GameObject _equippedOpticInstance;
        private Transform _activeSightAnchor;
        private OpticDefinition _activeOpticDefinition;
        private MuzzleAttachmentDefinition _activeMuzzleDefinition;

        public event Action<OpticDefinition> ActiveOpticChanged;
        public OpticDefinition ActiveOpticDefinition => _activeOpticDefinition;
        public MuzzleAttachmentDefinition ActiveMuzzleDefinition => _activeMuzzleDefinition;

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

            RaiseActiveOpticChanged();
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
            RaiseActiveOpticChanged();
        }

        public Transform GetActiveSightAnchor()
        {
            if (_activeSightAnchor == null)
            {
                RefreshSightAnchor();
            }

            return _activeSightAnchor;
        }

        public bool EquipMuzzle(MuzzleAttachmentDefinition muzzle)
        {
            if (muzzle == null)
            {
                UnequipMuzzle();
                return true;
            }

            if (_muzzleRuntime != null)
            {
                if (!TryEquipMuzzleWithRuntime(muzzle))
                {
                    _activeMuzzleDefinition = null;
                    return false;
                }

                _activeMuzzleDefinition = muzzle;
                return true;
            }

            if (_muzzleSlot == null || muzzle.MuzzlePrefab == null)
            {
                _activeMuzzleDefinition = null;
                return false;
            }

            for (var i = _muzzleSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(_muzzleSlot.GetChild(i).gameObject);
            }

            Instantiate(muzzle.MuzzlePrefab, _muzzleSlot, false);
            _activeMuzzleDefinition = muzzle;
            return true;
        }

        public void UnequipMuzzle()
        {
            _activeMuzzleDefinition = null;
            if (_muzzleRuntime != null)
            {
                _muzzleRuntime.Unequip();
            }

            if (_muzzleSlot == null)
            {
                return;
            }

            for (var i = _muzzleSlot.childCount - 1; i >= 0; i--)
            {
                Destroy(_muzzleSlot.GetChild(i).gameObject);
            }
        }

        private bool TryEquipMuzzleWithRuntime(MuzzleAttachmentDefinition muzzle)
        {
            if (_muzzleRuntime == null || muzzle == null || muzzle.MuzzlePrefab == null)
            {
                _muzzleRuntime?.Unequip();
                return false;
            }

            var slot = ResolveRuntimeAttachmentSlot(_muzzleRuntime);
            if (slot == null)
            {
                _muzzleRuntime.Unequip();
                return false;
            }

            _muzzleRuntime.Equip(muzzle);
            if (_muzzleRuntime.ActiveAttachment != muzzle)
            {
                return false;
            }

            if (slot.childCount == 0)
            {
                return false;
            }

            var expectedPrefix = muzzle.MuzzlePrefab.name;
            for (var i = 0; i < slot.childCount; i++)
            {
                var child = slot.GetChild(i);
                if (child != null && child.name.StartsWith(expectedPrefix))
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform ResolveRuntimeAttachmentSlot(MuzzleAttachmentRuntime runtime)
        {
            if (runtime == null)
            {
                return null;
            }

            var slotField = typeof(MuzzleAttachmentRuntime).GetField("_attachmentSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            return slotField?.GetValue(runtime) as Transform;
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

        private void RaiseActiveOpticChanged()
        {
            ActiveOpticChanged?.Invoke(_activeOpticDefinition);
        }
    }
}
