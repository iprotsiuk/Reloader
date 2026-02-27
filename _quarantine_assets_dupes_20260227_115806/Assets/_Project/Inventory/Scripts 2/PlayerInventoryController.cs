using System.Collections.Generic;
using Reloader.Core.Events;
using Reloader.Player;
using UnityEngine;

namespace Reloader.Inventory
{
    public sealed class PlayerInventoryController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _pickupTargetResolverBehaviour;
        [SerializeField] private int _startingBackpackCapacity;
        [Header("Debug (Runtime)")]
        [SerializeField] private int _selectedBeltIndexDebug = -1;
        [SerializeField] private string _selectedBeltItemIdDebug;
        [SerializeField] private bool _inputResolvedDebug;

        private IPlayerInputSource _inputSource;
        private IInventoryPickupTargetResolver _pickupTargetResolver;
        private readonly Dictionary<string, IInventoryPickupTarget> _pendingPickupTargetsById = new Dictionary<string, IInventoryPickupTarget>();

        public PlayerInventoryRuntime Runtime { get; private set; }
        public int SelectedBeltIndexDebug => _selectedBeltIndexDebug;

        private void Awake()
        {
            Runtime = new PlayerInventoryRuntime();
            Runtime.SetBackpackCapacity(_startingBackpackCapacity);
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            GameEvents.OnItemPickupRequested += HandleItemPickupRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnItemPickupRequested -= HandleItemPickupRequested;
            _pendingPickupTargetsById.Clear();
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(IPlayerInputSource inputSource, IInventoryPickupTargetResolver pickupTargetResolver, PlayerInventoryRuntime runtime = null)
        {
            _inputSource = inputSource;
            _pickupTargetResolver = pickupTargetResolver;
            Runtime = runtime ?? new PlayerInventoryRuntime();
            Runtime.SetBackpackCapacity(_startingBackpackCapacity);
        }

        public void Tick()
        {
            ResolveReferences();
            UpdateDebugFields();
            if (_inputSource == null || Runtime == null)
            {
                return;
            }

            var beltPressed = _inputSource.ConsumeBeltSelectPressed();
            if (beltPressed >= 0)
            {
                var previous = Runtime.SelectedBeltIndex;
                Runtime.SelectBeltSlot(beltPressed);
                if (Runtime.SelectedBeltIndex != previous)
                {
                    GameEvents.RaiseBeltSelectionChanged(Runtime.SelectedBeltIndex);
                    UpdateDebugFields();
                }
            }

            if (!_inputSource.ConsumePickupPressed())
            {
                return;
            }

            if (_pickupTargetResolver == null || !_pickupTargetResolver.TryResolvePickupTarget(out var pickupTarget) || pickupTarget == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(pickupTarget.ItemId))
            {
                _pendingPickupTargetsById[pickupTarget.ItemId] = pickupTarget;
                GameEvents.RaiseItemPickupRequested(pickupTarget.ItemId);
            }
        }

        public bool TryMoveItem(InventoryArea sourceArea, int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (Runtime == null)
            {
                return false;
            }

            var moved = Runtime.TryMoveItem(sourceArea, sourceIndex, targetArea, targetIndex);
            if (!moved)
            {
                return false;
            }

            GameEvents.RaiseInventoryChanged();
            UpdateDebugFields();
            return true;
        }

        private void HandleItemPickupRequested(string itemId)
        {
            if (Runtime == null)
            {
                return;
            }

            var stored = Runtime.TryStoreItem(itemId, out var area, out var index, out var rejectReason);
            if (!stored)
            {
                GameEvents.RaiseItemPickupRejected(itemId, rejectReason);
                _pendingPickupTargetsById.Remove(itemId);
                return;
            }

            GameEvents.RaiseItemStored(itemId, area, index);
            GameEvents.RaiseInventoryChanged();
            if (_pendingPickupTargetsById.TryGetValue(itemId, out var target) && target != null)
            {
                target.OnPickedUp();
            }

            _pendingPickupTargetsById.Remove(itemId);
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            if (_inputSource == null)
            {
                _inputSource = GetInterfaceFromBehaviours<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                _inputSource = GetInterfaceFromBehaviours<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            _pickupTargetResolver ??= _pickupTargetResolverBehaviour as IInventoryPickupTargetResolver;
            if (_pickupTargetResolver == null)
            {
                _pickupTargetResolver = GetInterfaceFromBehaviours<IInventoryPickupTargetResolver>(GetComponents<MonoBehaviour>());
            }
        }

        private void UpdateDebugFields()
        {
            _inputResolvedDebug = _inputSource != null;
            if (Runtime == null)
            {
                _selectedBeltIndexDebug = -1;
                _selectedBeltItemIdDebug = null;
                return;
            }

            _selectedBeltIndexDebug = Runtime.SelectedBeltIndex;
            _selectedBeltItemIdDebug = Runtime.SelectedBeltItemId;
        }

        private static TInterface GetInterfaceFromBehaviours<TInterface>(MonoBehaviour[] behaviours) where TInterface : class
        {
            if (behaviours == null)
            {
                return null;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is TInterface typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}
