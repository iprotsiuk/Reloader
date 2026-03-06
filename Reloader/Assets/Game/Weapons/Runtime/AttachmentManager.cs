using System;
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
        [Header("Debug")]
        [SerializeField] private bool _verboseOpticLogs;

        private GameObject _equippedOpticInstance;
        private Transform _activeSightAnchor;
        private OpticDefinition _activeOpticDefinition;
        private MuzzleAttachmentDefinition _activeMuzzleDefinition;

        public event Action<OpticDefinition> ActiveOpticChanged;
        public OpticDefinition ActiveOpticDefinition => _activeOpticDefinition;
        public MuzzleAttachmentDefinition ActiveMuzzleDefinition => _activeMuzzleDefinition;
        public GameObject ActiveOpticInstance => _equippedOpticInstance;
        public Transform ScopeSlot => _scopeSlot;
        public Transform MuzzleSlot => _muzzleSlot;

        private void Awake()
        {
            RefreshSightAnchor();
        }

        public void ConfigureMounts(
            Transform scopeSlot,
            Transform ironSightAnchor,
            Transform muzzleSlot,
            MuzzleAttachmentRuntime muzzleRuntime)
        {
            _scopeSlot = scopeSlot;
            _ironSightAnchor = ironSightAnchor;
            _muzzleSlot = muzzleSlot;
            _muzzleRuntime = muzzleRuntime;
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
                if (_verboseOpticLogs)
                {
                    Debug.Log("AttachmentManager: EquipOptic(null) -> unequipped optic, using fallback sight anchor.", this);
                }
                return _activeSightAnchor != null;
            }

            var opticPrefab = optic.OpticPrefab;
            if (opticPrefab == null)
            {
                Debug.LogWarning("AttachmentManager: OpticDefinition has no OpticPrefab. Equip rejected.", optic);
                RefreshSightAnchor();
                return false;
            }

            _activeOpticDefinition = optic;
            if (_verboseOpticLogs)
            {
                Debug.Log(
                    $"AttachmentManager: EquipOptic begin. opticId='{optic.OpticId}', prefabObject='{opticPrefab.name}', type='{opticPrefab.GetType().Name}', scopeSlot='{_scopeSlot.name}'.",
                    this);
            }

            try
            {
                _equippedOpticInstance = Instantiate(opticPrefab, _scopeSlot, false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"AttachmentManager: Optic instantiate threw {ex.GetType().Name}. opticId='{optic.OpticId}', prefabObject='{opticPrefab.name}', prefabType='{opticPrefab.GetType().FullName}'. Equip rejected.",
                    optic);
                UnequipOptic();
                return false;
            }

            if (_equippedOpticInstance == null)
            {
                Debug.LogWarning(
                    $"AttachmentManager: Optic instantiate returned null. opticId='{optic.OpticId}', prefabObject='{opticPrefab.name}', prefabType='{opticPrefab.GetType().FullName}'. Equip rejected.",
                    optic);
                UnequipOptic();
                return false;
            }

            _activeSightAnchor = ResolveSightAnchor(_equippedOpticInstance.transform);
            if (_activeSightAnchor == null)
            {
                Debug.LogWarning(
                    $"AttachmentManager: Scoped optic '{optic.OpticId}' is missing an authored {SightAnchorName}. Equip rejected.",
                    optic);
                DisposeInvalidOpticInstance();
                return false;
            }
            ApplySlotLayer(_scopeSlot);

            if (_verboseOpticLogs)
            {
                Debug.Log(
                    $"AttachmentManager: EquipOptic success. spawned='{_equippedOpticInstance.name}', scopeSlotChildren={_scopeSlot.childCount}, activeSightAnchor='{_activeSightAnchor.name}'.",
                    this);
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
            if (_verboseOpticLogs)
            {
                Debug.Log("AttachmentManager: UnequipOptic complete.", this);
            }
            RaiseActiveOpticChanged();
        }

        private void DisposeInvalidOpticInstance()
        {
            if (_equippedOpticInstance != null)
            {
                _equippedOpticInstance.SetActive(false);
                _equippedOpticInstance.transform.SetParent(null, false);
                Destroy(_equippedOpticInstance);
                _equippedOpticInstance = null;
            }

            _activeOpticDefinition = null;
            RefreshSightAnchor();
            if (_verboseOpticLogs)
            {
                Debug.Log("AttachmentManager: Invalid optic instance disposed during equip.", this);
            }
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
            ApplySlotLayer(_muzzleSlot);
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

            ApplySlotLayer(slot);

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

            var slotField = typeof(MuzzleAttachmentRuntime).GetField("_attachmentSlot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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

        private static void ApplySlotLayer(Transform slot)
        {
            if (slot == null)
            {
                return;
            }

            for (var i = 0; i < slot.childCount; i++)
            {
                ApplyLayerRecursively(slot.GetChild(i), slot.gameObject.layer);
            }
        }

        private static void ApplyLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                ApplyLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void RaiseActiveOpticChanged()
        {
            ActiveOpticChanged?.Invoke(_activeOpticDefinition);
        }
    }
}
