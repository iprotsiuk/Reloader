using System.Collections.Generic;
using Reloader.Core;
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
        private bool _loggedMissingInputSource;

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
            UpdateDebugFields();
            if (Runtime == null)
            {
                return;
            }

            if (!DependencyResolutionGuard.HasRequiredReferences(
                    ref _loggedMissingInputSource,
                    this,
                    "PlayerInventoryController requires an IPlayerInputSource reference.",
                    _inputSource))
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

            if (_pickupTargetResolver == null || !_pickupTargetResolver.TryResolvePickupTarget(out var pickupTarget) || pickupTarget == null)
            {
                return;
            }

            if (!_inputSource.ConsumePickupPressed())
            {
                return;
            }

            var resolvedItemId = ResolvePickupItemId(pickupTarget);
            if (!string.IsNullOrWhiteSpace(resolvedItemId))
            {
                _pendingPickupTargetsById[resolvedItemId] = pickupTarget;
                GameEvents.RaiseItemPickupRequested(resolvedItemId);
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

            _pendingPickupTargetsById.TryGetValue(itemId, out var pendingTarget);
            if (pendingTarget is IInventoryDefinitionPickupTarget definitionTarget
                && definitionTarget.SpawnDefinition != null
                && definitionTarget.SpawnDefinition.ItemDefinition != null)
            {
                var maxStack = definitionTarget.SpawnDefinition.ItemDefinition.MaxStack;
                Runtime.SetItemMaxStack(itemId, maxStack);
            }

            var stackTarget = pendingTarget as IInventoryStackPickupTarget;
            var isStackPickup = stackTarget != null;
            var quantity = isStackPickup ? Mathf.Max(1, stackTarget.Quantity) : 1;
            var area = InventoryArea.Belt;
            var index = -1;
            var rejectReason = PickupRejectReason.InvalidItem;
            var stored = isStackPickup
                ? Runtime.TryAddStackItem(itemId, quantity, out area, out index, out rejectReason)
                : Runtime.TryStoreItem(itemId, out area, out index, out rejectReason);
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
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }

            _pickupTargetResolver ??= _pickupTargetResolverBehaviour as IInventoryPickupTargetResolver;
            if (_pickupTargetResolver == null)
            {
                _pickupTargetResolver = DependencyResolutionGuard.FindInterface<IInventoryPickupTargetResolver>(GetComponents<MonoBehaviour>());
            }
        }

        private static string ResolvePickupItemId(IInventoryPickupTarget pickupTarget)
        {
            if (pickupTarget is IInventoryDefinitionPickupTarget definitionTarget
                && definitionTarget.SpawnDefinition != null
                && definitionTarget.SpawnDefinition.IsValid)
            {
                return definitionTarget.SpawnDefinition.ItemId;
            }

            return pickupTarget.ItemId;
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

    }
}
