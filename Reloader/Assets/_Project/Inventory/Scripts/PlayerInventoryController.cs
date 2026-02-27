using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Inventory
{
    public sealed class PlayerInventoryController : MonoBehaviour
    {
        private const int DefaultBackpackCapacity = 9;

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _pickupTargetResolverBehaviour;
        [SerializeField] private int _startingBackpackCapacity = DefaultBackpackCapacity;
        [Header("Debug (Runtime)")]
        [SerializeField] private int _selectedBeltIndexDebug = -1;
        [SerializeField] private string _selectedBeltItemIdDebug;
        [SerializeField] private bool _inputResolvedDebug;

        private IPlayerInputSource _inputSource;
        private IInventoryPickupTargetResolver _pickupTargetResolver;
        private IInventoryEvents _inventoryEvents;
        private IInventoryEvents _subscribedInventoryEvents;
        private bool _useRuntimeKernelInventoryEvents = true;
        private readonly Dictionary<string, IInventoryPickupTarget> _pendingPickupTargetsById = new Dictionary<string, IInventoryPickupTarget>();
        private bool _loggedMissingInputSource;
        private bool _flushPickupInputAtEndOfFrame;

        public PlayerInventoryRuntime Runtime { get; private set; }
        public int SelectedBeltIndexDebug => _selectedBeltIndexDebug;

        private void Awake()
        {
            Runtime = new PlayerInventoryRuntime();
            Runtime.SetBackpackCapacity(ResolveStartingBackpackCapacity());
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRuntimeHubReconfigure();
            SubscribeToInventoryEvents(ResolveInventoryEvents());
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromInventoryEvents();
            _pendingPickupTargetsById.Clear();
            _flushPickupInputAtEndOfFrame = false;
        }

        private void Update()
        {
            Tick();
        }

        private void LateUpdate()
        {
            if (!_flushPickupInputAtEndOfFrame || _inputSource == null)
            {
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            _inputSource.ConsumePickupPressed();
        }

        public void Configure(
            IPlayerInputSource inputSource,
            IInventoryPickupTargetResolver pickupTargetResolver,
            PlayerInventoryRuntime runtime = null,
            IInventoryEvents inventoryEvents = null)
        {
            _inputSource = inputSource;
            _pickupTargetResolver = pickupTargetResolver;
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToInventoryEvents(ResolveInventoryEvents());
            }

            Runtime = runtime ?? new PlayerInventoryRuntime();
            Runtime.SetBackpackCapacity(ResolveStartingBackpackCapacity());
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
                    ResolveInventoryEvents().RaiseBeltSelectionChanged(Runtime.SelectedBeltIndex);
                    UpdateDebugFields();
                }
            }

            if (_pickupTargetResolver == null || !_pickupTargetResolver.TryResolvePickupTarget(out var pickupTarget) || pickupTarget == null)
            {
                _flushPickupInputAtEndOfFrame = true;
                return;
            }

            var pickupPressedThisFrame = IsPickupPressedThisFrame();
            if (!pickupPressedThisFrame)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            var resolvedItemId = ResolvePickupItemId(pickupTarget);
            if (!string.IsNullOrWhiteSpace(resolvedItemId))
            {
                _pendingPickupTargetsById[resolvedItemId] = pickupTarget;
                ResolveInventoryEvents().RaiseItemPickupRequested(resolvedItemId);
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

            ResolveInventoryEvents().RaiseInventoryChanged();
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
                ResolveInventoryEvents().RaiseItemPickupRejected(itemId, rejectReason);
                _pendingPickupTargetsById.Remove(itemId);
                return;
            }

            ResolveInventoryEvents().RaiseItemStored(itemId, area, index);
            ResolveInventoryEvents().RaiseInventoryChanged();
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

            ResolveInventoryEvents();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelInventoryEvents)
            {
                return;
            }

            SubscribeToInventoryEvents(ResolveInventoryEvents());
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            if (_useRuntimeKernelInventoryEvents)
            {
                var runtimeInventoryEvents = RuntimeKernelBootstrapper.InventoryEvents;
                if (!ReferenceEquals(_inventoryEvents, runtimeInventoryEvents))
                {
                    _inventoryEvents = runtimeInventoryEvents;
                    SubscribeToInventoryEvents(_inventoryEvents);
                }
                else if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
                {
                    SubscribeToInventoryEvents(_inventoryEvents);
                }

                return _inventoryEvents;
            }

            if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
            {
                SubscribeToInventoryEvents(_inventoryEvents);
            }

            return _inventoryEvents;
        }

        private void SubscribeToInventoryEvents(IInventoryEvents inventoryEvents)
        {
            if (inventoryEvents == null)
            {
                UnsubscribeFromInventoryEvents();
                return;
            }

            if (ReferenceEquals(_subscribedInventoryEvents, inventoryEvents))
            {
                return;
            }

            UnsubscribeFromInventoryEvents();
            _subscribedInventoryEvents = inventoryEvents;
            _subscribedInventoryEvents.OnItemPickupRequested += HandleItemPickupRequested;
        }

        private void UnsubscribeFromInventoryEvents()
        {
            if (_subscribedInventoryEvents == null)
            {
                return;
            }

            _subscribedInventoryEvents.OnItemPickupRequested -= HandleItemPickupRequested;
            _subscribedInventoryEvents = null;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
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

        private bool IsPickupPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                _inputSource?.ConsumePickupPressed();
                return true;
            }

            return _inputSource != null && _inputSource.ConsumePickupPressed();
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

        private int ResolveStartingBackpackCapacity()
        {
            return _startingBackpackCapacity > 0 ? _startingBackpackCapacity : DefaultBackpackCapacity;
        }
    }
}
